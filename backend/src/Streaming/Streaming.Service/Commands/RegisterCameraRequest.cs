using Common.ApiResponse;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace Streaming.Service.Commands
{
	public class RegisterCameraRequest : IRequest<ApiResult>
	{
		[Required]
		[MaxLength(100)]
		public string Name { get; set; }

		[MaxLength(500)]
		public string Description { get; set; }

		[Required]
		[MaxLength(500)]
		public string StreamUrl { get; set; }

		[MaxLength(100)]
		public string Location { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }

		[MaxLength(50)]
		public string CameraType { get; set; } = "Fixed";

		[MaxLength(100)]
		public string Brand { get; set; }

		[MaxLength(100)]
		public string Model { get; set; }

		[MaxLength(20)]
		public string Resolution { get; set; } = "1280x720";

		public int FrameRate { get; set; } = 30;
		public bool HasAudio { get; set; } = false;
		public bool HasPTZ { get; set; } = false;
		public bool HasNightVision { get; set; } = false;
		public bool HasMotionDetection { get; set; } = true;
		public bool TestMode { get; set; } = false;

		// Audit fields
		public string CreatedBy { get; set; }
	}
}