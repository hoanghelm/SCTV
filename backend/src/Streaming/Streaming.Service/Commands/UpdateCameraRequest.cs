using Common.ApiResponse;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace Streaming.Service.Commands
{
	public class UpdateCameraRequest : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }

		[MaxLength(100)]
		public string Name { get; set; }

		[MaxLength(500)]
		public string Description { get; set; }

		[MaxLength(500)]
		public string StreamUrl { get; set; }

		[MaxLength(100)]
		public string Location { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }

		[MaxLength(50)]
		public string CameraType { get; set; }

		[MaxLength(100)]
		public string Brand { get; set; }

		[MaxLength(100)]
		public string Model { get; set; }

		[MaxLength(20)]
		public string Resolution { get; set; }

		public int? FrameRate { get; set; }
		public bool? HasAudio { get; set; }
		public bool? HasPTZ { get; set; }
		public bool? HasNightVision { get; set; }
		public bool? HasMotionDetection { get; set; }

		[MaxLength(50)]
		public string Status { get; set; }

		// Audit fields
		public string UpdatedBy { get; set; }
	}
}