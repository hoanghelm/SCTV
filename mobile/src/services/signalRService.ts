import { PersonDetectionEvent } from '../types';

export class SignalRService {
  private static instance: SignalRService;
  private connected = false;
  private eventHandlers: Map<string, Function[]> = new Map();

  static getInstance(): SignalRService {
    if (!SignalRService.instance) {
      SignalRService.instance = new SignalRService();
    }
    return SignalRService.instance;
  }

  async connect(): Promise<void> {
    try {
      console.log('Attempting to connect to SignalR...');
      await new Promise(resolve => setTimeout(resolve, 1000));
      this.connected = true;
      console.log('SignalR connected successfully');
    } catch (error) {
      console.error('SignalR connection failed:', error);
      throw error;
    }
  }

  isConnected(): boolean {
    return this.connected;
  }

  onPersonDetected(handler: (event: PersonDetectionEvent) => void): void {
    const handlers = this.eventHandlers.get('personDetected') || [];
    handlers.push(handler);
    this.eventHandlers.set('personDetected', handlers);
  }

  async requestCameraStream(cameraId: string): Promise<{success: boolean; data?: any; error?: string}> {
    console.log(`Requesting stream for camera ${cameraId}`);
    
    await new Promise(resolve => setTimeout(resolve, 500));
    
    return {
      success: true,
      data: {
        type: 'offer',
        sdp: 'mock-sdp-offer-data',
      },
    };
  }

  async sendAnswer(cameraId: string, answer: any): Promise<{success: boolean; error?: string}> {
    console.log(`Sending answer for camera ${cameraId}`);
    
    await new Promise(resolve => setTimeout(resolve, 300));
    
    return {
      success: true,
    };
  }

  async sendIceCandidate(cameraId: string, candidate: any): Promise<void> {
    console.log(`Sending ICE candidate for camera ${cameraId}`);
  }

  async stopCameraStream(cameraId: string): Promise<void> {
    console.log(`Stopping stream for camera ${cameraId}`);
  }

  disconnect(): void {
    this.connected = false;
    this.eventHandlers.clear();
    console.log('SignalR disconnected');
  }
}

export const signalRService = SignalRService.getInstance();