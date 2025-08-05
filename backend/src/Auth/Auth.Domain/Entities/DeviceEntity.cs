using Common.Entites;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Domain.Entities
{
	public partial class DeviceEntity : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }
		public string UserId { get; set; }
		public string DeviceId { get; set; }
		public string AppId { get; set; }
		public string DeviceType { get; set; }
		public string TimeZone { get; set; }
		public string Os { get; set; }
		public string SystemVersion { get; set; }
		public string Manufacturer { get; set; }
		public string DeviceToken { get; set; }
		public string Locale { get; set; }
		public string Country { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }

		public virtual UserEntity User { get; set; }
	}
}
