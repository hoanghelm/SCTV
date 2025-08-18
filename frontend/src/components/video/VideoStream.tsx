import React from 'react'
import styled from 'styled-components'
import { theme } from '../../styles/theme'
import { Camera } from '../../types'
import { StatusBadge } from '../common/StatusBadge'
import { useVideo } from '../../hooks/useVideo'

interface VideoStreamProps {
  camera: Camera
  showStats?: boolean
  signalRConnected?: boolean
}

const VideoContainer = styled.div`
  position: relative;
  background-color: ${theme.colors.surface};
  border-radius: ${theme.sizes.borderRadiusLarge};
  padding: ${theme.sizes.spacing.lg};
  box-shadow: ${theme.shadows.card};
  overflow: hidden;
`

const VideoHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: ${theme.sizes.spacing.md};
`

const VideoTitle = styled.h3`
  font-size: 18px;
  font-weight: 600;
  color: ${theme.colors.text};
  margin: 0;
`

const VideoElement = styled.video`
  width: 100%;
  height: auto;
  border-radius: ${theme.sizes.borderRadius};
  background-color: black;
  min-height: 300px;
  object-fit: cover;
  
  /* Debug styles */
  border: 2px solid red !important;
  
  &:has-source {
    border-color: green !important;
  }
`

const DetectionOverlay = styled.div`
  position: absolute;
  top: ${theme.sizes.spacing.lg};
  left: ${theme.sizes.spacing.lg};
  right: ${theme.sizes.spacing.lg};
  bottom: ${theme.sizes.spacing.lg};
  margin-top: 60px;
  pointer-events: none;
  z-index: 10;
`

const Stats = styled.div`
  margin-top: ${theme.sizes.spacing.md};
  font-size: 12px;
  color: ${theme.colors.textSecondary};
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: ${theme.sizes.spacing.md};
`

const StatItem = styled.div`
  background-color: ${theme.colors.surfaceAlt};
  padding: ${theme.sizes.spacing.sm} ${theme.sizes.spacing.md};
  border-radius: ${theme.sizes.borderRadius};
  text-align: center;
`

const LoadingMessage = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 300px;
  background-color: black;
  border-radius: ${theme.sizes.borderRadius};
  color: ${theme.colors.textSecondary};
  font-size: 14px;
`

const ErrorMessage = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 300px;
  background-color: black;
  border-radius: ${theme.sizes.borderRadius};
  color: ${theme.colors.error};
  font-size: 14px;
  text-align: center;
  padding: ${theme.sizes.spacing.lg};
  gap: ${theme.sizes.spacing.md};
`

const RetryButton = styled.button`
  background-color: ${theme.colors.primary};
  color: white;
  border: none;
  padding: ${theme.sizes.spacing.sm} ${theme.sizes.spacing.md};
  border-radius: ${theme.sizes.borderRadius};
  font-size: 12px;
  cursor: pointer;
  transition: background-color 0.2s;
  
  &:hover {
    background-color: ${theme.colors.primaryHover};
  }
  
  &:disabled {
    background-color: ${theme.colors.surfaceAlt};
    color: ${theme.colors.textSecondary};
    cursor: not-allowed;
  }
`

export const VideoStream: React.FC<VideoStreamProps> = ({ camera, showStats = true, signalRConnected = false }) => {
  const {
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
  } = useVideo(camera.id)
  
  React.useEffect(() => {
    if (videoRef.current) {
      const video = videoRef.current
      video.autoplay = true
      video.playsInline = true
      video.muted = true
      video.controls = true
      console.log('Video element configured for camera:', camera.id)
    }
  }, [])

  React.useEffect(() => {
    if (camera.status === 'Active' && signalRConnected) {
      const timeoutId = setTimeout(() => {
        connect()
      }, 100)
      
      return () => clearTimeout(timeoutId)
    }
  }, [camera.id, camera.status, signalRConnected, connect])
  
  React.useEffect(() => {
    return () => {
      disconnect()
    }
  }, [])

  const calculateDetectionBoxStyle = (detection: any) => {
    const video = videoRef.current
    if (!video || video.videoWidth === 0) return {}

    const videoDisplayWidth = video.offsetWidth
    const videoDisplayHeight = video.offsetHeight
    const videoActualWidth = video.videoWidth
    const videoActualHeight = video.videoHeight

    const videoAspectRatio = videoActualWidth / videoActualHeight
    const displayAspectRatio = videoDisplayWidth / videoDisplayHeight

    let scaleX: number, scaleY: number, offsetX = 0, offsetY = 0

    if (videoAspectRatio > displayAspectRatio) {
      scaleX = videoDisplayWidth / videoActualWidth
      scaleY = scaleX
      offsetY = (videoDisplayHeight - (videoActualHeight * scaleY)) / 2
    } else {
      scaleY = videoDisplayHeight / videoActualHeight
      scaleX = scaleY
      offsetX = (videoDisplayWidth - (videoActualWidth * scaleX)) / 2
    }

    const [x, y, width, height] = detection.bbox

    return {
      position: 'absolute' as const,
      left: (x * scaleX + offsetX) + 'px',
      top: (y * scaleY + offsetY) + 'px',
      width: (width * scaleX) + 'px',
      height: (height * scaleY) + 'px',
      border: '3px solid #00ff00',
      backgroundColor: 'rgba(0, 255, 0, 0.15)',
      pointerEvents: 'none' as const,
      zIndex: 20,
      boxShadow: '0 0 10px rgba(0, 255, 0, 0.5)',
      transition: 'opacity 0.15s ease-in-out',
      opacity: (detection as any).fading ? 0 : 1
    }
  }

  const renderVideoContent = () => {
    return (
      <>
        <video
          ref={videoRef}
          autoPlay
          playsInline  
          muted
          controls
          style={{
            width: '100%',
            height: 'auto',
            borderRadius: '8px',
            backgroundColor: 'black',
            minHeight: '300px',
            border: '2px solid green',
            display: (!signalRConnected || error || isLoading || status === 'connecting' || status === 'reconnecting') ? 'none' : 'block'
          }}
          onClick={() => {
            const video = videoRef.current
            if (video && video.paused) {
              video.play().catch(() => {})
            }
          }}
        />
        
        {!signalRConnected && (
          <LoadingMessage>Waiting for streaming service connection...</LoadingMessage>
        )}
        
        {signalRConnected && error && (
          <ErrorMessage>
            <div>{error}</div>
            <RetryButton 
              onClick={retry} 
              disabled={status === 'connecting' || status === 'reconnecting'}
            >
              {status === 'reconnecting' ? 'Retrying...' : 'Retry Connection'}
            </RetryButton>
          </ErrorMessage>
        )}

        {signalRConnected && !error && (isLoading || status === 'connecting') && (
          <LoadingMessage>Connecting to {camera.name}...</LoadingMessage>
        )}
        
        {signalRConnected && !error && status === 'reconnecting' && (
          <LoadingMessage>Reconnecting to {camera.name}...</LoadingMessage>
        )}

        <DetectionOverlay id={`detections-${camera.id}`}>
          {realtimeDetections.map((detection, index) => (
            <div
              key={index}
              className="detection-box"
              style={calculateDetectionBoxStyle(detection)}
            >
              <div 
                className="detection-label"
                style={{
                  position: 'absolute',
                  top: '-30px',
                  left: '0',
                  backgroundColor: '#00ff00',
                  color: 'black',
                  padding: '4px 8px',
                  fontSize: '12px',
                  fontWeight: 'bold',
                  borderRadius: '4px',
                  whiteSpace: 'nowrap',
                  boxShadow: '0 2px 4px rgba(0, 0, 0, 0.3)'
                }}
              >
                Person {Math.round(detection.score * 100)}%
              </div>
            </div>
          ))}
        </DetectionOverlay>
      </>
    )
  }

  return (
    <VideoContainer>
      <VideoHeader>
        <VideoTitle>{camera.name}</VideoTitle>
        <StatusBadge $status={status} />
      </VideoHeader>
      
      {renderVideoContent()}
      
      {showStats && stats && status === 'connected' && (
        <Stats>
          <StatItem>
            <div>FPS: {stats.fps || 0}</div>
          </StatItem>
          <StatItem>
            <div>Bitrate: {stats.bitrate || 0} kbps</div>
          </StatItem>
          <StatItem>
            <div>Resolution: {stats.resolution || '-'}</div>
          </StatItem>
        </Stats>
      )}
    </VideoContainer>
  )
}