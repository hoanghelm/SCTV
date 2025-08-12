using System;

namespace Streaming.Service.ViewModels
{
	public class CameraViewModel
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string StreamUrl { get; set; }
		public string Status { get; set; }
		public string Location { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string CameraType { get; set; }
		public string Brand { get; set; }
		public string Model { get; set; }
		public string Resolution { get; set; }
		public bool HasAudio { get; set; }
		public bool HasPTZ { get; set; }
		public bool HasNightVision { get; set; }
		public bool HasMotionDetection { get; set; }
		public DateTime? LastPingAt { get; set; }
		public bool IsOnline { get; set; }
		public DateTime? CreatedAt { get; set; }
	}
}