import { useState, useEffect, useCallback, useRef } from 'react';
import { RTCPeerConnection, RTCIceCandidate, RTCSessionDescription, MediaStream } from 'react-native-webrtc';
import { videoConnectionManager } from '../services/videoConnectionManager';
import { signalRService } from '../services/signalRService';
import { VideoStats } from '../types';

interface UseVideoReturn {
  status: "connecting" | "connected" | "disconnected" | "error" | "reconnecting";
  stats: VideoStats | null;
  isLoading: boolean;
  error: string | null;
  localStream: MediaStream | null;
  remoteStream: MediaStream | null;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  retry: () => Promise<void>;
}

const ICE_SERVERS = [
  { urls: "stun:stun.l.google.com:19302" },
  { urls: "stun:stun1.l.google.com:19302" },
];

export const useVideo = (cameraId: string): UseVideoReturn => {
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const [status, setStatus] = useState<"connecting" | "connected" | "disconnected" | "error" | "reconnecting">("disconnected");
  const [stats, setStats] = useState<VideoStats | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [localStream, setLocalStream] = useState<MediaStream | null>(null);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 3;

  const setupWebRTC = useCallback(async (offer: RTCSessionDescriptionInit) => {
    try {
      console.log('Setting up WebRTC connection for camera:', cameraId);
      
      if (peerConnectionRef.current) {
        peerConnectionRef.current.close();
        peerConnectionRef.current = null;
      }
      
      const pcConfig = {
        iceServers: ICE_SERVERS,
        iceCandidatePoolSize: 10,
      };

      const pc = new RTCPeerConnection(pcConfig);
      peerConnectionRef.current = pc;

      pc.ontrack = (event) => {
        console.log('Received track event for camera:', cameraId, {
          track: event.track,
          streams: event.streams,
          kind: event.track.kind,
          readyState: event.track.readyState,
          enabled: event.track.enabled
        });

        if (event.streams && event.streams.length > 0) {
          const stream = event.streams[0];
          console.log('Setting remote stream for camera:', cameraId, {
            streamId: stream.id,
            tracks: stream.getTracks().map(t => ({
              kind: t.kind,
              id: t.id,
              readyState: t.readyState,
              enabled: t.enabled
            }))
          });
          
          console.log('Stream URL:', stream.toURL());
          setRemoteStream(stream);
        }
      };

      pc.onaddstream = (event) => {
        console.log('Received addstream event for camera:', cameraId, {
          stream: event.stream,
          streamId: event.stream.id,
          tracks: event.stream.getTracks().map(t => ({
            kind: t.kind,
            id: t.id,
            readyState: t.readyState,
            enabled: t.enabled
          }))
        });
        
        console.log('Stream URL:', event.stream.toURL());
        setRemoteStream(event.stream);
      };

      pc.onicecandidate = async (event) => {
        if (event.candidate) {
          try {
            if (!signalRService.isConnected()) {
              console.warn('SignalR not connected, skipping ICE candidate');
              return;
            }
            await signalRService.sendIceCandidate(cameraId, {
              candidate: event.candidate.candidate,
              sdpMid: event.candidate.sdpMid,
              sdpMLineIndex: event.candidate.sdpMLineIndex,
            });
          } catch (err) {
            console.warn('Failed to send ICE candidate (non-critical):', err.message);
          }
        }
      };

      pc.onconnectionstatechange = () => {
        const state = pc.connectionState;
        console.log('Connection state changed to:', state);

        switch (state) {
          case 'connected':
            console.log('WebRTC connection established successfully');
            if (status !== 'connected') {
              setStatus('connected');
              setError(null);
              setIsLoading(false);
            }
            break;
          case 'disconnected':
            console.log('WebRTC connection disconnected');
            setStatus('disconnected');
            setStats(null);
            break;
          case 'failed':
            console.log('WebRTC connection failed');
            setStatus('error');
            setError('WebRTC connection failed - camera may be offline');
            setIsLoading(false);
            break;
          case 'closed':
            console.log('WebRTC connection closed');
            setStatus('disconnected');
            setStats(null);
            break;
        }
      };

      if (!offer || !offer.type || !offer.sdp) {
        throw new Error('Invalid offer received from server');
      }

      console.log('Setting remote description');
      await pc.setRemoteDescription(new RTCSessionDescription(offer));
      
      console.log('Creating answer');
      const answer = await pc.createAnswer();
      
      console.log('Setting local description');
      await pc.setLocalDescription(answer);

      console.log('Sending answer to SignalR');
      const answerResponse = await signalRService.sendAnswer(cameraId, answer);
      
      if (!answerResponse || !answerResponse.success) {
        throw new Error('Server rejected answer: ' + (answerResponse?.error || 'Unknown error'));
      }

      console.log('WebRTC setup completed for camera:', cameraId);

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
      if (peerConnectionRef.current) {
        peerConnectionRef.current.close();
        peerConnectionRef.current = null;
      }
      
      if (signalRService.isConnected()) {
        try {
          await signalRService.stopCameraStream(cameraId);
        } catch (err) {
          console.warn('Failed to stop camera stream on server:', err);
        }
      }
      
      setLocalStream(null);
      setRemoteStream(null);
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
    localStream,
    remoteStream,
    connect,
    disconnect,
    retry,
  };
};