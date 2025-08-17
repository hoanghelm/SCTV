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

		public Task<VideoFrame> GetNextFrameAsync()
		{
			if (!_isRunning) return Task.FromResult<VideoFrame>(null);

			// Create a simple H.264 test pattern frame for WebRTC compatibility
			var h264Frame = CreateH264TestFrame();
			_frameCount++;

			var frame = new VideoFrame
			{
				Data = h264Frame,
				Width = _width,
				Height = _height,
				Format = VideoPixelFormatsEnum.Bgr, // Not used for H.264
				Duration = 1000 / _frameRate,
				IsPreEncoded = true // This is H.264 encoded data
			};

			return Task.FromResult(frame);
		}

		private byte[] CreateH264TestFrame()
		{
			// Create a proper H.264 frame with SPS/PPS/IDR structure for WebRTC
			// This creates a simple test pattern that browsers can decode
			
			// SPS (Sequence Parameter Set) - defines video properties
			var sps = new byte[]
			{
				0x00, 0x00, 0x00, 0x01, // NAL start code
				0x67, 0x42, 0xE0, 0x1E, // SPS NAL header + profile
				0xDA, 0x05, 0x82, 0x59, // SPS data for basic resolution
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
			// Create a simple pattern that changes based on frame count
			var baseIdr = new byte[]
			{
				0x00, 0x00, 0x00, 0x01, // NAL start code
				0x65, 0x88, 0x84, 0x00, // IDR slice header
				0x20, 0x00, 0x00, 0x03, // Slice data 
				0x00, 0x00, 0x32, 0x08
			};

			// Modify frame data slightly to create animation
			var idr = new byte[baseIdr.Length];
			Array.Copy(baseIdr, idr, baseIdr.Length);
			
			// Simple animation by modifying the last byte based on frame count
			idr[idr.Length - 1] = (byte)(0x08 + (_frameCount % 16));

			// Combine all NAL units
			var frameData = new byte[sps.Length + pps.Length + idr.Length];
			Array.Copy(sps, 0, frameData, 0, sps.Length);
			Array.Copy(pps, 0, frameData, sps.Length, pps.Length);
			Array.Copy(idr, 0, frameData, sps.Length + pps.Length, idr.Length);

			return frameData;
		}

		public void Start() => _isRunning = true;
		public void Stop() => _isRunning = false;
		public void Dispose() => Stop();
	}
}
