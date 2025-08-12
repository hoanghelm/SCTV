using Common.ApiResponse;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class GetAlertUsersForCameraHandler : IRequestHandler<GetAlertUsersForCameraQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<GetAlertUsersForCameraHandler> _logger;

		public GetAlertUsersForCameraHandler(IUnitOfWork unitOfWork, ILogger<GetAlertUsersForCameraHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetAlertUsersForCameraQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var alertUsers = await _unitOfWork.GetRepository<AlertNotificationRule>()
					.GetListAsync(
						selector: r => r.UserId,
						predicate: r => r.AlertRule.CameraId == request.CameraId &&
									   r.IsActive &&
									   r.AlertRule.IsActive,
						include: q => q.Include(r => r.AlertRule),
						cancellationToken: cancellationToken);

				return ApiResult.Succeeded(alertUsers.Distinct().ToList());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting alert users for camera {request.CameraId}");
				return ApiResult.Succeeded(new List<string>());
			}
		}
	}
}