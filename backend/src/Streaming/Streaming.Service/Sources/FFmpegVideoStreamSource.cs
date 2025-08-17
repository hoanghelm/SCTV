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
		private readonly int _targetWidth;
		private readonly int _targetHeight;
		
		private Process _ffmpegProcess;
		private Stream _ffmpegOutput;
		private bool _isRunning;
		private bool _isDisposed;
		private byte[] _frameBuffer;
		private int _frameSize;

		public FFmpegVideoStreamSource(string streamUrl, int frameRate, int width = 1280, int height = 720)
		{
			_streamUrl = streamUrl;
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
				// Read a complete frame from FFmpeg (fixed size for raw BGR24)
				var totalBytesRead = 0;
				var readBuffer = new byte[_frameSize];
				
				while (totalBytesRead < _frameSize)
				{
					var bytesRead = await _ffmpegOutput.ReadAsync(
						readBuffer,
						totalBytesRead,
						_frameSize - totalBytesRead);

					if (bytesRead == 0)
					{
						// Connection lost or stream ended
						Console.WriteLine($"RTSP stream ended or connection lost: {_streamUrl}");
						return null;
					}

					totalBytesRead += bytesRead;
				}

				// Create a copy of the frame data
				var frameData = new byte[_frameSize];
				Array.Copy(readBuffer, frameData, _frameSize);

				return new VideoFrame
				{
					Data = frameData,
					Width = _targetWidth,
					Height = _targetHeight,
					Format = VideoPixelFormatsEnum.Bgr, // FFmpeg outputs BGR24
					Duration = 1000 / _frameRate,
					IsPreEncoded = false // Raw frame data for SIPSorcery to encode
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading RTSP frame from {_streamUrl}: {ex.Message}");
				return null;
			}
		}

		public void Start()
		{
			if (_isDisposed) return;

			try
			{
				StartFFmpegProcess();
				_isRunning = true;

				Console.WriteLine($"Started RTSP stream: {_streamUrl}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to start RTSP stream {_streamUrl}: {ex.Message}");
				_isRunning = false;
				throw;
			}
		}

		private void StartFFmpegProcess()
		{
			// FFmpeg command to read RTSP stream and output raw BGR24 frames for SIPSorcery
			// We go back to raw frames since SIPSorcery needs to handle the encoding pipeline
			var ffmpegArgs = $"-i \"{_streamUrl}\" " +
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
					Console.WriteLine($"FFmpeg RTSP: {e.Data}");
				}
			};

			_ffmpegProcess.Start();
			_ffmpegProcess.BeginErrorReadLine();

			_ffmpegOutput = _ffmpegProcess.StandardOutput.BaseStream;

			Console.WriteLine($"FFmpeg RTSP process started with PID: {_ffmpegProcess.Id}");
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
				Console.WriteLine($"Error stopping FFmpeg RTSP process: {ex.Message}");
			}
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_isDisposed = true;
			Stop();

			Console.WriteLine($"Disposed FFmpeg RTSP source for: {_streamUrl}");
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
				var readBuffer = new byte[8192]; // 8KB chunks
				var bytesRead = await _ffmpegOutput.ReadAsync(readBuffer, 0, readBuffer.Length);
				
				if (bytesRead == 0)
				{
					// End of file, restart
					await RestartVideo();
					return null;
				}

				_frameCount++;

				// Trim the buffer to actual bytes read
				var frameData = new byte[bytesRead];
				Array.Copy(readBuffer, frameData, bytesRead);
				
				return new VideoFrame
				{
					Data = frameData,
					Width = _targetWidth,
					Height = _targetHeight,
					Format = VideoPixelFormatsEnum.Bgr, // Not used for H.264
					Duration = 1000 / _frameRate,
					IsPreEncoded = true
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading H.264 data: {ex.Message}");
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
			// FFmpeg command to read video file and output WebRTC-compatible H.264
			var ffmpegArgs = $"-re -stream_loop -1 -i \"{_filePath}\" " +
						   $"-c:v libx264 -preset ultrafast -tune zerolatency " +
						   $"-profile:v baseline -level 3.1 " +
						   $"-pix_fmt yuv420p " +
						   $"-s {_targetWidth}x{_targetHeight} " +
						   $"-r {_frameRate} " +
						   $"-g {_frameRate} " + // GOP size = frame rate for frequent keyframes
						   $"-keyint_min {_frameRate} " + // Force keyframes regularly
						   $"-force_key_frames expr:gte(t,n_forced*1) " + // Force keyframes every second
						   $"-bsf:v h264_mp4toannexb " + // Convert to Annex B format for WebRTC
						   $"-f h264 " +
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