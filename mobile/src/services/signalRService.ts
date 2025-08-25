import { PersonDetectionEvent } from '../types';
import { environment } from '../config/environment';

export class SignalRService {
  private static instance: SignalRService;
  private ws: WebSocket | null = null;
  private connected = false;
  private eventHandlers: Map<string, Function[]> = new Map();
  private apiUrl: string;
  private isConnecting: boolean = false;
  private connectionPromise: Promise<any> | null = null;
  private invocationId = 0;
  private pendingInvocations = new Map<number, { resolve: Function; reject: Function }>();

  constructor() {
    this.apiUrl = environment.API_BASE_URL.replace('/api/v1/Stream', '');
  }

  static getInstance(): SignalRService {
    if (!SignalRService.instance) {
      SignalRService.instance = new SignalRService();
    }
    return SignalRService.instance;
  }

  async connect(): Promise<void> {
    if (this.connected) {
      return;
    }

    if (this.isConnecting && this.connectionPromise) {
      console.log('Connection already in progress, waiting for existing attempt...');
      try {
        await this.connectionPromise;
        return;
      } catch (error) {
        console.warn('Previous connection attempt failed, will retry');
      }
    }

    this.isConnecting = true;
    this.connectionPromise = this.doConnect();

    try {
      await this.connectionPromise;
    } finally {
      this.isConnecting = false;
      this.connectionPromise = null;
    }
  }

  private async doConnect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        console.log('Attempting to connect to SignalR...');
        
        const wsUrl = `${this.apiUrl.replace('http', 'ws')}/streamingHub`;
        console.log('Connecting to SignalR hub via WebSocket:', wsUrl);
        
        this.ws = new WebSocket(wsUrl);

        this.ws.onopen = () => {
          console.log('WebSocket connected, performing SignalR handshake...');
          
          const handshakeMessage = JSON.stringify({ protocol: 'json', version: 1 }) + '\u001e';
          this.ws?.send(handshakeMessage);
        };

        this.ws.onmessage = (event) => {
          const messages = event.data.split('\u001e').filter((msg: string) => msg.trim());
          
          for (const message of messages) {
            if (!message) continue;
            
            try {
              if (message === '{}') {
                console.log('SignalR handshake successful');
                this.connected = true;
                resolve();
                continue;
              }
              
              const data = JSON.parse(message);
              console.log('Received SignalR message:', data);
              
              if (data.type === 3 && data.invocationId) {
                const pending = this.pendingInvocations.get(parseInt(data.invocationId));
                if (pending) {
                  pending.resolve(data.result);
                  this.pendingInvocations.delete(parseInt(data.invocationId));
                }
              } else if (data.type === 1 && data.target) {
                const handlers = this.eventHandlers.get(data.target) || [];
                handlers.forEach(handler => handler(...(data.arguments || [])));
              }
            } catch (err) {
              console.warn('Failed to parse SignalR message:', message, err);
            }
          }
        };

        this.ws.onerror = (error) => {
          console.error('WebSocket error:', error);
          this.connected = false;
          reject(error);
        };

        this.ws.onclose = (event) => {
          console.log('WebSocket connection closed:', event.code, event.reason);
          this.connected = false;
          this.ws = null;
          this.isConnecting = false;
          this.connectionPromise = null;
        };

        setTimeout(() => {
          if (!this.connected) {
            reject(new Error('SignalR connection timeout'));
          }
        }, 10000);
        
      } catch (error) {
        console.error('SignalR connection failed:', error);
        this.connected = false;
        this.ws = null;
        reject(error);
      }
    });
  }

  private sendInvocation(target: string, ...args: any[]): Promise<any> {
    return new Promise((resolve, reject) => {
      if (!this.ws || !this.connected) {
        reject(new Error('SignalR not connected'));
        return;
      }

      const invocationId = ++this.invocationId;
      this.pendingInvocations.set(invocationId, { resolve, reject });

      const message = JSON.stringify({
        type: 1,
        invocationId: invocationId.toString(),
        target,
        arguments: args
      }) + '\u001e';

      console.log('Sending SignalR invocation:', { target, args, invocationId });
      this.ws.send(message);

      setTimeout(() => {
        if (this.pendingInvocations.has(invocationId)) {
          this.pendingInvocations.delete(invocationId);
          reject(new Error(`Invocation ${target} timed out`));
        }
      }, 60000);
    });
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
    
    try {
      await this.ensureConnected();
      const result = await this.sendInvocation('RequestCameraStream', cameraId);
      console.log('RequestCameraStream result:', result);
      
      if (result && result.success !== undefined) {
        return result;
      }
      
      return { success: true, data: result };
    } catch (error) {
      console.error('Failed to request camera stream:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Failed to request camera stream'
      };
    }
  }

  async sendAnswer(cameraId: string, answer: any): Promise<{success: boolean; error?: string}> {
    console.log(`Sending answer for camera ${cameraId}`, answer);
    
    try {
      await this.ensureConnected();
      const result = await this.sendInvocation('SendAnswer', cameraId, {
        type: answer.type,
        sdp: answer.sdp,
      });
      console.log('SendAnswer result:', result);
      
      if (result && result.success !== undefined) {
        return result;
      }
      
      return { success: true };
    } catch (error) {
      console.error('Failed to send answer:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Failed to send answer'
      };
    }
  }

  async sendIceCandidate(cameraId: string, candidate: any): Promise<void> {
    console.log(`Sending ICE candidate for camera ${cameraId}`, candidate);
    
    try {
      await this.ensureConnected();
      const result = await this.sendInvocation('SendIceCandidate', cameraId, {
        candidate: candidate.candidate,
        sdpMid: candidate.sdpMid,
        sdpMLineIndex: candidate.sdpMLineIndex,
      });
      console.log('SendIceCandidate result:', result);
    } catch (error) {
      console.error('Failed to send ICE candidate:', error);
    }
  }

  async stopCameraStream(cameraId: string): Promise<void> {
    console.log(`Stopping stream for camera ${cameraId}`);
    
    try {
      await this.ensureConnected();
      const result = await this.sendInvocation('StopCameraStream', cameraId);
      console.log('StopCameraStream result:', result);
    } catch (error) {
      console.error('Failed to stop camera stream:', error);
    }
  }

  async disconnect(): Promise<void> {
    this.isConnecting = false;
    this.connectionPromise = null;

    if (this.ws) {
      try {
        this.ws.close();
      } catch (err) {
        console.warn('Error during disconnect:', err);
      }
      this.ws = null;
    }
    
    this.connected = false;
    this.eventHandlers.clear();
    this.pendingInvocations.clear();
    console.log('SignalR disconnected');
  }

  private async ensureConnected(): Promise<void> {
    if (!this.connected || !this.ws) {
      await this.connect();
    }
  }
}

export const signalRService = SignalRService.getInstance();