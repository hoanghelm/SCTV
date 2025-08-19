class ConnectionManager {
  private activeConnections = new Set<string>();
  private pendingConnections = new Map<string, Promise<any>>();
  private lastReleaseTime = new Map<string, number>();

  async acquireConnection(cameraId: string): Promise<boolean> {
    const lastRelease = this.lastReleaseTime.get(cameraId) || 0;
    if (Date.now() - lastRelease < 500) {
      console.log(
        `Camera ${cameraId} connection blocked - too soon after release`,
      );
      return false;
    }

    if (this.activeConnections.has(cameraId)) {
      console.warn(`Camera ${cameraId} already has an active connection`);
      return false;
    }

    if (this.pendingConnections.has(cameraId)) {
      console.warn(
        `Camera ${cameraId} connection already in progress, waiting...`,
      );
      try {
        await this.pendingConnections.get(cameraId);
        return false;
      } catch (error) {
        this.pendingConnections.delete(cameraId);
      }
    }

    this.activeConnections.add(cameraId);
    console.log(`Connection acquired for camera ${cameraId}`);
    return true;
  }

  releaseConnection(cameraId: string): void {
    this.activeConnections.delete(cameraId);
    this.pendingConnections.delete(cameraId);
    this.lastReleaseTime.set(cameraId, Date.now());
    console.log(`Connection released for camera ${cameraId}`);
  }

  setPendingConnection(
    cameraId: string,
    connectionPromise: Promise<any>,
  ): void {
    this.pendingConnections.set(cameraId, connectionPromise);
  }

  isConnected(cameraId: string): boolean {
    return this.activeConnections.has(cameraId);
  }

  isPending(cameraId: string): boolean {
    return this.pendingConnections.has(cameraId);
  }
}

export const connectionManager = new ConnectionManager();
