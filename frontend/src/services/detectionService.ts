import * as tf from '@tensorflow/tfjs'
// Import platform-specific backends
import '@tensorflow/tfjs-backend-webgl'
import '@tensorflow/tfjs-backend-cpu'
import * as cocoSsd from '@tensorflow-models/coco-ssd'

export interface DetectionResult {
  class: string
  score: number
  bbox: [number, number, number, number] // [x, y, width, height]
}

export interface DetectionStats {
  fps: number
  totalDetections: number
  avgConfidence: number
}

class DetectionService {
  private model: cocoSsd.ObjectDetection | null = null
  private isLoading = false
  private isEnabled = false
  private detectionIntervals: Map<string, NodeJS.Timeout> = new Map()
  private lastDetectionTime: Map<string, number> = new Map()
  private stats: DetectionStats = {
    fps: 0,
    totalDetections: 0,
    avgConfidence: 0
  }

  async initialize(): Promise<boolean> {
    if (this.model || this.isLoading) {
      return this.model !== null
    }

    try {
      this.isLoading = true
      console.log('Initializing TensorFlow.js...')
      
      // Try to set WebGL backend first, fallback to CPU
      try {
        await tf.setBackend('webgl')
        console.log('WebGL backend set successfully')
      } catch (webglError) {
        console.warn('WebGL backend failed, trying CPU:', webglError)
        await tf.setBackend('cpu')
        console.log('CPU backend set successfully')
      }
      
      // Ensure TensorFlow.js is ready
      await tf.ready()
      console.log('TensorFlow.js backend:', tf.getBackend())
      
      console.log('Loading COCO-SSD detection model...')
      
      // Load with same config as index.html
      this.model = await cocoSsd.load({ base: 'lite_mobilenet_v2' })
      this.isEnabled = true
      
      console.log('COCO-SSD model loaded successfully')
      return true
    } catch (error) {
      console.error('Error loading detection model:', error)
      return false
    } finally {
      this.isLoading = false
    }
  }

  isModelReady(): boolean {
    return this.model !== null && this.isEnabled
  }

  async detectPersonsInVideo(video: HTMLVideoElement): Promise<DetectionResult[]> {
    if (!this.model || !this.isEnabled || video.videoWidth === 0) {
      return []
    }

    try {
      const startTime = performance.now()
      
      const predictions = await this.model.detect(video)
      
      // Filter for persons with confidence > 0.4 (same as index.html)
      const personDetections = predictions
        .filter((prediction: any) => 
          prediction.class === 'person' && prediction.score > 0.4
        )
        .map((prediction: any) => ({
          class: prediction.class,
          score: prediction.score,
          bbox: prediction.bbox as [number, number, number, number]
        }))

      const endTime = performance.now()
      const detectionTime = endTime - startTime
      
      // Update stats (same as index.html)
      this.stats.fps = Math.round(1000 / detectionTime)
      if (personDetections.length > 0) {
        this.stats.totalDetections += personDetections.length
        this.stats.avgConfidence = personDetections.reduce((sum: number, det: DetectionResult) => sum + det.score, 0) / personDetections.length
      }

      return personDetections
    } catch (error) {
      console.error('Detection error:', error)
      return []
    }
  }

  startDetectionForVideo(
    cameraId: string, 
    video: HTMLVideoElement, 
    onDetection: (detections: DetectionResult[]) => void,
    onRealtimeEvent?: (detections: DetectionResult[]) => void
  ): void {
    if (this.detectionIntervals.has(cameraId)) {
      this.stopDetectionForVideo(cameraId)
    }

    console.log('Starting detection for camera:', cameraId)
    
    const interval = setInterval(async () => {
      const detections = await this.detectPersonsInVideo(video)
      
      // Always call onDetection for overlay updates
      onDetection(detections)
      
      // Send realtime events (same logic as index.html)
      if (this.shouldSendDetectionEvent(cameraId, detections) && onRealtimeEvent) {
        onRealtimeEvent(detections)
      }
    }, 1000) // Same interval as index.html
    
    this.detectionIntervals.set(cameraId, interval)
  }

  stopDetectionForVideo(cameraId: string): void {
    const interval = this.detectionIntervals.get(cameraId)
    if (interval) {
      clearInterval(interval)
      this.detectionIntervals.delete(cameraId)
      this.lastDetectionTime.delete(cameraId)
      console.log('Stopped detection for camera:', cameraId)
    }
  }

  stopAllDetections(): void {
    this.detectionIntervals.forEach((interval, cameraId) => {
      clearInterval(interval)
    })
    this.detectionIntervals.clear()
    this.lastDetectionTime.clear()
  }

  private shouldSendDetectionEvent(cameraId: string, detections: DetectionResult[]): boolean {
    if (detections.length === 0) return false

    const currentTime = Date.now()
    const lastTime = this.lastDetectionTime.get(cameraId) || 0

    // Same 3-second throttle as index.html
    if (currentTime - lastTime > 3000) {
      this.lastDetectionTime.set(cameraId, currentTime)
      return true
    }
    return false
  }

  getStats(): DetectionStats {
    return { ...this.stats }
  }

  cleanup(): void {
    this.stopAllDetections()
    this.model = null
    this.isEnabled = false
  }
}

export const detectionService = new DetectionService()