using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Domain.Entities
{
    public partial class UserStatusEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<UserEntity> Users { get; set; }
    }

    public static class UserStatus
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
		public const string Suspended = "suspended";
		public const string Deleted = "deleted";
	}
}