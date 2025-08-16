using Asp.Versioning;
using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using Streaming.Infrastructure.ApiRoute;
using Streaming.Service.Commands;
using Streaming.Service.Queries;
using Streaming.Service.ViewModels;
using Streaming.Service.WebRTC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Streaming.API.Controllers.v1
{
	[ApiVersion("1.0")]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiController]
	public class StreamController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IWebRTCManager _webRTCManager;
		private readonly ILogger<StreamController> _logger;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public StreamController(
			IMediator mediator,
			IWebRTCManager webRTCManager,
			ILogger<StreamController> logger,
			IHttpContextAccessor httpContextAccessor)
		{
			_mediator = mediator;
			_webRTCManager = webRTCManager;
			_logger = logger;
			_httpContextAccessor = httpContextAccessor;
		}

		/// <summary>
		/// Register a new camera in the system
		/// </summary>
		[HttpPost("camera/register")]
		public async Task<IActionResult> RegisterCamera([FromBody] RegisterCameraRequest request)
		{
			try
			{
				request.CreatedBy = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
				var result = await _mediator.Send(request);
				return result; // ApiResult implements IActionResult
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error registering camera");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Update camera information
		/// </summary>
		[HttpPut("camera/{cameraId}")]
		public async Task<IActionResult> UpdateCamera(Guid cameraId, [FromBody] UpdateCameraRequest request)
		{
			try
			{
				request.CameraId = cameraId;
				request.UpdatedBy = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get list of all cameras
		/// </summary>
		[HttpGet("cameras")]
		public async Task<IActionResult> GetCameras([FromQuery] GetCamerasRequest request)
		{
			try
			{
				var userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
				request.UserId = userId;
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting cameras");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get list of active cameras (for AI detection service)
		/// </summary>
		[HttpGet("cameras/active")]
		public async Task<IActionResult> GetActiveCameras()
		{
			try
			{
				var result = await _mediator.Send(new GetActiveCamerasQuery());
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active cameras");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get camera details by ID
		/// </summary>
		[HttpGet("camera/{cameraId}")]
		public async Task<IActionResult> GetCamera(Guid cameraId)
		{
			try
			{
				var userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
				var result = await _mediator.Send(new GetCameraByIdQuery
				{
					CameraId = cameraId
				});
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Delete a camera
		/// </summary>
		[HttpDelete("camera/{cameraId}")]
		public async Task<IActionResult> DeleteCamera(Guid cameraId)
		{
			try
			{
				var result = await _mediator.Send(new DeleteCameraCommand { CameraId = cameraId });
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get WebRTC offer for camera stream
		/// </summary>
		[HttpGet("camera/{cameraId}/offer")]
		public async Task<IActionResult> GetWebRTCOffer(Guid cameraId)
		{
			try
			{
				var userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();

				// Check permissions
				var permissionResult = await _mediator.Send(new CheckCameraPermissionQuery
				{
					UserId = userId,
					CameraId = cameraId
				});

				if (permissionResult.HttpCode != HttpCode.OK)
				{
					return ApiResult.Failed(HttpCode.Forbidden, "Access denied to camera");
				}

				// Get camera details
				var cameraResult = await _mediator.Send(new GetCameraByIdQuery
				{
					CameraId = cameraId
				});

				if (cameraResult.HttpCode != HttpCode.OK)
				{
					return ApiResult.Failed(HttpCode.Notfound, "Camera not found");
				}

				var camera = cameraResult.Value.Result as CameraViewModel;

				var connectionId = $"{cameraId}-{userId}-{Guid.NewGuid()}";
				await _webRTCManager.CreateConnectionAsync(connectionId, camera.StreamUrl);
				var offer = _webRTCManager.CreateOfferAsync(connectionId);

				return ApiResult.Succeeded(new
				{
					connectionId,
					offer
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting WebRTC offer for camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Set WebRTC answer for camera stream
		/// </summary>
		[HttpPost("camera/{cameraId}/answer")]
		public async Task<IActionResult> SetWebRTCAnswer(Guid cameraId, [FromBody] SetAnswerRequest request)
		{
			try
			{
				var success = _webRTCManager.SetAnswerAsync(request.ConnectionId, request.Answer);

				if (success)
				{
					// Create stream session record
					var userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
					var sessionResult = await _mediator.Send(new CreateStreamSessionCommand
					{
						CameraId = cameraId,
						ViewerId = userId,
						ConnectionId = request.ConnectionId
					});

					return ApiResult.Succeeded();
				}

				return ApiResult.Failed(HttpCode.BadRequest, "Failed to set answer");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error setting WebRTC answer for camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Add ICE candidate
		/// </summary>
		[HttpPost("camera/{cameraId}/ice-candidate")]
		public async Task<IActionResult> AddIceCandidate(Guid cameraId, [FromBody] AddIceCandidateRequest request)
		{
			try
			{
				var success = _webRTCManager.AddIceCandidateAsync(request.ConnectionId, request.Candidate);
				return success ? ApiResult.Succeeded() : ApiResult.Failed(HttpCode.BadRequest, "Failed to add ICE candidate");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error adding ICE candidate for camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get stream sessions for a camera
		/// </summary>
		[HttpGet("camera/{cameraId}/sessions")]
		public async Task<IActionResult> GetStreamSessions(Guid cameraId, [FromQuery] GetStreamSessionsRequest request)
		{
			try
			{
				request.CameraId = cameraId;
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting stream sessions for camera {cameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get detection events
		/// </summary>
		[HttpGet("detections")]
		public async Task<IActionResult> GetDetectionEvents([FromQuery] GetDetectionEventsRequest request)
		{
			try
			{
				var userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString();
				request.UserId = userId;
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting detection events");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get detection event by ID
		/// </summary>
		[HttpGet("detection/{detectionId}")]
		public async Task<IActionResult> GetDetectionEvent(Guid detectionId)
		{
			try
			{
				var result = await _mediator.Send(new GetDetectionEventByIdQuery { DetectionId = detectionId });
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting detection event {detectionId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Get stream statistics
		/// </summary>
		[HttpGet("statistics")]
		public async Task<IActionResult> GetStatistics([FromQuery] GetStreamStatisticsRequest request)
		{
			try
			{
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting stream statistics");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Test camera connection
		/// </summary>
		[HttpPost("camera/test")]
		public async Task<IActionResult> TestCameraConnection([FromBody] TestCameraConnectionRequest request)
		{
			try
			{
				var result = await _mediator.Send(request);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error testing camera connection");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		/// <summary>
		/// Create test stream for development/testing
		/// </summary>
		[HttpPost("test/create-stream")]
		public async Task<IActionResult> CreateTestStream([FromBody] CreateTestStreamRequest request)
		{
			try
			{
				var testCamera = new RegisterCameraRequest
				{
					Name = request.Name ?? "Test Camera",
					Location = "Test Location",
					StreamUrl = "test://pattern",
					Resolution = "1280x720",
					FrameRate = 30,
					TestMode = true,
					CreatedBy = _httpContextAccessor.HttpContext?.Request.Headers[HeaderInfo.USER_ID].ToString()
				};

				var result = await _mediator.Send(testCamera);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating test stream");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}
	}

	public class SetAnswerRequest
	{
		public string ConnectionId { get; set; }
		public RTCSessionDescriptionInit Answer { get; set; }
	}

	public class AddIceCandidateRequest
	{
		public string ConnectionId { get; set; }
		public RTCIceCandidateInit Candidate { get; set; }
	}

	public class CreateTestStreamRequest
	{
		public string Name { get; set; }
		public string Type { get; set; } = "pattern";
		public string VideoFilePath { get; set; }
	}
}