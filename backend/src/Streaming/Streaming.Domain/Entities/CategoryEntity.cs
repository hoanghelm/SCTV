using Common.Entites;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streaming.Domain.Entities
{
	public class CategoryEntity : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public Guid Id { get; set; }
		public Guid PostId { get; set; }
		public virtual PostEntity Post { get; set; }
	}
}
