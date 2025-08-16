using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public class StreamSession : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		public Guid CameraId { get; set; }

		[MaxLength(450)]
		public string? ViewerId { get; set; } // User ID

		[MaxLength(50)]
		public string ConnectionId { get; set; } // SignalR connection ID

		public DateTime StartedAt { get; set; }
		public DateTime? EndedAt { get; set; }

		[MaxLength(2000)]
		public string SessionDescription { get; set; } // WebRTC session description

		[MaxLength(50)]
		public string Status { get; set; } // Active, Ended, Failed

		public TimeSpan? Duration { get; set; }

		[MaxLength(100)]
		public string UserAgent { get; set; }

		[MaxLength(45)]
		public string IpAddress { get; set; }

		// Statistics
		public long? BytesTransferred { get; set; }
		public int? FramesSent { get; set; }
		public double? AverageLatency { get; set; }
		public int? PacketLoss { get; set; }

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string? CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		// Navigation properties
		public virtual Camera Camera { get; set; }
	}
}
