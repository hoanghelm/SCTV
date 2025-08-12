using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class GetStreamStatisticsRequest : IRequest<ApiResult>
	{
		public Guid? CameraId { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string GroupBy { get; set; } = "day";
	}
}