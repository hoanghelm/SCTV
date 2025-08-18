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
    isLoading,
    error,
    connect,
    disconnect,
    retry
  } = useVideo(camera.id)
  
  // Set the video ref immediately on first render - only once
  React.useEffect(() => {
    if (videoRef.current) {
      const video = videoRef.current
      video.autoplay = true
      video.playsInline = true
      video.muted = true
      video.controls = true
      console.log('Video element configured for camera:', camera.id)
    }
  }, []) // Remove camera.id dependency to prevent re-runs

  React.useEffect(() => {
    if (camera.status === 'Active' && signalRConnected) {
      // Add a small delay to prevent race conditions
      const timeoutId = setTimeout(() => {
        connect()
      }, 100)
      
      return () => clearTimeout(timeoutId)
    }
  }, [camera.id, camera.status, signalRConnected, connect])
  
  // Only cleanup on unmount - not on every dependency change
  React.useEffect(() => {
    return () => {
      disconnect()
    }
  }, [])

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
          {detections.map((detection, index) => (
            <div
              key={index}
              className="detection-box"
              style={{
                left: `${detection.bbox[0]}px`,
                top: `${detection.bbox[1]}px`,
                width: `${detection.bbox[2] - detection.bbox[0]}px`,
                height: `${detection.bbox[3] - detection.bbox[1]}px`
              }}
            >
              <div className="detection-label">
                {detection.label} {Math.round(detection.confidence * 100)}%
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