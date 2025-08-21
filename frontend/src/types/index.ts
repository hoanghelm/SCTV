export interface Camera {
  id: string;
  name: string;
  location: string;
  streamUrl: string;
  status: CameraStatus;
  resolution: string;
  frameRate: number;
  testMode?: boolean;
  priority?: number;
  createdAt?: string;
  updatedAt?: string;
}

export enum CameraStatus {
  Active = "Active",
  Inactive = "Inactive",
  Error = "Error",
  Connecting = "Connecting",
}

export interface Detection {
  bbox: number[];
  confidence: number;
  label: string;
}

export interface PersonDetectionEvent {
  cameraId: string;
  cameraName: string;
  timestamp: string;
  detections: Detection[];
  frameImageUrl?: string;
  frameImageBase64?: string;
}

export interface StreamSession {
  id: string;
  cameraId: string;
  viewerId: string;
  connectionId: string;
  startedAt: string;
  endedAt?: string;
  sessionDescription?: string;
  userAgent?: string;
  ipAddress?: string;
}

export interface StreamStatistics {
  cameraId: string;
  viewerCount: number;
  connectionStats: Record<string, any>;
  timestamp: string;
}

export interface AlertNotification {
  type: string;
  cameraId: string;
  timestamp: string;
  message: string;
  imageUrl?: string;
}

export interface StreamConnection {
  id: string;
  cameraId: string;
  peerConnection?: RTCPeerConnection;
  status: "connecting" | "connected" | "disconnected" | "failed";
  stats?: StreamStatistics;
}
