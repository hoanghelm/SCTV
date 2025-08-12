using Common.ApiResponse;
using MediatR;
using System;
using System.Collections.Generic;

namespace Streaming.Service.Queries
{
	public class GetAlertUsersForCameraQuery : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
	}
}