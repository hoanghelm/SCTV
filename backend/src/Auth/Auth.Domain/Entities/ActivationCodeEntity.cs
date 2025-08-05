using Common.Entites;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Domain.Entities
{
	public partial class ActivationCodeEntity : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public DateTime ExpireAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public string UserId { get; set; }
        public virtual UserEntity User { get; set; }
    }
}