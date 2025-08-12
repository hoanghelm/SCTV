using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public class CameraPermission : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		public Guid CameraId { get; set; }

		[Required]
		[MaxLength(450)]
		public string UserId { get; set; }

		[Required]
		[MaxLength(50)]
		public string PermissionType { get; set; } // View, Control, Admin

		public DateTime? ExpiresAt { get; set; }
		public bool IsActive { get; set; } = true;

		[MaxLength(500)]
		public string Notes { get; set; }

		// Audit fields
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }

		// Navigation properties
		public virtual Camera Camera { get; set; }
	}
}
