using Common.ApiResponse;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonDetections.Domain.Contracts;
using PersonDetections.Domain.Entities;
using PersonDetections.Service.Commands;
using System.Text.Json;

namespace PersonDetections.Service.Handlers;

public class ProcessPersonDetectionHandler : IRequestHandler<ProcessPersonDetectionCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPersonDetectionHandler> _logger;

    public ProcessPersonDetectionHandler(IUnitOfWork unitOfWork, ILogger<ProcessPersonDetectionHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessPersonDetectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var message = request.DetectionMessage;
            
            string frameStoragePath = null;
            if (!string.IsNullOrEmpty(message.Frame))
            {
                frameStoragePath = await SaveFrameToFileSystem(message.Frame, message.CameraId, message.Timestamp);
            }

            var detection = new PersonDetection
            {
                Id = Guid.NewGuid(),
                CameraId = message.CameraId,
                CameraName = message.CameraName,
                EventType = message.EventType,
                EventTimestamp = message.Timestamp,
                DetectionCount = message.DetectionCount,
                DetectionsData = JsonSerializer.Serialize(message.Detections),
                FrameStoragePath = frameStoragePath,
                CreatedAt = DateTime.UtcNow
            };

            var repository = _unitOfWork.GetRepository<PersonDetection>();
            await repository.InsertAsync(detection);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Processed person detection for camera {CameraId} with {DetectionCount} detections", 
                message.CameraId, message.DetectionCount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing person detection message");
            return false;
        }
    }

    private async Task<string> SaveFrameToFileSystem(string frameBase64, Guid cameraId, DateTime timestamp)
    {
        try
        {
            var frameData = Convert.FromBase64String(frameBase64);
            
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var storageDirectory = Path.Combine(baseDirectory, "storage", "detections", cameraId.ToString(), timestamp.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(storageDirectory);
            
            var fileName = $"{timestamp:yyyy-MM-dd_HH-mm-ss-fff}.jpg";
            var filePath = Path.Combine(storageDirectory, fileName);
            
            await File.WriteAllBytesAsync(filePath, frameData);
            
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving frame to file system");
            return null;
        }
    }
}