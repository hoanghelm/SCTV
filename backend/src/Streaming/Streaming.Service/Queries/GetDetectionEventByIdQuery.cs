using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class GetDetectionEventByIdQuery : IRequest<ApiResult>
	{
		public Guid DetectionId { get; set; }
	}
}