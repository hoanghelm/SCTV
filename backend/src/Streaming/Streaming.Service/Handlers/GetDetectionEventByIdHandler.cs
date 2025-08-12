using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class GetDetectionEventByIdHandler : IRequestHandler<GetDetectionEventByIdQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<GetDetectionEventByIdHandler> _logger;

		public GetDetectionEventByIdHandler(IUnitOfWork unitOfWork, ILogger<GetDetectionEventByIdHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetDetectionEventByIdQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var detectionEvent = await _unitOfWork.GetRepository<DetectionEvent>()
					.SingleOrDefaultAsync(
						predicate: d => d.Id == request.DetectionId,
						include: q => q.Include(d => d.Camera),
						cancellationToken: cancellationToken);

				if (detectionEvent == null)
				{
					return ApiResult.Failed(HttpCode.Notfound, "Detection event not found");
				}

				var result = new
				{
					detectionEvent.Id,
					detectionEvent.CameraId,
					CameraName = detectionEvent.Camera.Name,
					detectionEvent.EventType,
					detectionEvent.EventTimestamp,
					detectionEvent.Confidence,
					detectionEvent.DetectionData,
					detectionEvent.FrameImageUrl,
					detectionEvent.VideoClipUrl,
					detectionEvent.AlertTriggered,
					detectionEvent.Metadata
				};

				return ApiResult.Succeeded(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting detection event {request.DetectionId}");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting detection event");
			}
		}
	}
}