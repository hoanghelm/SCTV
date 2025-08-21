import { useEffect } from 'react';
import { videoConnectionManager } from '../services/videoConnectionManager';

interface ConnectionConfig {
  maxConcurrentConnections: number;
  staggerDelay: number;
  priorityMode: 'location' | 'resolution' | 'custom' | 'none';
}

export const useConnectionConfig = (config?: Partial<ConnectionConfig>) => {
  useEffect(() => {
    if (config?.maxConcurrentConnections) {
      videoConnectionManager.setMaxConcurrentConnections(config.maxConcurrentConnections);
    }
  }, [config?.maxConcurrentConnections]);

  const updateMaxConnections = (max: number) => {
    videoConnectionManager.setMaxConcurrentConnections(max);
  };

  return {
    updateMaxConnections,
  };
};