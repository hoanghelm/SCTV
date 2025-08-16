using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using Streaming.Service.Models;
using System.Diagnostics;
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

				// For RTSP streams, you'd integrate with FFmpeg here
				// This is a placeholder for now
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
		private readonly int _targetWidth;
		private readonly int _targetHeight;

		private Process _ffmpegProcess;
		private Stream _ffmpegOutput;
		private bool _isRunning;
		private bool _isDisposed;
		private long _frameCount;
		private byte[] _frameBuffer;
		private int _frameSize;

		public FFmpegFileVideoSource(string filePath, int frameRate, int width = 1280, int height = 720)
		{
			_filePath = filePath;
			_frameRate = frameRate;
			_targetWidth = width;
			_targetHeight = height;
			_frameSize = width * height * 3; // BGR24 = 3 bytes per pixel
			_frameBuffer = new byte[_frameSize];
		}

		public async Task<VideoFrame> GetNextFrameAsync()
		{
			if (!_isRunning || _ffmpegOutput == null || _isDisposed)
				return null;

			try
			{
				// Read a complete frame from FFmpeg
				var totalBytesRead = 0;
				while (totalBytesRead < _frameSize)
				{
					var bytesRead = await _ffmpegOutput.ReadAsync(
						_frameBuffer,
						totalBytesRead,
						_frameSize - totalBytesRead);

					if (bytesRead == 0)
					{
						// End of file reached, restart the video (loop)
						await RestartVideo();
						continue;
					}

					totalBytesRead += bytesRead;
				}

				_frameCount++;

				// Create a copy of the frame data
				var frameData = new byte[_frameSize];
				Array.Copy(_frameBuffer, frameData, _frameSize);

				return new VideoFrame
				{
					Data = frameData,
					Width = _targetWidth,
					Height = _targetHeight,
					Format = VideoPixelFormatsEnum.Bgr, // FFmpeg outputs BGR24
					Duration = 1000 / _frameRate
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading video frame: {ex.Message}");
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

				StartFFmpegProcess();
				_isRunning = true;

				Console.WriteLine($"Started video file playback: {_filePath}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to start video file: {ex.Message}");
				_isRunning = false;
				throw;
			}
		}

		private void StartFFmpegProcess()
		{
			// FFmpeg command to read video file and output raw BGR24 frames
			var ffmpegArgs = $"-re -stream_loop -1 -i \"{_filePath}\" " +
						   $"-f rawvideo -pix_fmt bgr24 " +
						   $"-s {_targetWidth}x{_targetHeight} " +
						   $"-r {_frameRate} " +
						   $"-an -"; // -an = no audio, - = output to stdout

			_ffmpegProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "ffmpeg",
					Arguments = ffmpegArgs,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = null // Important for binary data
				}
			};

			// Log FFmpeg errors for debugging
			_ffmpegProcess.ErrorDataReceived += (sender, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					Console.WriteLine($"FFmpeg: {e.Data}");
				}
			};

			_ffmpegProcess.Start();
			_ffmpegProcess.BeginErrorReadLine();

			_ffmpegOutput = _ffmpegProcess.StandardOutput.BaseStream;

			Console.WriteLine($"FFmpeg process started with PID: {_ffmpegProcess.Id}");
		}

		private async Task RestartVideo()
		{
			try
			{
				Console.WriteLine("Restarting video playback (loop)");

				// Stop current process
				StopFFmpegProcess();

				// Small delay to ensure clean restart
				await Task.Delay(100);

				// Start new process
				if (!_isDisposed && _isRunning)
				{
					StartFFmpegProcess();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error restarting video: {ex.Message}");
			}
		}

		public void Stop()
		{
			_isRunning = false;
			StopFFmpegProcess();
		}

		private void StopFFmpegProcess()
		{
			try
			{
				_ffmpegOutput?.Close();
				_ffmpegOutput = null;

				if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
				{
					_ffmpegProcess.Kill();
					_ffmpegProcess.WaitForExit(1000);
				}

				_ffmpegProcess?.Dispose();
				_ffmpegProcess = null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error stopping FFmpeg process: {ex.Message}");
			}
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_isDisposed = true;
			Stop();

			Console.WriteLine($"Disposed FFmpeg file source for: {_filePath}");
		}
	}
}