using Common.Entites;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Auth.Domain.Entities
{
	public partial class UserEntity : IdentityUser<string>, ICreatedEntity, IUpdatedEntity
	{
		public UserEntity()
		{
			Claims = new HashSet<IdentityUserClaim<string>>();
			Devices = new HashSet<DeviceEntity>();
		}

		public virtual ActivationCodeEntity ActivationCode { get; set; }
		public virtual ICollection<DeviceEntity> Devices { get; set; }
		public virtual UserLoginEntity UserLogin { get; set; }
		public virtual UserStatusEntity UserStatus { get; set; }
		public virtual ICollection<UserRoleEntity> UserRoles { get; set; }
		public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; }
		public string FullName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string AvatarUrl { get; set; }
		public string RefreshToken { get; set; }
		public int LockedCount { get; set; }
		public bool IsReceiveNotification { get; set; }
		public bool IsRoot { get; set; }
		public string UserStatusId { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
	}
}
