using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public class AlertNotificationRule : IBaseEntity<Guid>, ICreatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		public Guid AlertRuleId { get; set; }

		[Required]
		[MaxLength(450)]
		public string? UserId { get; set; }

		[MaxLength(50)]
		public string NotificationType { get; set; } // Email, SMS, Push, SignalR

		public bool IsActive { get; set; } = true;

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string? CreatedBy { get; set; }

		// Navigation properties
		public virtual AlertRule AlertRule { get; set; }
	}
}
