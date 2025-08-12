using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class CheckCameraControlPermissionQuery : IRequest<ApiResult>
	{
		public string UserId { get; set; }
		public Guid CameraId { get; set; }
	}
}