using System.Text.Json.Serialization;

namespace PersonDetections.Service.Models;

public class PersonDetectionMessage
{
    [JsonPropertyName("camera_id")]
    public Guid CameraId { get; set; }
    
    [JsonPropertyName("camera_name")]
    public string CameraName { get; set; }
    
    public List<DetectionData> Detections { get; set; }
    
    [JsonPropertyName("detection_count")]
    public int DetectionCount { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("event_type")]
    public string EventType { get; set; }
    
    [JsonPropertyName("frame_path")]
    public string? FramePath { get; set; }
}

public class DetectionData
{
    public List<double> Bbox { get; set; }
    public double Confidence { get; set; }
    public List<double> Center { get; set; }
    public double Area { get; set; }
    public DateTime Timestamp { get; set; }
}