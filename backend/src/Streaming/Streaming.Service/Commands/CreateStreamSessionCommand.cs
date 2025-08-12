using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Commands
{
	public class CreateStreamSessionCommand : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
		public string ViewerId { get; set; }
		public string ConnectionId { get; set; }
		public string SessionDescription { get; set; }
		public string UserAgent { get; set; }
		public string IpAddress { get; set; }
	}
}