using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using Streaming.Service.Models;
using Streaming.Service.Tests;
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
		private MediaStreamTrack _videoTrack;
		private IVideoSource _videoSource;
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

			_videoSource = await CreateVideoSourceAsync();

			// Create video track
			var videoFormat = new VideoFormat(
				_config.VideoCodec,
				_config.VideoWidth,
				_config.VideoHeight,
				_config.VideoFrameRate
			);

			_videoTrack = new MediaStreamTrack(
				SDPMediaTypesEnum.video,
				false,
				new List<SDPAudioVideoMediaFormat> { new SDPAudioVideoMediaFormat(videoFormat) },
				MediaStreamStatusEnum.SendOnly
			);

			_videoTrack.OnVideoSourceEncodedSample += _peerConnection.SendVideo;

			_peerConnection.addTrack(_videoTrack);

			StartVideoStream();
		}

		private async Task<IVideoSource> CreateVideoSourceAsync()
		{
			if (_streamSource.StartsWith("rtsp://") || _streamSource.StartsWith("http://"))
			{
				var ffmpegSource = new FFmpegVideoStreamSource(_streamSource, _config.VideoFrameRate);
				return ffmpegSource;
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

		private void StartVideoStream()
		{
			Task.Run(async () =>
			{
				var frameInterval = 1000 / _config.VideoFrameRate;

				while (!_cancellationTokenSource.Token.IsCancellationRequested)
				{
					try
					{
						var frame = await _videoSource.GetNextFrameAsync();
						if (frame != null)
						{
							_videoTrack.ExternalVideoSourceRawSample(
								(uint)frame.Duration,
								frame.Width,
								frame.Height,
								frame.Data,
								frame.Format
							);
							_framesSent++;
						}

						await Task.Delay(frameInterval, _cancellationTokenSource.Token);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, $"Error in video stream for connection {_connectionId}");
					}
				}
			}, _cancellationTokenSource.Token);
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
			_videoSource?.Stop();
			_peerConnection?.close();
		}

		public void Dispose()
		{
			CloseAsync().Wait();
			_cancellationTokenSource?.Dispose();
			_videoSource?.Dispose();
			_peerConnection?.Dispose();
		}
	}
}
