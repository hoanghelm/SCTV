using System;

namespace Streaming.Service.ViewModels
{
	public class StreamSessionViewModel
	{
		public Guid Id { get; set; }
		public Guid CameraId { get; set; }
		public string? ViewerId { get; set; }
		public string ConnectionId { get; set; }
		public DateTime StartedAt { get; set; }
		public DateTime? EndedAt { get; set; }
		public string Status { get; set; }
		public TimeSpan? Duration { get; set; }
		public long? BytesTransferred { get; set; }
		public int? FramesSent { get; set; }
	}
}