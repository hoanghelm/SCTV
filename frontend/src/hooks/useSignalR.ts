import { useEffect, useState, useCallback } from "react";
import { useDispatch } from "react-redux";
import { signalRService } from "../services/signalRService";
import { PersonDetectionEvent, AlertNotification } from "../types";
import { AppDispatch } from "../store";
import {
  setSignalRConnection,
  setConnectionStatus,
  addDetectionEvent,
  addNotification,
} from "../store/slices/streamingSlice";

interface UseSignalRReturn {
  isConnected: boolean;
  isConnecting: boolean;
  error: string | null;
  connect: (apiUrl?: string, authToken?: string) => Promise<void>;
  disconnect: () => Promise<void>;
}

export const useSignalR = (): UseSignalRReturn => {
  const [isConnected, setIsConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const dispatch = useDispatch<AppDispatch>();

  const setupEventHandlers = useCallback(() => {
    signalRService.onPersonDetected((event: PersonDetectionEvent) => {
      dispatch(addDetectionEvent(event));
    });

    signalRService.onAlertNotification((alert: AlertNotification) => {
      dispatch(addNotification(alert));
    });
  }, [dispatch]);

  const connect = useCallback(
    async (apiUrl?: string, authToken?: string) => {
      if (signalRService.isConnected()) {
        setIsConnected(true);
        setIsConnecting(false);
        return;
      }

      if (isConnecting) {
        console.log("Already connecting, skipping duplicate request");
        return;
      }

      try {
        setIsConnecting(true);
        setError(null);

        if (authToken) {
          signalRService.setAuthToken(authToken);
        }

        const connection = await signalRService.connect();

        setupEventHandlers();

        dispatch(setSignalRConnection(connection));
        dispatch(setConnectionStatus("connected"));

        setIsConnected(true);
        setIsConnecting(false);
      } catch (error) {
        console.error("SignalR connection error:", error);
        setError(error instanceof Error ? error.message : "Connection failed");
        setIsConnected(false);
        setIsConnecting(false);
        dispatch(setConnectionStatus("disconnected"));
      }
    },
    [isConnecting, dispatch, setupEventHandlers],
  );

  const disconnect = useCallback(async () => {
    try {
      await signalRService.disconnect();

      dispatch(setSignalRConnection(null));
      dispatch(setConnectionStatus("disconnected"));

      setIsConnected(false);
      setIsConnecting(false);
      setError(null);
    } catch (error) {
      console.error("SignalR disconnect error:", error);
      setError(error instanceof Error ? error.message : "Disconnect failed");
    }
  }, [dispatch]);

  useEffect(() => {
    return () => {
      if (isConnected) {
        disconnect();
      }
    };
  }, [isConnected, disconnect]);

  return {
    isConnected,
    isConnecting,
    error,
    connect,
    disconnect,
  };
};
