using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Commands
{
	public class DeleteCameraCommand : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
	}
}