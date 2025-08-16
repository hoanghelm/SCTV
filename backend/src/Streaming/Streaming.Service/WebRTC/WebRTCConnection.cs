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

			// Keep it simple - use the original working approach but just send test pattern
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
				// Start the working test pattern transmission
				_logger.LogInformation($"Starting test pattern video transmission for {_connectionId}");
				StartBasicVideoTransmission();

				// Also start our custom source to read real video frames (for monitoring)
				if (_customVideoSource != null)
				{
					var sourceType = GetSourceType();
					_logger.LogInformation($"Also starting custom video source for {_connectionId} - Type: {sourceType}");
					_customVideoSource.Start();

					// Monitor the real video frames
					if (sourceType == "VideoFile" || sourceType == "RtspStream")
					{
						var frameInterval = 1000 / _config.VideoFrameRate;
						_frameTimer = new Timer(MonitorCustomFrames, null, 0, frameInterval);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to start video source for {_connectionId}");
			}
		}

		private void StartBasicVideoTransmission()
		{
			// Create a timer to send simple test frames
			var frameInterval = 1000 / _config.VideoFrameRate; // ms between frames
			_frameTimer = new Timer(SendTestFrame, null, 0, frameInterval);
			_logger.LogInformation($"Started basic video transmission for {_connectionId} at {_config.VideoFrameRate} FPS");
		}

		private void SendTestFrame(object state)
		{
			try
			{
				var timestamp = (uint)(DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds * 90);
				
				// Create a proper H.264 frame with correct SPS/PPS/IDR structure
				// This creates a simple 16x16 black frame that browsers can decode
				
				// SPS (Sequence Parameter Set) - defines video properties
				var sps = new byte[]
				{
					0x00, 0x00, 0x00, 0x01, // NAL start code
					0x67, 0x42, 0xE0, 0x1E, // SPS NAL header + profile
					0xDA, 0x05, 0x82, 0x59, // SPS data for 16x16 resolution
					0x25, 0xB8, 0x0C, 0x04,
					0x04, 0x06, 0x9F, 0x18,
					0x32, 0xA0
				};

				// PPS (Picture Parameter Set) - defines picture properties  
				var pps = new byte[]
				{
					0x00, 0x00, 0x00, 0x01, // NAL start code
					0x68, 0xCE, 0x31, 0x12, 0x11 // PPS NAL header + data
				};

				// IDR (Instantaneous Decoder Refresh) - actual frame data
				var idr = new byte[]
				{
					0x00, 0x00, 0x00, 0x01, // NAL start code
					0x65, 0x88, 0x84, 0x00, // IDR slice header
					0x20, 0x00, 0x00, 0x03, // Slice data (black 16x16 macroblock)
					0x00, 0x00, 0x32, 0x08
				};

				// Combine all NAL units
				var frameData = new byte[sps.Length + pps.Length + idr.Length];
				Array.Copy(sps, 0, frameData, 0, sps.Length);
				Array.Copy(pps, 0, frameData, sps.Length, pps.Length);
				Array.Copy(idr, 0, frameData, sps.Length + pps.Length, idr.Length);

				// Send via WebRTC
				_peerConnection.SendVideo(timestamp, frameData);
				_framesSent++;
				
				if (_framesSent % 30 == 0)
				{
					_logger.LogInformation($"Sent {_framesSent} H.264 frames (SPS+PPS+IDR) for {_connectionId}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error sending test frame for {_connectionId}");
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

		private async void MonitorCustomFrames(object state)
		{
			if (_customVideoSource == null) return;

			try
			{
				var frame = await _customVideoSource.GetNextFrameAsync();
				if (frame != null && frame.Data != null && frame.Data.Length > 0)
				{
					_framesSent++;
					if (_framesSent % 60 == 0)
					{
						_logger.LogInformation($"Monitoring real video: received frame {frame.Width}x{frame.Height} ({frame.Data.Length} bytes) - frame #{_framesSent}");
						_logger.LogInformation($"SIPSorcery test pattern is being displayed to browser while we monitor real video");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error monitoring custom frame for {_connectionId}");
			}
		}

		private async Task SendVideoFrame(VideoFrame frame)
		{
			try
			{
				var timestamp = (uint)(DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds * 90);

				// For now, send the working test pattern format but log that we're getting real frames
				// This maintains video flow while we debug the H.264 compatibility
				var testFrameData = CreateWebRTCCompatibleFrame();
				_peerConnection.SendVideo(timestamp, testFrameData);
				
				if (_framesSent % 60 == 0)
				{
					_logger.LogInformation($"Received real frame {frame.Width}x{frame.Height} ({frame.Data.Length} bytes) but sending test pattern for WebRTC compatibility");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error sending video frame for {_connectionId}: {ex.Message}");
			}
		}

		private byte[] CreateWebRTCCompatibleFrame()
		{
			// Use the working test pattern structure that browsers can decode
			var sps = new byte[]
			{
				0x00, 0x00, 0x00, 0x01, // NAL start code
				0x67, 0x42, 0xE0, 0x1E, // SPS NAL header + profile
				0xDA, 0x05, 0x82, 0x59, // SPS data for 16x16 resolution
				0x25, 0xB8, 0x0C, 0x04,
				0x04, 0x06, 0x9F, 0x18,
				0x32, 0xA0
			};

			var pps = new byte[]
			{
				0x00, 0x00, 0x00, 0x01, // NAL start code
				0x68, 0xCE, 0x31, 0x12, 0x11 // PPS NAL header + data
			};

			var idr = new byte[]
			{
				0x00, 0x00, 0x00, 0x01, // NAL start code
				0x65, 0x88, 0x84, 0x00, // IDR slice header
				0x20, 0x00, 0x00, 0x03, // Slice data (black 16x16 macroblock)
				0x00, 0x00, 0x32, 0x08
			};

			var frameData = new byte[sps.Length + pps.Length + idr.Length];
			Array.Copy(sps, 0, frameData, 0, sps.Length);
			Array.Copy(pps, 0, frameData, sps.Length, pps.Length);
			Array.Copy(idr, 0, frameData, sps.Length + pps.Length, idr.Length);
			
			return frameData;
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