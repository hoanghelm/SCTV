using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Streaming.Domain.Entities
{
	public class Camera : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; }

		[MaxLength(500)]
		public string Description { get; set; }

		[Required]
		[MaxLength(500)]
		public string StreamUrl { get; set; }

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } // Active, Inactive, Maintenance

		[MaxLength(100)]
		public string Location { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }

		[MaxLength(50)]
		public string CameraType { get; set; } // Indoor, Outdoor, PTZ, Fixed

		[MaxLength(100)]
		public string Brand { get; set; }

		[MaxLength(100)]
		public string Model { get; set; }

		[MaxLength(20)]
		public string Resolution { get; set; } // 1080p, 4K, etc.

		public bool HasAudio { get; set; }
		public bool HasPTZ { get; set; } // Pan, Tilt, Zoom capabilities
		public bool HasNightVision { get; set; }
		public bool HasMotionDetection { get; set; }

		[MaxLength(1000)]
		public string ConfigurationJson { get; set; } // Additional camera settings

		public DateTime? LastPingAt { get; set; }
		public bool IsOnline { get; set; }

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }

		// Navigation properties
		public virtual ICollection<StreamSession> StreamSessions { get; set; } = new List<StreamSession>();
		public virtual ICollection<CameraPermission> CameraPermissions { get; set; } = new List<CameraPermission>();
		public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
	}
}
