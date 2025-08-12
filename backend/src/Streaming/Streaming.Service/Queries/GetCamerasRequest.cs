using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class GetCamerasRequest : IRequest<ApiResult>
	{
		public string UserId { get; set; }
		public string Search { get; set; }
		public string Status { get; set; }
		public string CameraType { get; set; }
		public string Location { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
		public string SortBy { get; set; } = "Name";
		public string SortOrder { get; set; } = "asc";
	}
}