using Common.Entites;
using System;
using System.ComponentModel.DataAnnotations;

namespace Post.Domain.Entities
{
    public class CommentEntity : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
    {
        [Required]
        public string Author { get; set; }
        [Required]
        public string Content { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
        [Required]
        public DateTime PubDate { get; set; }
        public virtual PostEntity Post { get; set; }
		public Guid Id { get ; set ; }
		public Guid PostId { get ; set ; }
		public DateTime? CreatedAt { get ; set ; }
		public string CreatedBy { get ; set ; }
		public DateTime? UpdatedAt { get ; set ; }
		public string UpdatedBy { get ; set ; }
	}
}