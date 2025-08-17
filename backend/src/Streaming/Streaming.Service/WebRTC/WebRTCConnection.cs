using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using Streaming.Service.Sources;
using System.Net;

namespace Streaming.Service.WebRTC
{
	public class WebRTCConnection : IDisposable
	{
		private readonly string _connectionId;
		private readonly string _streamSource;
		private readonly WebRTCConfiguration _config;
		private readonly ILogger _logger;
		private RTCPeerConnection? _peerConnection;
		private RtpFFmpegVideoSource? _rtpVideoSource;
		private CancellationTokenSource _cancellationTokenSource;
		private DateTime _createdAt;
		private long _framesSent;
		private static int _portCounter = 5020; // Start with even port

		public WebRTCConnection(string connectionId, string streamSource, WebRTCConfiguration config, ILogger logger)
		{
			_connectionId = connectionId;
			_streamSource = streamSource;
			_config = config;
			_logger = logger;
			_cancellationTokenSource = new CancellationTokenSource();
			_createdAt = DateTime.UtcNow;
		}

		public async Task InitializeAsync()
		{
			try
			{
				_logger.LogInformation($"Initializing WebRTC connection for {_connectionId} with source: {_streamSource}");

				// Start RTP video source first
				await StartRtpVideoSource();

				// Create RTCPeerConnection
				var rtcConfig = new RTCConfiguration
				{
					iceServers = _config.IceServers.Select(s => new RTCIceServer
					{
						urls = s.Url,
						username = s.Username,
						credential = s.Credential
					}).ToList()
				};

				_peerConnection = new RTCPeerConnection(rtcConfig);

				// Wire up event handlers
				SetupEventHandlers();

				// Create video track with the format from RTP source
				var videoFormat = _rtpVideoSource?.GetVideoFormat();
				if (videoFormat != null)
				{
					var videoFormats = new List<SDPAudioVideoMediaFormat> { videoFormat.Value };
					var videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, 
						videoFormats, MediaStreamStatusEnum.SendOnly);
					_peerConnection.addTrack(videoTrack);
					
					_logger.LogInformation($"Added video track with format: {videoFormat}");
				}
				else
				{
					throw new InvalidOperationException("Failed to get video format from RTP source");
				}

				_logger.LogInformation($"WebRTC connection initialized for {_connectionId}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to initialize WebRTC connection for {_connectionId}: {ex.Message}");
				throw;
			}
		}

		private async Task StartRtpVideoSource()
		{
			if (File.Exists(_streamSource))
			{
				// Ensure we get an even port (RTP requirement)
				var rtpPort = Interlocked.Add(ref _portCounter, 2);
				if (rtpPort % 2 != 0) rtpPort = Interlocked.Add(ref _portCounter, 1);
				
				_logger.LogInformation($"Creating RTP video source for file: {_streamSource} on port {rtpPort}");
				
				_rtpVideoSource = new RtpFFmpegVideoSource(_streamSource, rtpPort, _logger);
				var videoFormat = await _rtpVideoSource.StartAsync();
				
				_logger.LogInformation($"RTP video source started with format: {videoFormat}");
			}
			else
			{
				throw new FileNotFoundException($"Video file not found: {_streamSource}");
			}
		}

		private void SetupEventHandlers()
		{
			if (_peerConnection == null)
				return;

			_peerConnection.onicecandidate += (candidate) =>
			{
				_logger.LogDebug($"New ICE candidate for {_connectionId}: {candidate.candidate}");
			};

			_peerConnection.onconnectionstatechange += (state) =>
			{
				_logger.LogInformation($"Connection {_connectionId} state changed to: {state}");

				if (state == RTCPeerConnectionState.connected)
				{
					_logger.LogInformation($"WebRTC connected for {_connectionId} - starting RTP forwarding");
					StartRtpForwarding();
				}
				else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
				{
					_logger.LogWarning($"WebRTC connection failed/closed for {_connectionId}");
				}
			};

			_peerConnection.oniceconnectionstatechange += (state) =>
			{
				_logger.LogInformation($"ICE connection state for {_connectionId}: {state}");
			};

			_peerConnection.onicegatheringstatechange += (state) =>
			{
				_logger.LogInformation($"ICE gathering state for {_connectionId}: {state}");
			};

			_peerConnection.onsignalingstatechange += () =>
			{
				_logger.LogInformation($"Signaling state for {_connectionId}: {_peerConnection?.signalingState}");
			};
		}

		private void StartRtpForwarding()
		{
			if (_rtpVideoSource != null && _peerConnection != null)
			{
				_rtpVideoSource.OnRtpPacketReceived += (ep, media, rtpPkt) =>
				{
					if (media == SDPMediaTypesEnum.video && _peerConnection.VideoDestinationEndPoint != null)
					{
						try
						{
							_peerConnection.SendRtpRaw(media, rtpPkt.Payload, rtpPkt.Header.Timestamp, 
								rtpPkt.Header.MarkerBit, rtpPkt.Header.PayloadType);
							
							_framesSent++;
							
							if (_framesSent % 100 == 0) // Log every 100 packets
							{
								_logger.LogDebug($"Forwarded {_framesSent} RTP packets for {_connectionId}");
							}
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, $"Error forwarding RTP packet for {_connectionId}: {ex.Message}");
						}
					}
				};
				
				_logger.LogInformation($"RTP forwarding started for {_connectionId}");
			}
		}

		public RTCSessionDescriptionInit CreateOfferAsync()
		{
			if (_peerConnection == null)
				throw new InvalidOperationException("Peer connection not initialized");

			try
			{
				_logger.LogInformation($"Creating offer for {_connectionId}");
				
				var offer = _peerConnection.createOffer(new RTCOfferOptions());
				
				if (offer == null)
				{
					throw new InvalidOperationException($"Failed to create offer for {_connectionId}");
				}

				_logger.LogInformation($"Setting local description for {_connectionId}, SDP length: {offer.sdp?.Length ?? 0}");
				
				var setLocalResult = _peerConnection.setLocalDescription(offer);
				
				_logger.LogInformation($"Set local description result for {_connectionId}: {setLocalResult}");

				_logger.LogInformation($"Successfully created and set offer for {_connectionId}");
				return offer;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error creating offer for {_connectionId}: {ex.Message}");
				throw;
			}
		}

		public bool SetAnswerAsync(RTCSessionDescriptionInit answer)
		{
			try
			{
				if (_peerConnection == null)
				{
					_logger.LogError($"Peer connection is null for {_connectionId}");
					return false;
				}

				if (answer == null)
				{
					_logger.LogError($"Answer is null for {_connectionId}");
					return false;
				}

				_logger.LogInformation($"Setting remote description for {_connectionId}, type: {answer.type}, SDP length: {answer.sdp?.Length ?? 0}");
				_logger.LogInformation($"Current signaling state: {_peerConnection.signalingState}");

				var result = _peerConnection.setRemoteDescription(answer);
				
				if (result == SetDescriptionResultEnum.OK)
				{
					_logger.LogInformation($"Successfully set remote description for {_connectionId}");
					return true;
				}
				else
				{
					_logger.LogError($"Failed to set remote description for {_connectionId}, result: {result}");
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Exception setting answer for {_connectionId}: {ex.Message}");
				return false;
			}
		}

		public bool AddIceCandidateAsync(RTCIceCandidateInit candidate)
		{
			try
			{
				if (_peerConnection == null)
					return false;

				_peerConnection.addIceCandidate(candidate);
				_logger.LogDebug($"Added ICE candidate for {_connectionId}");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to add ICE candidate for {_connectionId}: {ex.Message}");
				return false;
			}
		}

		public Dictionary<string, object> GetStats()
		{
			return new Dictionary<string, object>
			{
				["connectionId"] = _connectionId,
				["connectionState"] = _peerConnection?.connectionState.ToString() ?? "Unknown",
				["iceConnectionState"] = _peerConnection?.iceConnectionState.ToString() ?? "Unknown",
				["framesSent"] = _framesSent,
				["uptime"] = DateTime.UtcNow - _createdAt,
				["streamSource"] = _streamSource,
				["sourceType"] = "RtpFFmpeg"
			};
		}

		public async Task CloseAsync()
		{
			_logger.LogInformation($"Closing WebRTC connection {_connectionId}");

			_cancellationTokenSource?.Cancel();

			_rtpVideoSource?.Stop();

			_peerConnection?.close();
		}

		public void Dispose()
		{
			_logger.LogInformation($"Disposing WebRTC connection {_connectionId}");

			try
			{
				CloseAsync().Wait();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error during cleanup for {_connectionId}");
			}

			_rtpVideoSource?.Dispose();
			_peerConnection?.Dispose();
			_cancellationTokenSource?.Dispose();
		}
	}
}