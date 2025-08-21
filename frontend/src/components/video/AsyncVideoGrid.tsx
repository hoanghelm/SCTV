import React from 'react';
import { Camera } from '../../types';
import { VideoGrid } from '../dashboard/VideoGrid';
import { useConnectionConfig } from '../../hooks/useConnectionConfig';
import { sortCamerasByPriority } from '../../utils/connectionPriority';

interface AsyncVideoGridProps {
  cameras: Camera[];
  signalRConnected?: boolean;
  maxConcurrentConnections?: number;
  priorityMode?: 'location' | 'resolution' | 'custom' | 'none';
}

export const AsyncVideoGrid: React.FC<AsyncVideoGridProps> = ({
  cameras,
  signalRConnected = false,
  maxConcurrentConnections = 3,
  priorityMode = 'none'
}) => {
  const { updateMaxConnections } = useConnectionConfig({
    maxConcurrentConnections
  });

  const sortedCameras = React.useMemo(() => {
    return sortCamerasByPriority(cameras, priorityMode);
  }, [cameras, priorityMode]);

  React.useEffect(() => {
    updateMaxConnections(maxConcurrentConnections);
  }, [maxConcurrentConnections, updateMaxConnections]);

  return (
    <VideoGrid 
      cameras={sortedCameras} 
      signalRConnected={signalRConnected}
    />
  );
};