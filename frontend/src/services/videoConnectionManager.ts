class VideoConnectionManager {
  private connections = new Map<
    string,
    {
      status: "idle" | "connecting" | "connected" | "error";
      lastAttempt: number;
      retryCount: number;
      peerConnection?: RTCPeerConnection;
      priority?: number;
    }
  >();

  private connectionQueue: Array<{
    cameraId: string;
    connectFn: () => Promise<void>;
    priority: number;
  }> = [];

  private maxConcurrentConnections = 3;
  private activeConnections = 0;
  private processingQueue = false;

  private static instance: VideoConnectionManager;

  static getInstance(): VideoConnectionManager {
    if (!VideoConnectionManager.instance) {
      VideoConnectionManager.instance = new VideoConnectionManager();
    }
    return VideoConnectionManager.instance;
  }

  canConnect(cameraId: string): boolean {
    const connection = this.connections.get(cameraId);

    if (!connection) {
      return true;
    }

    const timeSinceLastAttempt = Date.now() - connection.lastAttempt;
    if (timeSinceLastAttempt < 1000 && connection.status === "connecting") {
      console.log(
        `Connection attempt blocked for ${cameraId} - still connecting`,
      );
      return false;
    }

    if (
      connection.status === "connecting" ||
      connection.status === "connected"
    ) {
      console.log(
        `Connection blocked for ${cameraId} - already ${connection.status}`,
      );
      return false;
    }

    return true;
  }

  startConnection(cameraId: string): void {
    this.connections.set(cameraId, {
      status: "connecting",
      lastAttempt: Date.now(),
      retryCount: (this.connections.get(cameraId)?.retryCount || 0) + 1,
    });
  }

  setConnected(cameraId: string, peerConnection: RTCPeerConnection): void {
    const connection = this.connections.get(cameraId);
    if (connection) {
      connection.status = "connected";
      connection.peerConnection = peerConnection;
      connection.retryCount = 0;
    }
  }

  setError(cameraId: string): void {
    const connection = this.connections.get(cameraId);
    if (connection) {
      connection.status = "error";
      connection.peerConnection = undefined;
    }
  }

  disconnect(cameraId: string): void {
    const connection = this.connections.get(cameraId);
    if (connection?.peerConnection) {
      connection.peerConnection.close();
    }

    this.connections.set(cameraId, {
      status: "idle",
      lastAttempt: Date.now(),
      retryCount: 0,
    });
  }

  getStatus(cameraId: string): string {
    return this.connections.get(cameraId)?.status || "idle";
  }

  getRetryCount(cameraId: string): number {
    return this.connections.get(cameraId)?.retryCount || 0;
  }

  cleanup(cameraId: string): void {
    const connection = this.connections.get(cameraId);
    if (connection?.peerConnection) {
      connection.peerConnection.close();
    }
    this.connections.delete(cameraId);
    this.connectionQueue = this.connectionQueue.filter(
      (item) => item.cameraId !== cameraId
    );
  }

  queueConnection(
    cameraId: string,
    connectFn: () => Promise<void>,
    priority: number = 0
  ): void {
    if (this.connections.get(cameraId)?.status === "connected") {
      return;
    }

    this.connectionQueue = this.connectionQueue.filter(
      (item) => item.cameraId !== cameraId
    );

    this.connectionQueue.push({ cameraId, connectFn, priority });
    this.connectionQueue.sort((a, b) => b.priority - a.priority);

    this.processQueue();
  }

  private async processQueue(): Promise<void> {
    if (
      this.processingQueue ||
      this.activeConnections >= this.maxConcurrentConnections ||
      this.connectionQueue.length === 0
    ) {
      return;
    }

    this.processingQueue = true;

    while (
      this.connectionQueue.length > 0 &&
      this.activeConnections < this.maxConcurrentConnections
    ) {
      const item = this.connectionQueue.shift();
      if (!item) break;

      if (this.connections.get(item.cameraId)?.status === "connected") {
        continue;
      }

      this.activeConnections++;
      
      item
        .connectFn()
        .catch((error) => {
          console.error(`Connection failed for camera ${item.cameraId}:`, error);
        })
        .finally(() => {
          this.activeConnections--;
          this.processQueue();
        });

      await new Promise((resolve) => setTimeout(resolve, 200));
    }

    this.processingQueue = false;
  }

  getQueuePosition(cameraId: string): number {
    return this.connectionQueue.findIndex((item) => item.cameraId === cameraId);
  }

  setMaxConcurrentConnections(max: number): void {
    this.maxConcurrentConnections = Math.max(1, max);
  }
}

export const videoConnectionManager = VideoConnectionManager.getInstance();
