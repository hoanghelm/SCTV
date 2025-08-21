import { useState, useEffect, useCallback } from 'react';
import { videoConnectionManager } from '../services/videoConnectionManager';
import { signalRService } from '../services/signalRService';
import { VideoStats } from '../types';

interface UseVideoReturn {
  status: "connecting" | "connected" | "disconnected" | "error" | "reconnecting";
  stats: VideoStats | null;
  isLoading: boolean;
  error: string | null;
  streamUrl: string | null;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  retry: () => Promise<void>;
}

const ICE_SERVERS = [
  { urls: "stun:stun.l.google.com:19302" },
  { urls: "stun:stun1.l.google.com:19302" },
];

export const useVideo = (cameraId: string): UseVideoReturn => {
  const [status, setStatus] = useState<"connecting" | "connected" | "disconnected" | "error" | "reconnecting">("disconnected");
  const [stats, setStats] = useState<VideoStats | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [streamUrl, setStreamUrl] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 3;

  const setupWebRTC = useCallback(async (offer: any) => {
    try {
      console.log('Setting up WebRTC connection for camera:', cameraId);
      
      const pcConfig = {
        iceServers: ICE_SERVERS,
        iceCandidatePoolSize: 10,
      };

      // For mobile, we'll simulate the WebRTC connection
      // In a real implementation, you'd use react-native-webrtc
      console.log('WebRTC offer received:', offer);
      
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const mockStreamUrl = `mock://stream/${cameraId}`;
      setStreamUrl(mockStreamUrl);
      setStatus("connected");
      setIsLoading(false);
      videoConnectionManager.setConnected(cameraId);
      
      console.log('Mock WebRTC connection established for camera:', cameraId);
      
      setStats({
        fps: 25,
        bitrate: 2500,
        resolution: "1920x1080"
      });

    } catch (error) {
      console.error('WebRTC setup error:', error);
      setError(error instanceof Error ? error.message : 'WebRTC setup failed');
      setStatus("error");
      setIsLoading(false);
      throw error;
    }
  }, [cameraId]);

  const connect = useCallback(async () => {
    if (status === "connecting" || status === "connected") {
      console.log(`Connection attempt ignored - already ${status}`);
      return;
    }

    if (!videoConnectionManager.canConnect(cameraId)) {
      return;
    }

    const currentRetryCount = videoConnectionManager.getRetryCount(cameraId);
    if (currentRetryCount >= maxRetries) {
      setError("Maximum retry attempts exceeded. Please check your camera and network connection.");
      setStatus("error");
      videoConnectionManager.setError(cameraId);
      return;
    }

    try {
      videoConnectionManager.startConnection(cameraId);
      setIsLoading(true);
      setStatus("connecting");
      setError(null);

      console.log(`Connecting to camera ${cameraId}, attempt ${currentRetryCount + 1}/${maxRetries}`);

      if (!signalRService.isConnected()) {
        console.log('SignalR not connected, attempting to connect...');
        await signalRService.connect();
      }

      const response = await signalRService.requestCameraStream(cameraId);

      if (response.success && response.data) {
        await setupWebRTC(response.data);
      } else {
        throw new Error(response.error || "Failed to request camera stream");
      }
    } catch (error) {
      console.error('Connection error:', error);
      const errorMessage = error instanceof Error ? error.message : "Connection failed";

      let userFriendlyMessage = errorMessage;
      if (errorMessage.includes("SignalR")) {
        userFriendlyMessage = "Unable to connect to streaming service. Please check your network connection.";
      } else if (errorMessage.includes("camera stream")) {
        userFriendlyMessage = "Camera is not available or offline. Please check camera status.";
      } else if (errorMessage.includes("WebRTC")) {
        userFriendlyMessage = "Failed to establish video connection. Please try again.";
      }

      setError(userFriendlyMessage);
      setStatus("error");
      setIsLoading(false);
      videoConnectionManager.setError(cameraId);
    }
  }, [cameraId, setupWebRTC, maxRetries]);

  const disconnect = useCallback(async () => {
    try {
      setStreamUrl(null);
      setStatus("disconnected");
      setStats(null);
      setError(null);
      setIsLoading(false);
      setRetryCount(0);
      
      videoConnectionManager.disconnect(cameraId);
    } catch (error) {
      console.error('Disconnect error:', error);
    }
  }, [cameraId]);

  const retry = useCallback(async () => {
    if (status === "connecting" || status === "connected") {
      return;
    }

    setStatus("reconnecting");
    setError(null);

    await disconnect();
    await new Promise((resolve) => setTimeout(resolve, 1000));
    await connect();
  }, [status, disconnect, connect]);

  useEffect(() => {
    return () => {
      videoConnectionManager.cleanup(cameraId);
    };
  }, [cameraId]);

  return {
    status,
    stats,
    isLoading,
    error,
    streamUrl,
    connect,
    disconnect,
    retry,
  };
};