using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class CheckCameraPermissionHandler : IRequestHandler<CheckCameraPermissionQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CheckCameraPermissionHandler> _logger;

		public CheckCameraPermissionHandler(IUnitOfWork unitOfWork, ILogger<CheckCameraPermissionHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(CheckCameraPermissionQuery request, CancellationToken cancellationToken)
		{
			try
			{
				// Check if camera exists and is active
				var camera = await _unitOfWork.GetRepository<Camera>()
					.SingleOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken: cancellationToken);

				if (camera == null || camera.Status != CameraStatus.Active.ToString())
				{
					return ApiResult.Failed(HttpCode.Notfound);
				}

				// Check specific permission
				var permission = await _unitOfWork.GetRepository<CameraPermission>()
					.SingleOrDefaultAsync(
						p => p.CameraId == request.CameraId &&
							 p.UserId == request.UserId &&
							 p.IsActive &&
							 (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow) &&
							 (p.PermissionType == request.PermissionType || p.PermissionType == "Admin"),
						cancellationToken: cancellationToken);

				return ApiResult.Succeeded(permission != null);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking camera permission for user {request.UserId} and camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, ex.Message);
			}
		}
	}
}