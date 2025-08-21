import { Camera } from '../types';

export const calculateCameraPriority = (
  camera: Camera,
  viewportCameras: string[] = [],
  mode: 'location' | 'resolution' | 'custom' | 'none' = 'none'
): number => {
  let priority = camera.priority || 0;

  if (viewportCameras.includes(camera.id)) {
    priority += 100;
  }

  switch (mode) {
    case 'location':
      if (camera.location.toLowerCase().includes('entrance') || 
          camera.location.toLowerCase().includes('main')) {
        priority += 50;
      }
      break;

    case 'resolution':
      const [width] = camera.resolution.split('x').map(Number);
      priority += Math.floor((width || 0) / 100);
      break;

    case 'custom':
      break;

    case 'none':
    default:
      break;
  }

  return priority;
};

export const sortCamerasByPriority = (
  cameras: Camera[],
  mode: 'location' | 'resolution' | 'custom' | 'none' = 'none',
  viewportCameras: string[] = []
): Camera[] => {
  return [...cameras].sort((a, b) => {
    const aPriority = calculateCameraPriority(a, viewportCameras, mode);
    const bPriority = calculateCameraPriority(b, viewportCameras, mode);
    return bPriority - aPriority;
  });
};