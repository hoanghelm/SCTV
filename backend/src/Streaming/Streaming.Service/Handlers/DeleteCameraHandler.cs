using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class DeleteCameraHandler : IRequestHandler<DeleteCameraCommand, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<DeleteCameraHandler> _logger;

		public DeleteCameraHandler(IUnitOfWork unitOfWork, ILogger<DeleteCameraHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(DeleteCameraCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var camera = await _unitOfWork.GetRepository<Camera>()
					.SingleOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken: cancellationToken);

				if (camera == null)
				{
					return ApiResult.Failed(HttpCode.Notfound, "Camera not found");
				}

				// Check if there are active sessions
				var activeSessionsCount = await _unitOfWork.GetRepository<StreamSession>()
					.GetListAsync(
						predicate: s => s.CameraId == request.CameraId && s.Status == StreamSessionStatus.Active.ToString(),
						cancellationToken: cancellationToken);

				if (activeSessionsCount.Count > 0)
				{
					return ApiResult.Failed(HttpCode.BadRequest, "Cannot delete camera with active stream sessions");
				}

				_unitOfWork.GetRepository<Camera>().Delete(camera);
				await _unitOfWork.CommitAsync();

				_logger.LogInformation($"Camera {request.CameraId} deleted successfully");
				return ApiResult.Succeeded();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error deleting camera");
			}
		}
	}
}