using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Domain.Entities
{
	public enum CameraStatus
	{
		Active,
		Inactive,
		Maintenance,
		Offline,
		Error
	}

	public enum StreamSessionStatus
	{
		Active,
		Ended,
		Failed,
		Connecting,
		Disconnected
	}

	public enum PermissionType
	{
		View,
		Control,
		Admin
	}

	public enum AlertType
	{
		PersonDetected,
		MotionDetected,
		ObjectDetected,
		CameraOffline,
		AudioDetected
	}

	public enum NotificationType
	{
		Email,
		SMS,
		Push,
		SignalR
	}
}
