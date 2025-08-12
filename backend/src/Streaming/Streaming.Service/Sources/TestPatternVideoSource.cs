using SIPSorceryMedia.Abstractions;
using System.Drawing;
using Streaming.Service.Models;
using IVideoSource = Streaming.Service.Models.IVideoSource;
using Font = System.Drawing.Font;
using System.Drawing.Imaging;

namespace Streaming.Service.Sources
{
	public class TestPatternVideoSource : IVideoSource
	{
		private readonly int _width;
		private readonly int _height;
		private readonly int _frameRate;
		private bool _isRunning;
		private int _frameCount;

		public TestPatternVideoSource(int width, int height, int frameRate)
		{
			_width = width;
			_height = height;
			_frameRate = frameRate;
		}

		public async Task<VideoFrame> GetNextFrameAsync()
		{
			if (!_isRunning) return null;

			using (var bitmap = new Bitmap(_width, _height))
			using (var g = Graphics.FromImage(bitmap))
			{
				// Draw test pattern
				g.Clear(Color.Black);

				// Draw grid
				var gridSize = 40;
				using (var pen = new Pen(Color.Green, 1))
				{
					for (int x = 0; x < _width; x += gridSize)
						g.DrawLine(pen, x, 0, x, _height);
					for (int y = 0; y < _height; y += gridSize)
						g.DrawLine(pen, 0, y, _width, y);
				}

				// Draw frame counter
				var text = $"Frame: {_frameCount++}";
				var font = new Font("Arial", 24);
				var brush = new SolidBrush(Color.White);
				g.DrawString(text, font, brush, 10, 10);

				// Draw timestamp
				var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
				g.DrawString(timestamp, font, brush, 10, 50);

				// Convert to byte array
				using (var ms = new MemoryStream())
				{
					bitmap.Save(ms, ImageFormat.Bmp);
					return new VideoFrame
					{
						Data = ms.ToArray(),
						Width = _width,
						Height = _height,
						Format = VideoPixelFormatsEnum.Bgr,
						Duration = 1000 / _frameRate
					};
				}
			}
		}

		public void Start() => _isRunning = true;
		public void Stop() => _isRunning = false;
		public void Dispose() => Stop();
	}
}
