namespace PersonDetections.Service.Models;

public class PersonDetectionMessage
{
    public Guid CameraId { get; set; }
    public string CameraName { get; set; }
    public List<DetectionData> Detections { get; set; }
    public int DetectionCount { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string? Frame { get; set; }
}

public class DetectionData
{
    public List<double> Bbox { get; set; }
    public double Confidence { get; set; }
    public List<double> Center { get; set; }
    public double Area { get; set; }
    public DateTime Timestamp { get; set; }
}