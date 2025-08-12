using Common.ApiResponse;
using MediatR;
using Streaming.Service.ViewModels;
using System;

namespace Streaming.Service.Queries
{
	public class GetCameraByIdQuery : IRequest<ApiResult>
	{
		public Guid CameraId { get; set; }
	}
}