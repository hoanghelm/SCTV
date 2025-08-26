using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonDetections.Domain.Contracts;
using PersonDetections.Domain.Entities;
using PersonDetections.Service.Queries;
using PersonDetections.Service.ViewModels;

namespace PersonDetections.Service.Handlers;

public class GetPersonDetectionNotificationByIdHandler : IRequestHandler<GetPersonDetectionNotificationByIdQuery, ApiResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPersonDetectionNotificationByIdHandler> _logger;

    public GetPersonDetectionNotificationByIdHandler(IUnitOfWork unitOfWork, ILogger<GetPersonDetectionNotificationByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(GetPersonDetectionNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _unitOfWork.GetRepository<PersonDetection>()
                .SingleOrDefaultAsync(
                    predicate: d => d.Id == request.Id,
                    cancellationToken: cancellationToken);

            if (notification == null)
            {
                return ApiResult.Failed(HttpCode.Notfound, "Notification not found");
            }

            var result = new PersonDetectionNotificationViewModel
            {
                Id = notification.Id,
                CameraId = notification.CameraId,
                CameraName = notification.CameraName,
                EventType = notification.EventType,
                EventTimestamp = notification.EventTimestamp,
                DetectionCount = notification.DetectionCount,
                DetectionsData = notification.DetectionsData,
                FrameStoragePath = notification.FrameStoragePath,
                CreatedAt = notification.CreatedAt
            };

            return ApiResult.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting person detection notification {request.Id}");
            return ApiResult.Failed(HttpCode.InternalServerError, "Error getting person detection notification");
        }
    }
}