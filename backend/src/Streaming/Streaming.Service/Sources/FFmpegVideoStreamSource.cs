using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using Streaming.Service.Models;
using IVideoSource = Streaming.Service.Models.IVideoSource;

namespace Streaming.Service.Sources
{
	/// <summary>
	/// FFmpeg-based video source for streaming RTSP/HTTP sources
	/// </summary>
	public class FFmpegVideoStreamSource : IVideoSource
	{
		private readonly string _streamUrl;
		private readonly int _frameRate;
		private FFmpegVideoEndPoint _ffmpegEndpoint;
		private bool _isRunning;
		private bool _isDisposed;

		public FFmpegVideoStreamSource(string streamUrl, int frameRate)
		{
			_streamUrl = streamUrl;
			_frameRate = frameRate;
		}

		public async Task<VideoFrame> GetNextFrameAsync()
		{
			if (!_isRunning || _ffmpegEndpoint == null || _isDisposed)
				return null;

			try
			{
				// Wait for frame interval
				await Task.Delay(1000 / _frameRate);

				// For now, return a placeholder frame
				// In a real implementation, you would get the actual frame from FFmpeg
				var frameSize = 1280 * 720 * 3; // Assuming RGB24 format
				var frameData = new byte[frameSize];

				return new VideoFrame
				{
					Data = frameData,
					Width = 1280,
					Height = 720,
					Format = VideoPixelFormatsEnum.Rgb,
					Duration = 1000 / _frameRate
				};
			}
			catch (Exception)
			{
				return null;
			}
		}

		public void Start()
		{
			if (_isDisposed) return;

			try
			{
				_ffmpegEndpoint = new FFmpegVideoEndPoint();

				// Configure FFmpeg endpoint for the stream
				_ffmpegEndpoint.RestrictFormats(format =>
					format.Codec == VideoCodecsEnum.VP8 ||
					format.Codec == VideoCodecsEnum.H264);

				_isRunning = true;
			}
			catch (Exception)
			{
				_isRunning = false;
				throw;
			}
		}

		public void Stop()
		{
			_isRunning = false;
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_isDisposed = true;
			Stop();
			_ffmpegEndpoint?.Dispose();
		}
	}

	/// <summary>
	/// FFmpeg-based video source for file playback
	/// </summary>
	public class FFmpegFileVideoSource : IVideoSource
	{
		private readonly string _filePath;
		private readonly int _frameRate;
		private FFmpegVideoEndPoint _ffmpegEndpoint;
		private bool _isRunning;
		private bool _isDisposed;

		public FFmpegFileVideoSource(string filePath, int frameRate)
		{
			_filePath = filePath;
			_frameRate = frameRate;
		}

		public async Task<VideoFrame> GetNextFrameAsync()
		{
			if (!_isRunning || _ffmpegEndpoint == null || _isDisposed)
				return null;

			try
			{
				// Wait for frame interval
				await Task.Delay(1000 / _frameRate);

				// For now, return a placeholder frame
				// In a real implementation, you would get the actual frame from the file
				var frameSize = 1280 * 720 * 3; // Assuming RGB24 format
				var frameData = new byte[frameSize];

				return new VideoFrame
				{
					Data = frameData,
					Width = 1280,
					Height = 720,
					Format = VideoPixelFormatsEnum.Rgb,
					Duration = 1000 / _frameRate
				};
			}
			catch (Exception)
			{
				return null;
			}
		}

		public void Start()
		{
			if (_isDisposed) return;

			try
			{
				if (!File.Exists(_filePath))
					throw new FileNotFoundException($"Video file not found: {_filePath}");

				_ffmpegEndpoint = new FFmpegVideoEndPoint();

				// Configure FFmpeg endpoint for file playback
				_ffmpegEndpoint.RestrictFormats(format =>
					format.Codec == VideoCodecsEnum.VP8 ||
					format.Codec == VideoCodecsEnum.H264);

				_isRunning = true;
			}
			catch (Exception)
			{
				_isRunning = false;
				throw;
			}
		}

		public void Stop()
		{
			_isRunning = false;
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_isDisposed = true;
			Stop();
			_ffmpegEndpoint?.Dispose();
		}
	}
}
