using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class GetDetectionEventsRequest : IRequest<ApiResult>
	{
		public string UserId { get; set; }
		public Guid? CameraId { get; set; }
		public string EventType { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public double? MinConfidence { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
	}
}