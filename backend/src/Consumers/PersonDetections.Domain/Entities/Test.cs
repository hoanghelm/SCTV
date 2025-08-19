using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonDetections.Domain.Entities
{
	public class Test : IBaseEntity<Guid>//, ICreatedEntity, IUpdatedEntity
	{
		public Guid Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; }
	}
}
