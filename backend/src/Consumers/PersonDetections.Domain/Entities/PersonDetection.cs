using Common.Entites;

namespace PersonDetections.Domain.Entities;

public class PersonDetection : IBaseEntity<Guid>, ICreatedEntity
{
    public Guid Id { get; set; }
    public Guid CameraId { get; set; }
    public string CameraName { get; set; }
    public string EventType { get; set; }
    public DateTime EventTimestamp { get; set; }
    public int DetectionCount { get; set; }
    public string DetectionsData { get; set; }
    public string? FrameData { get; set; }
    public string? FrameStoragePath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}