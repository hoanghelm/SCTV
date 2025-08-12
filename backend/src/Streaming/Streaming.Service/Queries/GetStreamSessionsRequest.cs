using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class GetStreamSessionsRequest : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string Status { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
	}
}