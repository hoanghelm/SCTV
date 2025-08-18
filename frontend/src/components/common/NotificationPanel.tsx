import React, { useState, useEffect } from 'react'
import styled from 'styled-components'
import { theme } from '../../styles/theme'

interface DetectionNotification {
  id: string
  cameraId: string
  cameraName: string
  detections: Array<{
    confidence: number
    bbox: [number, number, number, number]
    class: string
  }>
  timestamp: string
  source: string
  frameImageBase64?: string | null
}

const NotificationContainer = styled.div`
  background-color: ${theme.colors.surface};
  border-radius: ${theme.sizes.borderRadiusLarge};
  padding: ${theme.sizes.spacing.lg};
  box-shadow: ${theme.shadows.card};
  max-height: 400px;
  overflow-y: auto;
  margin-bottom: ${theme.sizes.spacing.lg};
`

const NotificationTitle = styled.h3`
  color: ${theme.colors.text};
  margin: 0 0 ${theme.sizes.spacing.md} 0;
  font-size: 18px;
  font-weight: 600;
`

const NotificationItem = styled.div`
  background-color: ${theme.colors.surfaceAlt};
  padding: ${theme.sizes.spacing.md};
  margin-bottom: ${theme.sizes.spacing.sm};
  border-radius: ${theme.sizes.borderRadius};
  display: flex;
  align-items: center;
  gap: ${theme.sizes.spacing.md};
  border-left: 4px solid #00ba7c;
  transition: all 0.3s ease;

  &:hover {
    background-color: ${theme.colors.background};
  }
`

const NotificationContent = styled.div`
  flex: 1;
`

const NotificationHeader = styled.div`
  font-weight: 600;
  color: ${theme.colors.text};
  margin-bottom: ${theme.sizes.spacing.xs};
`

const NotificationTime = styled.div`
  font-size: 11px;
  color: ${theme.colors.textSecondary};
  margin-bottom: ${theme.sizes.spacing.xs};
`

const NotificationDetails = styled.div`
  font-size: 12px;
  color: ${theme.colors.textSecondary};
`

const DetectionImage = styled.img`
  width: 60px;
  height: 45px;
  border-radius: ${theme.sizes.borderRadius};
  object-fit: cover;
  cursor: pointer;
  transition: transform 0.2s ease;

  &:hover {
    transform: scale(1.1);
  }
`

const ClearButton = styled.button`
  background-color: ${theme.colors.primary};
  color: white;
  border: none;
  padding: ${theme.sizes.spacing.sm} ${theme.sizes.spacing.md};
  border-radius: ${theme.sizes.borderRadius};
  font-size: 12px;
  cursor: pointer;
  transition: background-color 0.2s;
  margin-bottom: ${theme.sizes.spacing.md};

  &:hover {
    background-color: ${theme.colors.primaryHover};
  }
`

export const NotificationPanel: React.FC = () => {
  const [notifications, setNotifications] = useState<DetectionNotification[]>([])

  useEffect(() => {
    const handlePersonDetected = (event: CustomEvent) => {
      const detection = event.detail as DetectionNotification
      const newNotification: DetectionNotification = {
        ...detection,
        id: `${detection.cameraId}-${Date.now()}`,
        cameraName: detection.cameraName || `Camera ${detection.cameraId}`
      }

      setNotifications(prev => [newNotification, ...prev.slice(0, 49)]) // Keep max 50 notifications
    }

    window.addEventListener('personDetected', handlePersonDetected as EventListener)

    return () => {
      window.removeEventListener('personDetected', handlePersonDetected as EventListener)
    }
  }, [])

  const clearNotifications = () => {
    setNotifications([])
  }

  const formatTime = (timestamp: string) => {
    return new Date(timestamp).toLocaleTimeString()
  }

  if (notifications.length === 0) {
    return (
      <NotificationContainer>
        <NotificationTitle>Notifications</NotificationTitle>
        <NotificationDetails style={{ textAlign: 'center', padding: '20px' }}>
          No detections yet
        </NotificationDetails>
      </NotificationContainer>
    )
  }

  return (
    <NotificationContainer>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <NotificationTitle>Notifications ({notifications.length})</NotificationTitle>
        <ClearButton onClick={clearNotifications}>Clear All</ClearButton>
      </div>
      
      {notifications.map((notification) => (
        <NotificationItem key={notification.id}>
          <NotificationContent>
            <NotificationHeader>
              Person Detected - {notification.cameraName}
            </NotificationHeader>
            <NotificationTime>
              {formatTime(notification.timestamp)}
            </NotificationTime>
            <NotificationDetails>
              {notification.detections.length} person(s) detected
              {notification.detections.length > 0 && (
                <span> (Avg: {Math.round(
                  notification.detections.reduce((sum, det) => sum + det.confidence, 0) / 
                  notification.detections.length * 100
                )}%)</span>
              )}
            </NotificationDetails>
          </NotificationContent>
          
          {notification.frameImageBase64 && (
            <DetectionImage
              src={`data:image/jpeg;base64,${notification.frameImageBase64}`}
              alt="Detection frame"
              onClick={() => {
                // Could open modal here
                console.log('Show detection image')
              }}
            />
          )}
        </NotificationItem>
      ))}
    </NotificationContainer>
  )
}