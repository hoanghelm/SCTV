class VideoConnectionManager {
  private connections = new Map<string, {
    status: 'idle' | 'connecting' | 'connected' | 'error',
    lastAttempt: number,
    retryCount: number,
    peerConnection?: RTCPeerConnection
  }>()

  private static instance: VideoConnectionManager
  
  static getInstance(): VideoConnectionManager {
    if (!VideoConnectionManager.instance) {
      VideoConnectionManager.instance = new VideoConnectionManager()
    }
    return VideoConnectionManager.instance
  }

  canConnect(cameraId: string): boolean {
    const connection = this.connections.get(cameraId)
    
    if (!connection) {
      return true
    }

    const timeSinceLastAttempt = Date.now() - connection.lastAttempt
    if (timeSinceLastAttempt < 1000 && connection.status === 'connecting') {
      console.log(`Connection attempt blocked for ${cameraId} - still connecting`)
      return false
    }

    if (connection.status === 'connecting' || connection.status === 'connected') {
      console.log(`Connection blocked for ${cameraId} - already ${connection.status}`)
      return false
    }

    return true
  }

  startConnection(cameraId: string): void {
    this.connections.set(cameraId, {
      status: 'connecting',
      lastAttempt: Date.now(),
      retryCount: (this.connections.get(cameraId)?.retryCount || 0) + 1
    })
  }

  setConnected(cameraId: string, peerConnection: RTCPeerConnection): void {
    const connection = this.connections.get(cameraId)
    if (connection) {
      connection.status = 'connected'
      connection.peerConnection = peerConnection
      connection.retryCount = 0
    }
  }

  setError(cameraId: string): void {
    const connection = this.connections.get(cameraId)
    if (connection) {
      connection.status = 'error'
      connection.peerConnection = undefined
    }
  }

  disconnect(cameraId: string): void {
    const connection = this.connections.get(cameraId)
    if (connection?.peerConnection) {
      connection.peerConnection.close()
    }
    
    this.connections.set(cameraId, {
      status: 'idle',
      lastAttempt: Date.now(),
      retryCount: 0
    })
  }

  getStatus(cameraId: string): string {
    return this.connections.get(cameraId)?.status || 'idle'
  }

  getRetryCount(cameraId: string): number {
    return this.connections.get(cameraId)?.retryCount || 0
  }

  cleanup(cameraId: string): void {
    const connection = this.connections.get(cameraId)
    if (connection?.peerConnection) {
      connection.peerConnection.close()
    }
    this.connections.delete(cameraId)
  }
}

export const videoConnectionManager = VideoConnectionManager.getInstance()