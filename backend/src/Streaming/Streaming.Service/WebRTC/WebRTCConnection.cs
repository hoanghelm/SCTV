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
					StartCustomVideoSource();
				}
				else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
				{
					StopCustomVideoSource();
				}
			};

			// Use SIPSorcery's VideoTestPatternSource as the base (always works)
			var encoder = new FFmpegVideoEncoder();
			_testPatternSource = new VideoTestPatternSource(encoder);
			_testPatternSource.RestrictFormats(format => format.Codec == _config.VideoCodec);

			// Create video track
			var videoTrack = new MediaStreamTrack(_testPatternSource.GetVideoSourceFormats(), MediaStreamStatusEnum.SendOnly);

			// Hook up the video encoding - this is the working pattern from your original code
			_testPatternSource.OnVideoSourceEncodedSample += (uint durationRtpUnits, byte[] sample) =>
			{
				_peerConnection.SendVideo(durationRtpUnits, sample);
				_framesSent++;

				if (_framesSent % 60 == 0)
				{
					_logger.LogInformation($"Sent {_framesSent} video frames for connection {_connectionId}");
				}
			};

			_peerConnection.addTrack(videoTrack);

			// Initialize our custom source for future enhancement
			_customVideoSource = await CreateVideoSourceAsync();

			// Start the test pattern - this ensures video is always working
			await _testPatternSource.StartVideo();

			_logger.LogInformation($"WebRTC connection initialized for {_connectionId} with source type: {GetSourceType()}");
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

		private void StartCustomVideoSource()
		{
			if (_customVideoSource == null) return;

			try
			{
				// For file sources and custom test patterns, start frame generation
				if (GetSourceType() == "VideoFile" || GetSourceType() == "TestPattern")
				{
					_logger.LogInformation($"Starting custom video source for {_connectionId}");
					_customVideoSource.Start();

					// Start frame processing timer
					var frameInterval = 1000 / _config.VideoFrameRate;
					_frameTimer = new Timer(ProcessCustomFrame, null, 0, frameInterval);
				}
				else if (GetSourceType() == "RtspStream")
				{
					_logger.LogInformation($"Starting RTSP stream for {_connectionId}");
					_customVideoSource.Start();
					// RTSP streams handle their own timing
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to start custom video source for {_connectionId}");
			}
		}

		private void StopCustomVideoSource()
		{
			_frameTimer?.Dispose();
			_frameTimer = null;
			_customVideoSource?.Stop();
			_logger.LogInformation($"Stopped custom video source for {_connectionId}");
		}

		private async void ProcessCustomFrame(object state)
		{
			if (_customVideoSource == null) return;

			try
			{
				var frame = await _customVideoSource.GetNextFrameAsync();
				if (frame != null)
				{
					// For now, we let the SIPSorcery test pattern handle the encoding
					// In the future, you could process the custom frame data here
					// and feed it to the test pattern source

					// This is where you'd convert your custom frame to SIPSorcery format
					// For now, the test pattern continues to work as fallback
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error processing custom frame for {_connectionId}");
			}
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

			StopCustomVideoSource();

			_cancellationTokenSource?.Cancel();
			_customVideoSource?.Stop();

			if (_testPatternSource != null)
			{
				await _testPatternSource.CloseVideo();
			}

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