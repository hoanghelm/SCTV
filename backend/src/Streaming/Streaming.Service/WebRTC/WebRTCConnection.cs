using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using Streaming.Service.Models;
using Streaming.Service.Sources;
using IVideoSource = Streaming.Service.Models.IVideoSource;

namespace Streaming.Service.WebRTC
{
	public class WebRTCConnection : IDisposable
	{
		private readonly string _connectionId;
		private readonly string _streamSource;
		private readonly WebRTCConfiguration _config;
		private readonly ILogger _logger;
		private RTCPeerConnection _peerConnection;
		private IVideoSource _customVideoSource;
		private VideoTestPatternSource _testPatternSource;
		private CancellationTokenSource _cancellationTokenSource;
		private DateTime _createdAt;
		private long _framesSent;
		private Timer _frameTimer;

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

			_peerConnection.onicecandidate += (candidate) =>
			{
				_logger.LogDebug($"New ICE candidate for {_connectionId}: {candidate.candidate}");
			};

			_peerConnection.onconnectionstatechange += (state) =>
			{
				_logger.LogInformation($"Connection {_connectionId} state changed to: {state}");

				if (state == RTCPeerConnectionState.connected)
				{
					_logger.LogInformation($"WebRTC connected for {_connectionId} - starting video source");
					StartVideoSource();
				}
				else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
				{
					_logger.LogWarning($"WebRTC connection failed/closed for {_connectionId} - stopping video");
					StopVideoSource();
				}
			};

			// Add more detailed monitoring
			_peerConnection.oniceconnectionstatechange += (state) =>
			{
				_logger.LogInformation($"ICE connection state for {_connectionId}: {state}");
			};

			_peerConnection.onicegatheringstatechange += (state) =>
			{
				_logger.LogInformation($"ICE gathering state for {_connectionId}: {state}");
			};

			_logger.LogInformation($"Creating basic video track for {_connectionId}");
			
			var videoFormats = new List<VideoFormat>
			{
				new VideoFormat(VideoCodecsEnum.H264, 96), // H.264 is most widely supported
				new VideoFormat(VideoCodecsEnum.VP8, 97),  // VP8 as backup
			};

			var videoTrack = new MediaStreamTrack(videoFormats, MediaStreamStatusEnum.SendOnly);
			_peerConnection.addTrack(videoTrack);

			// Also create our custom source to read the real video (for monitoring)
			_customVideoSource = await CreateVideoSourceAsync();

			_logger.LogInformation($"WebRTC connection initialized for {_connectionId} - will send test pattern frames");
			_logger.LogInformation($"Real video source: {GetSourceType()}");
		}

		private async Task<IVideoSource> CreateVideoSourceAsync()
		{
			try
			{
				if (_streamSource.StartsWith("rtsp://") || _streamSource.StartsWith("http://"))
				{
					_logger.LogInformation($"Creating RTSP/HTTP stream source: {_streamSource}");
					return new FFmpegVideoStreamSource(_streamSource, _config.VideoFrameRate);
				}
				else if (_streamSource.StartsWith("test://"))
				{
					_logger.LogInformation("Creating test pattern source");
					return new TestPatternVideoSource(_config.VideoWidth, _config.VideoHeight, _config.VideoFrameRate);
				}
				else if (File.Exists(_streamSource))
				{
					_logger.LogInformation($"Creating video file source: {_streamSource}");
					return new FFmpegFileVideoSource(_streamSource, _config.VideoFrameRate);
				}
				else
				{
					_logger.LogWarning($"Stream source not recognized: {_streamSource}, using test pattern");
					return new TestPatternVideoSource(_config.VideoWidth, _config.VideoHeight, _config.VideoFrameRate);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to create video source for {_streamSource}, using test pattern");
				return new TestPatternVideoSource(_config.VideoWidth, _config.VideoHeight, _config.VideoFrameRate);
			}
		}

		private void StartVideoSource()
		{
			try
			{
				var sourceType = GetSourceType();
				_logger.LogInformation($"Starting video source for {_connectionId} - Type: {sourceType}, StreamSource: {_streamSource}");

				if (_customVideoSource != null)
				{
					_logger.LogInformation($"Starting real video streaming from {sourceType}");
					_customVideoSource.Start();
					
					var frameInterval = 1000 / _config.VideoFrameRate;
					_frameTimer = new Timer(SendRealVideoFrame, null, 0, frameInterval);
					_logger.LogInformation($"Frame timer started for {_connectionId}, interval: {frameInterval}ms");
				}
				else
				{
					_logger.LogError($"No video source available for {_connectionId} - Stream source: {_streamSource}");
					_logger.LogError($"Source type detected as: {sourceType}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to start video source for {_connectionId}");
			}
		}


		private void StopVideoSource()
		{
			_frameTimer?.Dispose();
			_frameTimer = null;
			
			// SIPSorcery test pattern source stops automatically when peer connection closes
			_customVideoSource?.Stop();
			
			_logger.LogInformation($"Stopped video sources for {_connectionId}");
		}

		private async void SendRealVideoFrame(object state)
		{
			if (_customVideoSource == null)
			{
				_logger.LogWarning($"Custom video source is null for {_connectionId}");
				return;
			}

			try
			{
				var frame = await _customVideoSource.GetNextFrameAsync();
				if (frame != null && frame.Data != null && frame.Data.Length > 0)
				{
					await SendVideoFrame(frame);
					_framesSent++;
				}
				else
				{
					if (_framesSent % 60 == 0) // Log when no frames are available
					{
						_logger.LogWarning($"No frame data available from video source for {_connectionId}");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error sending real video frame for {_connectionId}: {ex.Message}");
			}
		}

		private async Task SendVideoFrame(VideoFrame frame)
		{
			try
			{
				var timestamp = (uint)(DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds * 90);

				if (frame.IsPreEncoded)
				{
					// Frame is already H.264 encoded (from video file)
					_peerConnection.SendVideo(timestamp, frame.Data);
				}
				else
				{
					// Raw frame from RTSP - need proper encoding
					// For now, skip raw frames until we implement proper encoding
					_logger.LogWarning($"Skipping raw frame - need to implement proper H.264 encoding for RTSP");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error sending video frame for {_connectionId}: {ex.Message}");
			}
		}

		private VideoPixelFormatsEnum ConvertToSIPSorceryFormat(VideoPixelFormatsEnum format)
		{
			return format switch
			{
				VideoPixelFormatsEnum.Bgr => VideoPixelFormatsEnum.Bgr,
				VideoPixelFormatsEnum.Rgb => VideoPixelFormatsEnum.Rgb,
				_ => VideoPixelFormatsEnum.Bgr
			};
		}

		public RTCSessionDescriptionInit CreateOfferAsync()
		{
			if (_peerConnection == null)
				throw new InvalidOperationException("Peer connection not initialized");

			var offer = _peerConnection.createOffer(new RTCOfferOptions());
			_peerConnection.setLocalDescription(offer);

			_logger.LogInformation($"Created offer for {_connectionId}");
			return offer;
		}

		public bool SetAnswerAsync(RTCSessionDescriptionInit answer)
		{
			try
			{
				if (_peerConnection == null)
					return false;

				var result = _peerConnection.setRemoteDescription(answer);
				_logger.LogInformation($"Set remote description for {_connectionId}");
				return result == SetDescriptionResultEnum.OK;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to set answer for {_connectionId}: {ex.Message}");
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
				["sourceType"] = GetSourceType()
			};
		}

		private string GetSourceType()
		{
			if (_streamSource.StartsWith("rtsp://") || _streamSource.StartsWith("http://"))
				return "RtspStream";
			else if (File.Exists(_streamSource))
				return "VideoFile";
			else if (_streamSource.StartsWith("test://"))
				return "TestPattern";
			else
				return "Unknown";
		}

		public async Task CloseAsync()
		{
			_logger.LogInformation($"Closing WebRTC connection {_connectionId}");

			StopVideoSource();

			_cancellationTokenSource?.Cancel();

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

			_customVideoSource?.Dispose();
			_testPatternSource?.Dispose();
			_peerConnection?.Dispose();
			_cancellationTokenSource?.Dispose();
		}
	}
}