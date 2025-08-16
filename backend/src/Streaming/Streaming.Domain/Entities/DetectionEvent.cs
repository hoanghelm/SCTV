using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public class DetectionEvent : IBaseEntity<Guid>, ICreatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		public Guid CameraId { get; set; }

		[Required]
		[MaxLength(50)]
		public string EventType { get; set; } // PersonDetected, MotionDetected, etc.

		public DateTime EventTimestamp { get; set; }

		[MaxLength(2000)]
		public string DetectionData { get; set; } // JSON data with bounding boxes, confidence, etc.

		[MaxLength(500)]
		public string? FrameImageUrl { get; set; }

		[MaxLength(500)]
		public string? VideoClipUrl { get; set; }

		public double Confidence { get; set; }

		[MaxLength(1000)]
		public string Metadata { get; set; } // Additional metadata as JSON

		public bool AlertTriggered { get; set; }

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string? CreatedBy { get; set; }

		// Navigation properties
		public virtual Camera Camera { get; set; }
	}
}
