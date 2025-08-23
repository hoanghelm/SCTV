import axios from 'axios';
import { Camera, CameraStatus } from '../types';

const API_BASE_URL = 'https://10.0.2.2:44322/api/v1/Stream';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Accept': '*/*',
    'Content-Type': 'application/json',
  },
});

export class CameraService {
  async getCameras(): Promise<Camera[]> {
    try {
      const response = await apiClient.get('/cameras');
      return response.data || [];
    } catch (error) {
      console.error('Failed to fetch cameras:', error);
      return this.getMockCameras();
    }
  }

  async getCameraById(id: string): Promise<Camera | null> {
    try {
      const response = await apiClient.get(`/cameras/${id}`);
      return response.data || null;
    } catch (error) {
      console.error('Failed to fetch camera:', error);
      const mockCameras = this.getMockCameras();
      return mockCameras.find(camera => camera.id === id) || null;
    }
  }

  private getMockCameras(): Camera[] {
    return [
      {
        id: '44d98ad2-4de7-48ca-961a-ba02d0fca8cc',
        name: 'Front Entrance',
        location: 'Main Entrance',
        streamUrl: 'rtsp://demo:demo@ipvmdemo.dyndns.org:5540/onvif-media/media.amp?profile=profile_1_h264&sessiontimeout=60&streamtype=unicast',
        status: CameraStatus.Active,
        resolution: '1920x1080',
        frameRate: 30,
        priority: 100,
      },
      {
        id: '44d98ad2-4de7-48ca-961a-ba02d0fca8dd',
        name: 'Parking Lot',
        location: 'Exterior Parking',
        streamUrl: 'rtsp://demo:demo@ipvmdemo.dyndns.org:5540/onvif-media/media.amp?profile=profile_2_h264&sessiontimeout=60&streamtype=unicast',
        status: CameraStatus.Active,
        resolution: '1280x720',
        frameRate: 25,
        priority: 50,
      },
      {
        id: '44d98ad2-4de7-48ca-961a-ba02d0fca8ee',
        name: 'Reception Area',
        location: 'Lobby',
        streamUrl: 'rtsp://demo:demo@ipvmdemo.dyndns.org:5540/onvif-media/media.amp?profile=profile_3_h264&sessiontimeout=60&streamtype=unicast',
        status: CameraStatus.Active,
        resolution: '1920x1080',
        frameRate: 30,
        priority: 75,
      },
      {
        id: '44d98ad2-4de7-48ca-961a-ba02d0fca8ff',
        name: 'Back Exit',
        location: 'Rear Exit',
        streamUrl: 'rtsp://demo:demo@ipvmdemo.dyndns.org:5540/onvif-media/media.amp?profile=profile_4_h264&sessiontimeout=60&streamtype=unicast',
        status: CameraStatus.Active,
        resolution: '1280x720',
        frameRate: 20,
        priority: 25,
      },
    ];
  }
}

export const cameraService = new CameraService();