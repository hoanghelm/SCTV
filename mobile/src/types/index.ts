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

export interface PersonDetectionNotification {
  id: string;
  cameraId: string;
  cameraName: string;
  eventType: string;
  eventTimestamp: string;
  detectionCount: number;
  detectionsData: string;
  frameStoragePath?: string;
  createdAt?: string;
}

export interface NotificationListResponse {
  items: PersonDetectionNotification[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface VideoStats {
  fps: number;
  bitrate: number;
  resolution: string;
}

export interface ConnectionStatus {
  connected: boolean;
  lastConnected?: string;
  error?: string;
}