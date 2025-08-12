using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public class AlertRule : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		public Guid CameraId { get; set; }

		[Required]
		[MaxLength(100)]
		public string RuleName { get; set; }

		[Required]
		[MaxLength(50)]
		public string AlertType { get; set; } // PersonDetected, MotionDetected, ObjectDetected

		[MaxLength(1000)]
		public string Conditions { get; set; } // JSON conditions

		public bool IsActive { get; set; } = true;

		[MaxLength(500)]
		public string Message { get; set; }

		public int Priority { get; set; } = 1; // 1-5, 5 being highest

		public TimeSpan? CooldownPeriod { get; set; } // Minimum time between alerts

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }

		// Navigation properties
		public virtual Camera Camera { get; set; }
		public virtual ICollection<AlertNotificationRule> NotificationRules { get; set; } = new List<AlertNotificationRule>();
	}
}
