using Common.Entites;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Domain.Entities
{
	public partial class UserRoleEntity : IdentityUserRole<string>, ICreatedEntity, IUpdatedEntity
	{
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }

		public virtual UserEntity User { get; set; }
		public virtual RoleEntity Role { get; set; }
	}
}