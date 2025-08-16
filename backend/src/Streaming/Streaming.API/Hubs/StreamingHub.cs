using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using Streaming.Domain.Entities;
using Streaming.Service.Commands;
using Streaming.Service.Queries;
using Streaming.Service.ViewModels;
using Streaming.Service.WebRTC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Streaming.API.Hubs
{
	public class StreamingHub : Hub
	{
		private readonly IWebRTCManager _webRTCManager;
		private readonly IMediator _mediator;
		private readonly ILogger<StreamingHub> _logger;
		private static readonly ConcurrentDictionary<string, UserConnection> _userConnections = new();
		private static readonly ConcurrentDictionary<string, HashSet<string>> _cameraViewers = new();

		public StreamingHub(
			IWebRTCManager webRTCManager,
			IMediator mediator,
			ILogger<StreamingHub> logger)
		{
			_webRTCManager = webRTCManager;
			_mediator = mediator;
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.UserIdentifier ?? Context.User?.FindFirst("sub")?.Value;
			var connectionId = Context.ConnectionId;

			if (!string.IsNullOrEmpty(userId))
			{
				_userConnections[connectionId] = new UserConnection
				{
					ConnectionId = connectionId,
					UserId = userId,
					ConnectedAt = DateTime.UtcNow
				};

				await Groups.AddToGroupAsync(connectionId, $"user-{userId}");
				_logger.LogInformation($"User {userId} connected with connection {connectionId}");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			var connectionId = Context.ConnectionId;

			if (_userConnections.TryRemove(connectionId, out var userConnection))
			{
				// Remove from all camera groups
				foreach (var cameraGroup in _cameraViewers.ToList())
				{
					if (cameraGroup.Value.Contains(connectionId))
					{
						cameraGroup.Value.Remove(connectionId);
						await Groups.RemoveFromGroupAsync(connectionId, $"camera-{cameraGroup.Key}");

						// Close WebRTC connection
						await _webRTCManager.CloseConnectionAsync($"{cameraGroup.Key}-{connectionId}");

						// End stream session
						var endSessionResult = await _mediator.Send(new EndStreamSessionCommand
						{
							CameraId = Guid.Parse(cameraGroup.Key),
							ViewerId = userConnection.UserId,
							ConnectionId = connectionId
						});

						if (endSessionResult.HttpCode != HttpCode.OK)
						{
							_logger.LogWarning($"Failed to end stream session: {endSessionResult.Value.ErrorMessage}");
						}
					}
				}

				await Groups.RemoveFromGroupAsync(connectionId, $"user-{userConnection.UserId}");
				_logger.LogInformation($"User {userConnection.UserId} disconnected");
			}

			await base.OnDisconnectedAsync(exception);
		}

		// WebRTC Signaling Methods
		public async Task<SignalResponse> RequestCameraStream(string cameraId)
		{
			try
			{
				var userId = Context.UserIdentifier;
				var connectionId = Context.ConnectionId;

				// Check if user has permission to view this camera
				var permissionResult = await _mediator.Send(new CheckCameraPermissionQuery
				{
					UserId = userId,
					CameraId = Guid.Parse(cameraId),
					PermissionType = "View"
				});

				if (permissionResult.HttpCode != HttpCode.OK)
				{
					return new SignalResponse
					{
						Success = false,
						Error = "Access denied to camera"
					};
				}

				var hasPermission = permissionResult.Value.Success;

				if (!hasPermission)
				{
					return new SignalResponse
					{
						Success = false,
						Error = "Access denied to camera"
					};
				}

				// Get camera details
				var cameraResult = await _mediator.Send(new GetCameraByIdQuery
				{
					CameraId = Guid.Parse(cameraId)
				});

				if (cameraResult.HttpCode != HttpCode.OK)
				{
					return new SignalResponse
					{
						Success = false,
						Error = "Camera not found"
					};
				}

				// Extract camera data from ApiResult using your pattern
				var camera = cameraResult.Value.Result as CameraViewModel;

				if (camera == null || camera.Status != CameraStatus.Active.ToString())
				{
					return new SignalResponse
					{
						Success = false,
						Error = "Camera not available"
					};
				}

				// Create WebRTC connection
				var webRtcConnectionId = $"{cameraId}-{connectionId}";
				var connection = await _webRTCManager.CreateConnectionAsync(webRtcConnectionId, camera.StreamUrl);

				// Add to camera viewers group
				await Groups.AddToGroupAsync(connectionId, $"camera-{cameraId}");

				if (!_cameraViewers.ContainsKey(cameraId))
				{
					_cameraViewers[cameraId] = new HashSet<string>();
				}
				_cameraViewers[cameraId].Add(connectionId);

				// Create and return offer
				var sipsorceryOffer = _webRTCManager.CreateOfferAsync(webRtcConnectionId);

				// Convert SIPSorcery offer to browser-compatible format
				var browserOffer = new
				{
					type = "offer", // Convert enum to string
					sdp = sipsorceryOffer.sdp
				};

				// Log stream session
				var sessionResult = await _mediator.Send(new CreateStreamSessionCommand
				{
					CameraId = Guid.Parse(cameraId),
					ViewerId = userId,
					ConnectionId = connectionId,
					SessionDescription = JsonSerializer.Serialize(browserOffer),
					UserAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString(),
					IpAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString()
				});

				if (sessionResult.HttpCode != HttpCode.OK)
				{
					_logger.LogWarning($"Failed to create stream session: {sessionResult.Value.ErrorMessage}");
				}

				return new SignalResponse
				{
					Success = true,
					Data = browserOffer // Return the browser-compatible offer
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error requesting camera stream {cameraId}");
				return new SignalResponse
				{
					Success = false,
					Error = ex.Message
				};
			}
		}

		public async Task<SignalResponse> SendAnswer(string cameraId, AnswerData answerData)
		{
			try
			{
				var connectionId = Context.ConnectionId;
				var webRtcConnectionId = $"{cameraId}-{connectionId}";

				_logger.LogInformation($"Received answer for camera {cameraId}: type={answerData.Type}, sdp length={answerData.Sdp?.Length}");

				// Convert to SIPSorcery format
				var sipsorceryAnswer = new RTCSessionDescriptionInit
				{
					type = RTCSdpType.answer,
					sdp = answerData.Sdp
				};

				var success = _webRTCManager.SetAnswerAsync(webRtcConnectionId, sipsorceryAnswer);

				return new SignalResponse
				{
					Success = success,
					Error = success ? null : "Failed to set answer"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error sending answer for camera {cameraId}");
				return new SignalResponse
				{
					Success = false,
					Error = ex.Message
				};
			}
		}

		public async Task<SignalResponse> SendIceCandidate(string cameraId, IceCandidateData candidateData)
		{
			try
			{
				var connectionId = Context.ConnectionId;
				var webRtcConnectionId = $"{cameraId}-{connectionId}";

				_logger.LogInformation($"Received ICE candidate for camera {cameraId}: {candidateData.Candidate}");

				// Convert to SIPSorcery format
				var sipsorceryCandidate = new RTCIceCandidateInit
				{
					candidate = candidateData.Candidate,
					sdpMid = candidateData.SdpMid,
					sdpMLineIndex = (ushort)candidateData.SdpMLineIndex
				};

				var success = _webRTCManager.AddIceCandidateAsync(webRtcConnectionId, sipsorceryCandidate);

				return new SignalResponse
				{
					Success = success,
					Error = success ? null : "Failed to add ICE candidate"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error adding ICE candidate for camera {cameraId}");
				return new SignalResponse
				{
					Success = false,
					Error = ex.Message
				};
			}
		}

		public async Task StopCameraStream(string cameraId)
		{
			try
			{
				var connectionId = Context.ConnectionId;
				var webRtcConnectionId = $"{cameraId}-{connectionId}";

				// Close WebRTC connection
				await _webRTCManager.CloseConnectionAsync(webRtcConnectionId);

				// Remove from camera group
				await Groups.RemoveFromGroupAsync(connectionId, $"camera-{cameraId}");

				if (_cameraViewers.ContainsKey(cameraId))
				{
					_cameraViewers[cameraId].Remove(connectionId);
				}

				// Update stream session
				var endSessionResult = await _mediator.Send(new EndStreamSessionCommand
				{
					CameraId = Guid.Parse(cameraId),
					ViewerId = Context.UserIdentifier,
					ConnectionId = connectionId
				});

				if (endSessionResult.HttpCode != HttpCode.OK)
				{
					_logger.LogWarning($"Failed to end stream session: {endSessionResult.Value.ErrorMessage}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error stopping camera stream {cameraId}");
			}
		}

		// Detection Event Methods
		public async Task NotifyPersonDetected(PersonDetectionEvent detectionEvent)
		{
			try
			{
				// Send to all users viewing this camera
				await Clients.Group($"camera-{detectionEvent.CameraId}")
					.SendAsync("PersonDetected", detectionEvent);

				// Send to users with alert permissions
				var alertUsersResult = await _mediator.Send(new GetAlertUsersForCameraQuery
				{
					CameraId = Guid.Parse(detectionEvent.CameraId)
				});

				if (alertUsersResult.HttpCode == HttpCode.OK)
				{
					// Extract alert users list from ApiResult
					var alertUsers = alertUsersResult.Value.Result as List<string> ?? new List<string>();

					foreach (var userId in alertUsers)
					{
						await Clients.Group($"user-{userId}")
							.SendAsync("AlertNotification", new AlertNotification
							{
								Type = "PersonDetected",
								CameraId = detectionEvent.CameraId,
								Timestamp = detectionEvent.Timestamp,
								Message = $"Person detected on camera {detectionEvent.CameraName}",
								ImageUrl = detectionEvent.FrameImageUrl
							});
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error notifying person detection");
			}
		}

		// Camera Control Methods
		public async Task<CameraControlResponse> ControlCamera(string cameraId, CameraControlCommand command)
		{
			try
			{
				// Check permissions
				var permissionResult = await _mediator.Send(new CheckCameraControlPermissionQuery
				{
					UserId = Context.UserIdentifier,
					CameraId = Guid.Parse(cameraId)
				});

				if (permissionResult.HttpCode != HttpCode.OK)
				{
					return new CameraControlResponse
					{
						Success = false,
						Error = "No permission to control camera"
					};
				}

				var hasPermission = permissionResult.Value.Success;

				if (!hasPermission)
				{
					return new CameraControlResponse
					{
						Success = false,
						Error = "No permission to control camera"
					};
				}

				// Execute camera control command
				var result = await _mediator.Send(new ExecuteCameraControlCommand
				{
					CameraId = Guid.Parse(cameraId),
					Command = command.Command,
					Parameters = command.Parameters
				});

				return new CameraControlResponse
				{
					Success = result.HttpCode == HttpCode.OK,
					Error = result.HttpCode == HttpCode.OK ? null : (result.Value.ErrorMessage ?? "Camera control failed")
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error controlling camera {cameraId}");
				return new CameraControlResponse
				{
					Success = false,
					Error = ex.Message
				};
			}
		}

		// Statistics Methods
		public async Task<StreamStatistics> GetStreamStatistics(string cameraId)
		{
			try
			{
				var connectionId = Context.ConnectionId;
				var webRtcConnectionId = $"{cameraId}-{connectionId}";

				var stats = _webRTCManager.GetConnectionStats(webRtcConnectionId);
				var viewerCount = _cameraViewers.ContainsKey(cameraId) ? _cameraViewers[cameraId].Count : 0;

				return new StreamStatistics
				{
					CameraId = cameraId,
					ViewerCount = viewerCount,
					ConnectionStats = stats,
					Timestamp = DateTime.UtcNow
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting stream statistics for camera {cameraId}");
				return new StreamStatistics { CameraId = cameraId };
			}
		}

		// Admin Methods
		[Authorize(Roles = "Admin")]
		public async Task<List<ActiveStreamInfo>> GetActiveStreams()
		{
			var activeStreams = new List<ActiveStreamInfo>();

			foreach (var camera in _cameraViewers.ToList())
			{
				try
				{
					var cameraResult = await _mediator.Send(new GetCameraByIdQuery
					{
						CameraId = Guid.Parse(camera.Key)
					});

					string cameraName = null;
					if (cameraResult.HttpCode == HttpCode.OK)
					{
						var cameraInfo = cameraResult.Value.Result as CameraViewModel;
						cameraName = cameraInfo?.Name;
					}

					activeStreams.Add(new ActiveStreamInfo
					{
						CameraId = camera.Key,
						CameraName = cameraName,
						ViewerCount = camera.Value.Count,
						Viewers = camera.Value.Select(connectionId =>
						{
							_userConnections.TryGetValue(connectionId, out var userConn);
							return new ViewerInfo
							{
								UserId = userConn?.UserId,
								ConnectionId = connectionId,
								ConnectedAt = userConn?.ConnectedAt ?? DateTime.MinValue
							};
						}).ToList()
					});
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error getting camera info for {camera.Key}");
					// Continue with other cameras
				}
			}

			return activeStreams;
		}
	}

	// Helper Classes (Keep existing structure)
	public class UserConnection
	{
		public string ConnectionId { get; set; }
		public string UserId { get; set; }
		public DateTime ConnectedAt { get; set; }
	}

	public class SignalResponse
	{
		public bool Success { get; set; }
		public string Error { get; set; }
		public object Data { get; set; }
	}

	public class PersonDetectionEvent
	{
		public string CameraId { get; set; }
		public string CameraName { get; set; }
		public DateTime Timestamp { get; set; }
		public List<Detection> Detections { get; set; }
		public string FrameImageUrl { get; set; }
		public string FrameImageBase64 { get; set; }
	}

	public class Detection
	{
		public float[] BoundingBox { get; set; } // [x1, y1, x2, y2]
		public float Confidence { get; set; }
		public string Label { get; set; }
	}

	public class AlertNotification
	{
		public string Type { get; set; }
		public string CameraId { get; set; }
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
		public string ImageUrl { get; set; }
	}

	public class CameraControlCommand
	{
		public string Command { get; set; } // PTZ commands: pan, tilt, zoom, preset
		public Dictionary<string, object> Parameters { get; set; }
	}

	public class CameraControlResponse
	{
		public bool Success { get; set; }
		public string Error { get; set; }
	}

	public class StreamStatistics
	{
		public string CameraId { get; set; }
		public int ViewerCount { get; set; }
		public Dictionary<string, object> ConnectionStats { get; set; }
		public DateTime Timestamp { get; set; }
	}

	public class ActiveStreamInfo
	{
		public string CameraId { get; set; }
		public string CameraName { get; set; }
		public int ViewerCount { get; set; }
		public List<ViewerInfo> Viewers { get; set; }
	}

	public class ViewerInfo
	{
		public string UserId { get; set; }
		public string ConnectionId { get; set; }
		public DateTime ConnectedAt { get; set; }
	}

	public class AnswerData
	{
		public string Type { get; set; }
		public string Sdp { get; set; }
	}

	public class IceCandidateData
	{
		public string Candidate { get; set; }
		public string SdpMid { get; set; }
		public int? SdpMLineIndex { get; set; }
	}
}