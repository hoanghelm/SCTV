using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class CheckCameraControlPermissionHandler : IRequestHandler<CheckCameraControlPermissionQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CheckCameraControlPermissionHandler> _logger;

		public CheckCameraControlPermissionHandler(IUnitOfWork unitOfWork, ILogger<CheckCameraControlPermissionHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(CheckCameraControlPermissionQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var permission = await _unitOfWork.GetRepository<CameraPermission>()
					.SingleOrDefaultAsync(
						p => p.CameraId == request.CameraId &&
							 p.UserId == request.UserId &&
							 p.IsActive &&
							 (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow) &&
							 (p.PermissionType == "Control" || p.PermissionType == "Admin"),
						cancellationToken: cancellationToken);

				return ApiResult.Succeeded(permission != null);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error checking camera control permission for user {request.UserId} and camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, ex.Message);
			}
		}
	}
}