import * as signalR from '@microsoft/signalr'
import { PersonDetectionEvent, AlertNotification } from '../types'

export class SignalRService {
  private connection: signalR.HubConnection | null = null
  private apiUrl: string
  private authToken: string = ''
  private isConnecting: boolean = false
  private connectionPromise: Promise<signalR.HubConnection> | null = null

  constructor(apiUrl: string = 'https://localhost:44322') {
    this.apiUrl = apiUrl
  }

  setAuthToken(token: string) {
    this.authToken = token
  }

  async connect(): Promise<signalR.HubConnection> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return this.connection
    }

    if (this.isConnecting && this.connectionPromise) {
      console.log('Connection already in progress, waiting for existing attempt...')
      try {
        return await this.connectionPromise
      } catch (error) {
        console.warn('Previous connection attempt failed, will retry')
      }
    }

    this.isConnecting = true
    this.connectionPromise = this.doConnect()

    try {
      const connection = await this.connectionPromise
      return connection
    } finally {
      this.isConnecting = false
      this.connectionPromise = null
    }
  }

  private async doConnect(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.stop()
      } catch (err) {
        console.warn('Error stopping existing connection:', err)
      }
      this.connection = null
    }

    const hubUrl = `${this.apiUrl}/streamingHub`
    
    const connectionOptions: signalR.IHttpConnectionOptions = {
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      withCredentials: false
    }

    if (this.authToken && this.authToken.trim()) {
      connectionOptions.accessTokenFactory = () => this.authToken.replace('Bearer ', '')
    }

    const connectionBuilder = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, connectionOptions)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)

    this.connection = connectionBuilder.build()

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...')
    })

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected')
    })

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error)
      this.connection = null
      this.isConnecting = false
      this.connectionPromise = null
    })

    try {
      await this.connection.start()
      console.log('SignalR connected')
      return this.connection
    } catch (error) {
      console.error('Failed to connect to SignalR:', error)
      this.connection = null
      throw error
    }
  }

  async disconnect(): Promise<void> {
    this.isConnecting = false
    this.connectionPromise = null
    
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch (err) {
        console.warn('Error during disconnect:', err)
      }
      this.connection = null
    }
  }

  onPersonDetected(callback: (event: PersonDetectionEvent) => void): void {
    this.connection?.on('PersonDetected', callback)
  }

  onAlertNotification(callback: (alert: AlertNotification) => void): void {
    this.connection?.on('AlertNotification', callback)
  }

  async requestCameraStream(cameraId: string): Promise<any> {
    await this.ensureConnected()
    return await this.connection!.invoke('RequestCameraStream', cameraId)
  }

  async sendAnswer(cameraId: string, answer: RTCSessionDescriptionInit): Promise<any> {
    await this.ensureConnected()
    return await this.connection!.invoke('SendAnswer', cameraId, {
      type: answer.type,
      sdp: answer.sdp
    })
  }

  async sendIceCandidate(cameraId: string, candidate: RTCIceCandidateInit): Promise<any> {
    await this.ensureConnected()
    return await this.connection!.invoke('SendIceCandidate', cameraId, {
      candidate: candidate.candidate,
      sdpMid: candidate.sdpMid,
      sdpMLineIndex: candidate.sdpMLineIndex
    })
  }

  async stopCameraStream(cameraId: string): Promise<void> {
    await this.ensureConnected()
    await this.connection!.invoke('StopCameraStream', cameraId)
  }

  getConnection(): signalR.HubConnection | null {
    return this.connection
  }

  private async ensureConnected(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.connect()
    }
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state || null
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected
  }
}

export const signalRService = new SignalRService()