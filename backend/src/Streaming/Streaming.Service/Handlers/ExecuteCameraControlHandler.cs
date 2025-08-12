using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Service.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class ExecuteCameraControlHandler : IRequestHandler<ExecuteCameraControlCommand, ApiResult>
	{
		private readonly ILogger<ExecuteCameraControlHandler> _logger;

		public ExecuteCameraControlHandler(ILogger<ExecuteCameraControlHandler> logger)
		{
			_logger = logger;
		}

		public async Task<ApiResult> Handle(ExecuteCameraControlCommand request, CancellationToken cancellationToken)
		{
			try
			{
				CameraControlResult result = null;

				_logger.LogInformation($"Executing camera control command {request.Command} for camera {request.CameraId}");

				switch (request.Command.ToLower())
				{
					case "pan":
						result = await ExecutePanCommand(request.CameraId, request.Parameters);
						break;
					case "tilt":
						result = await ExecuteTiltCommand(request.CameraId, request.Parameters);
						break;
					case "zoom":
						result = await ExecuteZoomCommand(request.CameraId, request.Parameters);
						break;
					case "preset":
						result = await ExecutePresetCommand(request.CameraId, request.Parameters);
						break;
					default:
						result = new CameraControlResult
						{
							Success = false,
							Error = $"Unknown command: {request.Command}"
						};
						break;
				}

				return ApiResult.Succeeded(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error executing camera control command {request.Command} for camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}

		private async Task<CameraControlResult> ExecutePanCommand(Guid cameraId, Dictionary<string, object> parameters)
		{
			// Simulate pan operation
			await Task.Delay(100);
			return new CameraControlResult { Success = true };
		}

		private async Task<CameraControlResult> ExecuteTiltCommand(Guid cameraId, Dictionary<string, object> parameters)
		{
			// Simulate tilt operation
			await Task.Delay(100);
			return new CameraControlResult { Success = true };
		}

		private async Task<CameraControlResult> ExecuteZoomCommand(Guid cameraId, Dictionary<string, object> parameters)
		{
			// Simulate zoom operation
			await Task.Delay(100);
			return new CameraControlResult { Success = true };
		}

		private async Task<CameraControlResult> ExecutePresetCommand(Guid cameraId, Dictionary<string, object> parameters)
		{
			// Simulate preset operation
			await Task.Delay(100);
			return new CameraControlResult { Success = true };
		}
	}
}