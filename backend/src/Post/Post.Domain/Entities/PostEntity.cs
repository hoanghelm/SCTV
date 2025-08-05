using Common.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Post.Domain.Entities
{
	public class PostEntity : IBaseEntity<Guid>, ICreatedEntity, IUpdatedEntity
	{
		public PostEntity()
		{
			PostCategories = new HashSet<CategoryEntity>();
			PostComments = new HashSet<CommentEntity>();
		}

		public virtual ICollection<CategoryEntity> PostCategories { get; }
		public virtual ICollection<CommentEntity> PostComments { get; }
		[Required]
		public string Content { get; set; }
		[Required]
		public string Excerpt { get; set; }
		[Required]
		public Guid Id { get; set; }
		public bool IsPublished { get; set; }
		public DateTime LastModified { get; set; }
		public DateTime PubDate { get; set; }
		public string Slug { get; set; }
		[Required]
		public string Title { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string CreatedBy { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string UpdatedBy { get; set; }
	}
}