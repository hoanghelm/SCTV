using Common.ApiResponse;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Post.Service.Post.Commands
{
	public class CreatePostRequest : IRequest<ApiResult>
	{
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
		public string CreatedBy { get; set; }
	}
}
