using Common.ApiResponse;
using MediatR;
using System;
using System.Collections.Generic;

namespace Streaming.Service.Commands
{
	public class ExecuteCameraControlCommand : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
		public string Command { get; set; }
		public Dictionary<string, object> Parameters { get; set; }
	}

	public class CameraControlResult
	{
		public bool Success { get; set; }
		public string Error { get; set; }
		public Dictionary<string, object> ResponseData { get; set; }
	}
}