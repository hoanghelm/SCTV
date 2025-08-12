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
			};

			// For now, use a simple test pattern source that works with SIPSorcery
			// This follows the pattern from the search results
			var encoder = new FFmpegVideoEncoder();
			_testPatternSource = new VideoTestPatternSource(encoder);
			_testPatternSource.RestrictFormats(format => format.Codec == _config.VideoCodec);

			// Create video track
			var videoTrack = new MediaStreamTrack(_testPatternSource.GetVideoSourceFormats(), MediaStreamStatusEnum.SendOnly);

			// Hook up the video encoding - this is the correct event from SIPSorcery
			_testPatternSource.OnVideoSourceEncodedSample += _peerConnection.SendVideo;

			_peerConnection.addTrack(videoTrack);

			// Also initialize our custom source for future enhancement
			_customVideoSource = await CreateVideoSourceAsync();

			// Start the test pattern for now
			await _testPatternSource.StartVideo();
		}

		private async Task<IVideoSource> CreateVideoSourceAsync()
		{
			if (_streamSource.StartsWith("rtsp://") || _streamSource.StartsWith("http://"))
			{
				return new FFmpegVideoStreamSource(_streamSource, _config.VideoFrameRate);
			}
			else if (_streamSource.StartsWith("test://"))
			{
				return new TestPatternVideoSource(_config.VideoWidth, _config.VideoHeight, _config.VideoFrameRate);
			}
			else if (File.Exists(_streamSource))
			{
				return new FFmpegFileVideoSource(_streamSource, _config.VideoFrameRate);
			}
			else
			{
				throw new NotSupportedException($"Stream source type not supported: {_streamSource}");
			}
		}

		public async Task<RTCSessionDescriptionInit> CreateOfferAsync()
		{
			var offer = _peerConnection.createOffer(new RTCOfferOptions());
			await _peerConnection.setLocalDescription(offer);
			return offer;
		}

		public async Task<bool> SetAnswerAsync(RTCSessionDescriptionInit answer)
		{
			try
			{
				var result = _peerConnection.setRemoteDescription(answer);
				return result == SetDescriptionResultEnum.OK;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to set answer for connection {_connectionId}");
				return false;
			}
		}

		public async Task<bool> AddIceCandidateAsync(RTCIceCandidateInit candidate)
		{
			try
			{
				_peerConnection.addIceCandidate(candidate);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to add ICE candidate for connection {_connectionId}");
				return false;
			}
		}

		public Dictionary<string, object> GetStats()
		{
			return new Dictionary<string, object>
			{
				["connectionId"] = _connectionId,
				["state"] = _peerConnection?.connectionState.ToString(),
				["createdAt"] = _createdAt,
				["framesSent"] = _framesSent,
				["duration"] = DateTime.UtcNow - _createdAt
			};
		}

		public async Task CloseAsync()
		{
			_cancellationTokenSource?.Cancel();
			_customVideoSource?.Stop();
			await _testPatternSource?.CloseVideo();
			_peerConnection?.close();
		}

		public void Dispose()
		{
			CloseAsync().Wait();
			_cancellationTokenSource?.Dispose();
			_customVideoSource?.Dispose();
			_testPatternSource?.Dispose();
			_peerConnection?.Dispose();
		}
	}
}