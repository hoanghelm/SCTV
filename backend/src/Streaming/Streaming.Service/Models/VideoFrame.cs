using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Service.Models
{
	public class VideoFrame
	{
		public byte[] Data { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public VideoPixelFormatsEnum Format { get; set; }
		public int Duration { get; set; }
	}
	public interface IVideoSource : IDisposable
	{
		Task<VideoFrame> GetNextFrameAsync();
		void Start();
		void Stop();
	}
}
