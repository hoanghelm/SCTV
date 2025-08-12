using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Commands
{
	public class EndStreamSessionCommand : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
		public string ViewerId { get; set; }
		public string ConnectionId { get; set; }
		public TimeSpan? Duration { get; set; }
		public long? BytesTransferred { get; set; }
		public int? FramesSent { get; set; }
	}
}