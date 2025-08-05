using Common.Entites;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Auth.Domain.Entities
{
	public class RoleEntity : IdentityRole<string>, ICreatedEntity, IUpdatedEntity
	{
		public virtual ICollection<UserRoleEntity> UserRoles { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }
	}
}
