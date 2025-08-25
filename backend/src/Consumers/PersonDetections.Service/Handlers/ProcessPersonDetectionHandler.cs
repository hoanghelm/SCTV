using Common.ApiResponse;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonDetections.Domain.Contracts;
using PersonDetections.Domain.Entities;
using PersonDetections.Service.Commands;
using PersonDetections.Service.Services;
using System.Text.Json;

namespace PersonDetections.Service.Handlers;

public class ProcessPersonDetectionHandler : IRequestHandler<ProcessPersonDetectionCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPersonDetectionHandler> _logger;
    private readonly IFirebaseNotificationService _notificationService;

    public ProcessPersonDetectionHandler(IUnitOfWork unitOfWork, ILogger<ProcessPersonDetectionHandler> logger, IFirebaseNotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(ProcessPersonDetectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var message = request.DetectionMessage;

            var detection = new PersonDetection
            {
                Id = Guid.NewGuid(),
                CameraId = message.CameraId,
                CameraName = string.IsNullOrEmpty(message.CameraName) ? $"Camera-{message.CameraId}" : message.CameraName,
                EventType = string.IsNullOrEmpty(message.EventType) ? "person_detection" : message.EventType,
                EventTimestamp = message.Timestamp.Kind == DateTimeKind.Unspecified ? 
                    DateTime.SpecifyKind(message.Timestamp, DateTimeKind.Utc) : message.Timestamp,
                DetectionCount = message.DetectionCount,
                DetectionsData = JsonSerializer.Serialize(message.Detections),
                FrameStoragePath = message.FramePath,
                CreatedAt = DateTime.UtcNow
            };

            var repository = _unitOfWork.GetRepository<PersonDetection>();
            await repository.InsertAsync(detection);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Processed person detection for camera {CameraId} with {DetectionCount} detections", 
                message.CameraId, message.DetectionCount);

            if (message.DetectionCount > 0)
            {
                var notificationSent = await _notificationService.SendPersonDetectionNotificationAsync(
                    detection.CameraName, 
                    message.DetectionCount);
                
                if (notificationSent)
                {
                    _logger.LogInformation("FCM notification sent for camera {CameraId}", message.CameraId);
                }
                else
                {
                    _logger.LogWarning("Failed to send FCM notification for camera {CameraId}", message.CameraId);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing person detection message");
            return false;
        }
    }

}