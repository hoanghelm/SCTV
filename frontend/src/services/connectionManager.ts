// Global connection manager to prevent multiple simultaneous connections to the same camera
class ConnectionManager {
  private activeConnections = new Set<string>()
  private pendingConnections = new Map<string, Promise<any>>()
  private lastReleaseTime = new Map<string, number>()

  async acquireConnection(cameraId: string): Promise<boolean> {
    // Prevent rapid re-acquisition after release (debounce) - reduced time
    const lastRelease = this.lastReleaseTime.get(cameraId) || 0
    if (Date.now() - lastRelease < 500) {
      console.log(`Camera ${cameraId} connection blocked - too soon after release`)
      return false
    }

    // If already connected, reject
    if (this.activeConnections.has(cameraId)) {
      console.warn(`Camera ${cameraId} already has an active connection`)
      return false
    }

    // If connection is pending, wait for it
    if (this.pendingConnections.has(cameraId)) {
      console.warn(`Camera ${cameraId} connection already in progress, waiting...`)
      try {
        await this.pendingConnections.get(cameraId)
        return false // Another component got the connection
      } catch (error) {
        // Previous connection failed, we can try
        this.pendingConnections.delete(cameraId)
      }
    }

    // Mark as active
    this.activeConnections.add(cameraId)
    console.log(`Connection acquired for camera ${cameraId}`)
    return true
  }

  releaseConnection(cameraId: string): void {
    this.activeConnections.delete(cameraId)
    this.pendingConnections.delete(cameraId)
    this.lastReleaseTime.set(cameraId, Date.now())
    console.log(`Connection released for camera ${cameraId}`)
  }

  setPendingConnection(cameraId: string, connectionPromise: Promise<any>): void {
    this.pendingConnections.set(cameraId, connectionPromise)
  }

  isConnected(cameraId: string): boolean {
    return this.activeConnections.has(cameraId)
  }

  isPending(cameraId: string): boolean {
    return this.pendingConnections.has(cameraId)
  }
}

export const connectionManager = new ConnectionManager()