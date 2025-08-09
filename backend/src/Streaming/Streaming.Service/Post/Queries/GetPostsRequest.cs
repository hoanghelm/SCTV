using Common.ApiResponse;
using Common.Enums;
using MediatR;
using Paginate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streaming.Service.Streaming.Queries
{
    public class GetPostsRequest : IRequest<ApiResult>
    {
		public int Page { get; set; } = 1;
		public int Size { get; set; } = 20;
		public string Search { get; set; }
		public string Content { get; set; }
		public string Excerpt { get; set; }
		public Guid Id { get; set; }
		public DateTime LastModified { get; set; }
		public DateTime PubDate { get; set; }
		public string Slug { get; set; }
		public string Title { get; set; }
		public string CreatedBy { get; set; }

		public SortType SortType { get; set; } = SortType.DESC;
		public SortBy SortBy { get; set; } = SortBy.LATEST;
	}

	public enum SortBy
	{
		TRENDING,
		LATEST
	}
}
