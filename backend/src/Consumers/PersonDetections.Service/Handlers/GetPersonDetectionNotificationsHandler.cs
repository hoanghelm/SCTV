using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonDetections.Domain.Contracts;
using PersonDetections.Domain.Entities;
using PersonDetections.Service.Queries;
using PersonDetections.Service.ViewModels;
using System.Linq.Expressions;

namespace PersonDetections.Service.Handlers;

public class GetPersonDetectionNotificationsHandler : IRequestHandler<GetPersonDetectionNotificationsRequest, ApiResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPersonDetectionNotificationsHandler> _logger;

    public GetPersonDetectionNotificationsHandler(IUnitOfWork unitOfWork, ILogger<GetPersonDetectionNotificationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(GetPersonDetectionNotificationsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Expression<Func<PersonDetection, bool>> predicate = d => true;

            if (request.CameraId.HasValue)
            {
                predicate = d => d.CameraId == request.CameraId.Value;
            }

            if (request.FromDate.HasValue)
            {
                var currentPredicate = predicate;
                predicate = d => currentPredicate.Compile()(d) && d.EventTimestamp >= request.FromDate.Value;
            }

            if (request.ToDate.HasValue)
            {
                var currentPredicate = predicate;
                predicate = d => currentPredicate.Compile()(d) && d.EventTimestamp <= request.ToDate.Value;
            }

            var notifications = await _unitOfWork.GetRepository<PersonDetection>()
                .GetPagingListAsync(
                    predicate: predicate,
                    orderBy: q => q.OrderByDescending(d => d.EventTimestamp),
                    page: request.Page,
                    size: request.Size,
                    cancellationToken: cancellationToken);

            var viewModels = notifications.Items.Select(x => new PersonDetectionNotificationViewModel
            {
                Id = x.Id,
                CameraId = x.CameraId,
                CameraName = x.CameraName,
                EventType = x.EventType,
                EventTimestamp = x.EventTimestamp,
                DetectionCount = x.DetectionCount,
                DetectionsData = x.DetectionsData,
                FrameStoragePath = x.FrameStoragePath,
                CreatedAt = x.CreatedAt
            }).ToList();

            return ApiResult.Succeeded(new
            {
                items = viewModels,
                totalCount = notifications.Total,
                page = notifications.Page,
                pageSize = notifications.Size
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting person detection notifications");
            return ApiResult.Failed(HttpCode.InternalServerError, "Error getting person detection notifications");
        }
    }
}