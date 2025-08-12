using Common.ApiResponse;
using MediatR;
using System;

namespace Streaming.Service.Queries
{
	public class CheckCameraPermissionQuery : IRequest<ApiResult>
	{
		public string UserId { get; set; }
		public Guid CameraId { get; set; }
		public string PermissionType { get; set; } = "View";
	}
}