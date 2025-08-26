namespace PersonDetections.Service.ViewModels;

public class PersonDetectionNotificationViewModel
{
    public Guid Id { get; set; }
    public Guid CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public int DetectionCount { get; set; }
    public string DetectionsData { get; set; } = string.Empty;
    public string? FrameStoragePath { get; set; }
    public DateTime? CreatedAt { get; set; }
}