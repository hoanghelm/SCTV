import { useRef, useState, useCallback, useEffect } from 'react'
import { Detection } from '../types'
import { signalRService } from '../services/signalRService'
import { connectionManager } from '../services/connectionManager'
import { videoConnectionManager } from '../services/videoConnectionManager'
import { detectionService, DetectionResult } from '../services/detectionService'

interface VideoStats {
  fps: number
  bitrate: number
  resolution: string
}

interface UseVideoReturn {
  videoRef: React.RefObject<HTMLVideoElement | null>
  status: 'connecting' | 'connected' | 'disconnected' | 'error' | 'reconnecting'
  stats: VideoStats | null
  detections: Detection[]
  realtimeDetections: DetectionResult[]
  isLoading: boolean
  error: string | null
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  retry: () => Promise<void>
}

const ICE_SERVERS = [
  { urls: 'stun:stun.l.google.com:19302' },
  { urls: 'stun:stun1.l.google.com:19302' },
  { urls: 'stun:stun2.l.google.com:19302' },
  { urls: 'stun:stun.cloudflare.com:3478' }
]

export const useVideo = (cameraId: string): UseVideoReturn => {
  const videoRef = useRef<HTMLVideoElement>(null)
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null)
  const [status, setStatus] = useState<'connecting' | 'connected' | 'disconnected' | 'error' | 'reconnecting'>('disconnected')
  const [stats, setStats] = useState<VideoStats | null>(null)
  const [detections, setDetections] = useState<Detection[]>([])
  const [realtimeDetections, setRealtimeDetections] = useState<DetectionResult[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [retryCount, setRetryCount] = useState(0)
  const [lastRetryTime, setLastRetryTime] = useState(0)
  const [detectionDisplayTimeout, setDetectionDisplayTimeout] = useState<NodeJS.Timeout | null>(null)
  const maxRetries = 3

  // Initialize detection model on mount
  useEffect(() => {
    detectionService.initialize().then(success => {
      console.log('Detection model initialization:', success ? 'success' : 'failed')
    })

    return () => {
      detectionService.cleanup()
    }
  }, [])

  const startDetection = useCallback((video: HTMLVideoElement) => {
    if (!detectionService.isModelReady()) {
      console.log('Detection model not ready, skipping detection start')
      return
    }

    console.log('Starting detection for video', { 
      width: video.videoWidth, 
      height: video.videoHeight 
    })

    detectionService.startDetectionForVideo(
      cameraId,
      video,
      // Handle detection overlay updates with smooth transitions
      (detections: DetectionResult[]) => {
        // Clear any existing timeout
        if (detectionDisplayTimeout) {
          clearTimeout(detectionDisplayTimeout)
        }

        // If we have existing detections, apply fade-out effect first
        if (realtimeDetections.length > 0) {
          // Mark existing boxes as fading
          setRealtimeDetections(prev => prev.map(det => ({ ...det, fading: true } as any)))
          
          // Wait for fade-out, then show new detections
          setTimeout(() => {
            setRealtimeDetections(detections)
            
            // Auto-hide detections after 4 seconds (same as index.html)
            const timeout = setTimeout(() => {
              // Fade out before removing
              setRealtimeDetections(prev => prev.map(det => ({ ...det, fading: true } as any)))
              setTimeout(() => {
                setRealtimeDetections([])
              }, 150) // Match CSS transition time
            }, 4000)
            
            setDetectionDisplayTimeout(timeout)
          }, 100) // Small delay for smooth transition
        } else {
          // No existing detections, show immediately
          setRealtimeDetections(detections)
          
          // Auto-hide detections after 4 seconds
          const timeout = setTimeout(() => {
            // Fade out before removing
            setRealtimeDetections(prev => prev.map(det => ({ ...det, fading: true } as any)))
            setTimeout(() => {
              setRealtimeDetections([])
            }, 150) // Match CSS transition time
          }, 4000)
          
          setDetectionDisplayTimeout(timeout)
        }
      },
      // Handle realtime detection events for notifications
      (detections: DetectionResult[]) => {
        console.log('Realtime detection event', detections)
        
        // Send notification (same as index.html logic)
        const event = {
          cameraId: cameraId,
          cameraName: `Camera ${cameraId}`,
          detections: detections.map(det => ({
            confidence: det.score,
            bbox: det.bbox,
            class: det.class
          })),
          timestamp: new Date().toISOString(),
          source: 'realtime-browser',
          frameImageBase64: null // Could add frame capture here if needed
        }
        
        // Dispatch to notification system
        window.dispatchEvent(new CustomEvent('personDetected', { detail: event }))
        console.log('Person detection event dispatched:', event)
      }
    )
  }, [cameraId])

  const stopDetection = useCallback(() => {
    detectionService.stopDetectionForVideo(cameraId)
    setRealtimeDetections([])
    
    // Clear any pending display timeout
    if (detectionDisplayTimeout) {
      clearTimeout(detectionDisplayTimeout)
      setDetectionDisplayTimeout(null)
    }
  }, [cameraId, detectionDisplayTimeout])

  const setupWebRTC = useCallback(async (offer: RTCSessionDescriptionInit) => {
    try {
      // Clean up any existing peer connection first
      if (peerConnectionRef.current) {
        peerConnectionRef.current.close()
        peerConnectionRef.current = null
      }
      
      const pcConfig = {
        iceServers: ICE_SERVERS,
        iceCandidatePoolSize: 10,
        iceTransportPolicy: 'all' as RTCIceTransportPolicy,
        bundlePolicy: 'balanced' as RTCBundlePolicy,
        rtcpMuxPolicy: 'require' as RTCRtcpMuxPolicy
      }

      const pc = new RTCPeerConnection(pcConfig)
      peerConnectionRef.current = pc
      

      pc.ontrack = (event) => {
        console.log('Received track event', {
          track: event.track,
          streams: event.streams,
          kind: event.track.kind,
          readyState: event.track.readyState,
          enabled: event.track.enabled,
          muted: event.track.muted
        })

        if (event.streams && event.streams.length > 0) {
          const stream = event.streams[0]
          console.log('Setting video srcObject', {
            streamId: stream.id,
            tracks: stream.getTracks().map(t => ({
              kind: t.kind,
              id: t.id,
              readyState: t.readyState,
              enabled: t.enabled,
              muted: t.muted
            }))
          })
          
          const video = videoRef.current
          if (!video) {
            console.error('Video element not available when stream received')
            setError('Video element not ready')
            setStatus('error')
            setIsLoading(false)
            return
          }

          // Add extensive event listeners for debugging
          video.addEventListener('loadstart', () => console.log('Video: loadstart event'))
          video.addEventListener('loadedmetadata', () => {
            console.log('Video: metadata loaded', {
              videoWidth: video.videoWidth,
              videoHeight: video.videoHeight,
              duration: video.duration
            })
          })
          video.addEventListener('loadeddata', () => console.log('Video: data loaded'))
          video.addEventListener('canplay', () => console.log('Video: can play'))
          video.addEventListener('canplaythrough', () => console.log('Video: can play through'))
          video.addEventListener('playing', () => console.log('Video: is playing'))
          video.addEventListener('play', () => console.log('Video: play event'))
          video.addEventListener('pause', () => console.log('Video: pause event'))
          video.addEventListener('waiting', () => console.log('Video: waiting for data'))
          video.addEventListener('stalled', () => console.log('Video: stalled'))
          video.addEventListener('suspend', () => console.log('Video: suspend'))
          video.addEventListener('abort', () => console.log('Video: abort'))
          video.addEventListener('error', (e) => {
            const target = e.target as HTMLVideoElement
            console.log('Video error', {
              error: target?.error,
              code: target?.error?.code,
              message: target?.error?.message
            })
          })
          
          video.srcObject = stream
          
          // Wait for metadata to ensure video is ready
          video.addEventListener('loadedmetadata', () => {
            const playPromise = video.play()
            if (playPromise !== undefined) {
              playPromise.then(() => {
                console.log('Video started playing successfully')
                setStatus('connected')
                setIsLoading(false)
                
                // Start detection after video is playing and has dimensions
                setTimeout(() => {
                  if (video.videoWidth > 0 && video.videoHeight > 0) {
                    startDetection(video)
                  }
                }, 2000) // Same delay as index.html
              }).catch((err) => {
                console.log('Failed to play video', err)
                if (err.name === 'NotAllowedError') {
                  console.log('Autoplay prevented, user interaction required')
                  setError('Click video to start playback (autoplay blocked)')
                } else {
                  setError('Failed to play video: ' + err.message)
                  setStatus('error')
                  setIsLoading(false)
                }
              })
            }
          }, { once: true })
        } else {
          console.log('No streams in track event - this is unusual')
        }
      }

      pc.onicecandidate = async (event) => {
        if (event.candidate) {
          try {
            if (!signalRService.isConnected()) {
              console.warn('SignalR not connected, skipping ICE candidate')
              return
            }
            await signalRService.sendIceCandidate(cameraId, {
              candidate: event.candidate.candidate,
              sdpMid: event.candidate.sdpMid,
              sdpMLineIndex: event.candidate.sdpMLineIndex
            })
          } catch (err) {
            console.error('Failed to send ICE candidate:', err)
          }
        }
      }

      pc.onconnectionstatechange = () => {
        const state = pc.connectionState
        console.log('Connection state changed', state)
        
        switch (state) {
          case 'connected':
            console.log('WebRTC connection established successfully')
            // Only update if we're not already connected to prevent state confusion
            if (status !== 'connected') {
              setStatus('connected')
              setError(null)
              setIsLoading(false)
              videoConnectionManager.setConnected(cameraId, pc)
              startStatsMonitoring()

              const receivers = pc.getReceivers()
              console.log('Active receivers', receivers.map(r => ({
                track: r.track ? {
                  kind: r.track.kind,
                  id: r.track.id,
                  readyState: r.track.readyState,
                  enabled: r.track.enabled,
                  muted: r.track.muted
                } : null
              })))
            }
            break
          case 'disconnected':
            console.log('WebRTC connection disconnected')
            setStatus('disconnected')
            setStats(null)
            videoConnectionManager.disconnect(cameraId)
            break
          case 'failed':
            console.log('WebRTC connection failed')
            setStatus('error')
            setError('WebRTC connection failed - camera may be offline')
            setIsLoading(false)
            videoConnectionManager.setError(cameraId)
            break
          case 'connecting':
            console.log('WebRTC connection state: connecting')
            if (status !== 'connecting') {
              setStatus('connecting')
            }
            break
          case 'closed':
            console.log('WebRTC connection closed')
            setStatus('disconnected')
            setStats(null)
            videoConnectionManager.disconnect(cameraId)
            break
        }
      }

      pc.oniceconnectionstatechange = () => {
        console.log('ICE connection state', pc.iceConnectionState)
      }

      pc.onicegatheringstatechange = () => {
        console.log('ICE gathering state', pc.iceGatheringState)
      }

      // Validate the offer before setting it
      if (!offer || !offer.type || !offer.sdp) {
        throw new Error('Invalid offer received from server')
      }
      
      try {
        console.log('Setting remote description', offer)
        await pc.setRemoteDescription(offer)
        console.log('Remote description set successfully')
      } catch (error) {
        console.error('Failed to set remote description:', error)
        throw new Error(`Failed to set remote description: ${error}`)
      }
      
      let answer: RTCSessionDescriptionInit
      try {
        console.log('Creating answer')
        answer = await pc.createAnswer({
          offerToReceiveVideo: true,
          offerToReceiveAudio: false
        })
        console.log('Answer created', answer)
      } catch (error) {
        console.error('Failed to create answer:', error)
        throw new Error(`Failed to create answer: ${error}`)
      }
      
      try {
        console.log('Setting local description')
        await pc.setLocalDescription(answer)
        console.log('Local description set successfully')
      } catch (error) {
        console.error('Failed to set local description:', error)
        throw new Error(`Failed to set local description: ${error}`)
      }
      
      let answerResponse: any
      try {
        console.log('Sending answer to SignalR', answer)
        answerResponse = await signalRService.sendAnswer(cameraId, answer)
        console.log('Answer response received', answerResponse)
      } catch (error) {
        throw new Error(`Failed to send answer to server: ${error}`)
      }
      
      if (!answerResponse || !answerResponse.success) {
        console.error('Answer response indicates failure:', answerResponse)
        throw new Error('Server rejected answer: ' + (answerResponse?.error || answerResponse?.message || 'Unknown error'))
      }
      
      
      console.log('Connection successful for camera', cameraId)

    } catch (error) {
      console.error('WebRTC setup error:', error)
      setError(error instanceof Error ? error.message : 'WebRTC setup failed')
      setStatus('error')
      setIsLoading(false)
      throw error
    }
  }, [cameraId])

  const startStatsMonitoring = useCallback(() => {
    const pc = peerConnectionRef.current
    if (!pc) return

    const statsInterval = setInterval(async () => {
      if (pc.connectionState === 'closed' || pc.connectionState === 'failed') {
        clearInterval(statsInterval)
        return
      }

      try {
        const statsReport = await pc.getStats()
        let inboundVideoStats: any = null

        statsReport.forEach((report) => {
          if (report.type === 'inbound-rtp' && report.kind === 'video') {
            inboundVideoStats = report
          }
        })

        if (inboundVideoStats && videoRef.current) {
          const video = videoRef.current
          setStats({
            fps: Math.round(inboundVideoStats.framesPerSecond || 0),
            bitrate: Math.round((inboundVideoStats.bytesReceived * 8) / 1000) || 0,
            resolution: video.videoWidth && video.videoHeight 
              ? `${video.videoWidth}x${video.videoHeight}` 
              : '-'
          })
        }
      } catch (err) {
        console.error('Error getting stats:', err)
      }
    }, 5000)

    return () => clearInterval(statsInterval)
  }, [])

  const connect = useCallback(async () => {
    // Prevent duplicate connection attempts
    if (status === 'connecting' || status === 'connected') {
      console.log(`Connection attempt ignored - already ${status}`)
      return
    }
    
    // Use centralized connection manager to prevent race conditions
    if (!videoConnectionManager.canConnect(cameraId)) {
      return
    }
    
    // Prevent infinite retries
    const currentRetryCount = videoConnectionManager.getRetryCount(cameraId)
    if (currentRetryCount >= maxRetries) {
      setError('Maximum retry attempts exceeded. Please check your camera and network connection.')
      setStatus('error')
      videoConnectionManager.setError(cameraId)
      return
    }

    try {
      videoConnectionManager.startConnection(cameraId)
      setIsLoading(true)
      setStatus('connecting')
      setError(null)
      
      console.log(`Connecting to camera ${cameraId}, attempt ${currentRetryCount + 1}/${maxRetries}`)
      
      // Ensure SignalR is connected with retry logic
      let signalRRetries = 0
      const maxSignalRRetries = 3
      while (!signalRService.isConnected() && signalRRetries < maxSignalRRetries) {
        console.log(`SignalR not connected, attempting to connect... (${signalRRetries + 1}/${maxSignalRRetries})`)
        try {
          await signalRService.connect()
          await new Promise(resolve => setTimeout(resolve, 1000)) // Wait for connection to stabilize
        } catch (err) {
          console.warn(`SignalR connection attempt ${signalRRetries + 1} failed:`, err)
          signalRRetries++
          if (signalRRetries < maxSignalRRetries) {
            await new Promise(resolve => setTimeout(resolve, 2000))
          }
        }
      }
      
      if (!signalRService.isConnected()) {
        throw new Error('SignalR connection failed after multiple attempts')
      }

      const response = await signalRService.requestCameraStream(cameraId)
      
      if (response.success && response.data) {
        await setupWebRTC(response.data)
      } else {
        throw new Error(response.error || 'Failed to request camera stream')
      }
    } catch (error) {
      console.error('Connection error:', error)
      const errorMessage = error instanceof Error ? error.message : 'Connection failed'
      
      // Provide more specific error messages
      let userFriendlyMessage = errorMessage
      if (errorMessage.includes('SignalR')) {
        userFriendlyMessage = 'Unable to connect to streaming service. Please check your network connection.'
      } else if (errorMessage.includes('camera stream')) {
        userFriendlyMessage = 'Camera is not available or offline. Please check camera status.'
      } else if (errorMessage.includes('WebRTC')) {
        userFriendlyMessage = 'Failed to establish video connection. Please try again.'
      }
      
      setError(userFriendlyMessage)
      setStatus('error')
      setIsLoading(false)
      videoConnectionManager.setError(cameraId)
    }
  }, [cameraId, setupWebRTC, maxRetries])

  const disconnect = useCallback(async () => {
    try {
      // Stop detection first
      stopDetection()
      
      // Close peer connection first
      if (peerConnectionRef.current) {
        peerConnectionRef.current.close()
        peerConnectionRef.current = null
      }

      // Clean up video element properly
      if (videoRef.current) {
        const video = videoRef.current
        video.pause()
        video.srcObject = null
        video.load() // Reset video element state
      }

      // Stop camera stream on server
      if (signalRService.isConnected()) {
        try {
          await signalRService.stopCameraStream(cameraId)
        } catch (err) {
          console.warn('Failed to stop camera stream on server:', err)
        }
      }
      
      setStatus('disconnected')
      setStats(null)
      setDetections([])
      setRealtimeDetections([])
      setError(null)
      setIsLoading(false)
      setRetryCount(0)
      
      // Clear detection timeout
      if (detectionDisplayTimeout) {
        clearTimeout(detectionDisplayTimeout)
        setDetectionDisplayTimeout(null)
      }
      
      // Use centralized managers
      videoConnectionManager.disconnect(cameraId)
      connectionManager.releaseConnection(cameraId)
    } catch (error) {
      console.error('Disconnect error:', error)
    }
  }, [cameraId, stopDetection])

  useEffect(() => {
    const handlePersonDetected = (event: any) => {
      if (event.cameraId === cameraId) {
        setDetections(event.detections || [])
        
        setTimeout(() => {
          setDetections([])
        }, 3000)
      }
    }

    signalRService.onPersonDetected(handlePersonDetected)

    return () => {
      // Clean disconnect on component unmount
      disconnect()
    }
  }, [cameraId])

  const retry = useCallback(async () => {
    if (status === 'connecting' || status === 'connected') {
      return
    }
    
    // Prevent rapid retries
    if (Date.now() - lastRetryTime < 5000) {
      console.log('Retry blocked - too soon since last attempt')
      return
    }
    setLastRetryTime(Date.now())
    
    setStatus('reconnecting')
    setError(null)
    
    // Clean up existing connection first
    await disconnect()
    
    // Wait longer before retrying to prevent loops
    await new Promise(resolve => setTimeout(resolve, 3000))
    
    // Attempt to reconnect
    await connect()
  }, [status, lastRetryTime, disconnect, connect])
  
  // Cleanup on unmount
  useEffect(() => {
    return () => {
      videoConnectionManager.cleanup(cameraId)
    }
  }, [cameraId])

  return {
    videoRef,
    status,
    stats,
    detections,
    realtimeDetections,
    isLoading,
    error,
    connect,
    disconnect,
    retry
  }
}