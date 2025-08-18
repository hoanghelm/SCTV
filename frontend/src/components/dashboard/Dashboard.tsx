import React, { useEffect, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import styled from 'styled-components'
import { theme } from '../../styles/theme'
import { RootState, AppDispatch } from '../../store'
import { loadCameras, createTestStream } from '../../store/slices/camerasSlice'
import { VideoGrid } from './VideoGrid'
import { DashboardHeader } from './DashboardHeader'
import { ConnectionStatus } from './ConnectionStatus'
import { NotificationPanel } from '../common/NotificationPanel'
import { ConnectionTest } from '../common/ConnectionTest'
import { useSignalR } from '../../hooks/useSignalR'

const DashboardContainer = styled.div`
  min-height: 100vh;
  background-color: ${theme.colors.background};
  padding: ${theme.sizes.spacing.lg};
`

const DashboardContent = styled.div`
  max-width: 1400px;
  margin: 0 auto;
`

const MainGrid = styled.div`
  display: grid;
  grid-template-columns: 1fr auto;
  gap: ${theme.sizes.spacing.lg};
  min-height: calc(100vh - 200px);
  
  @media (max-width: 1200px) {
    grid-template-columns: 1fr;
  }
`

const VideoSection = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${theme.sizes.spacing.lg};
`

const SidePanel = styled.div`
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: ${theme.sizes.spacing.lg};
  
  @media (max-width: 1200px) {
    width: 100%;
  }
`

export const Dashboard: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>()
  const [apiUrl, setApiUrl] = useState('https://localhost:44322')
  const [authToken, setAuthToken] = useState('')
  
  const { cameras, selectedCameras } = useSelector((state: RootState) => state.cameras)
  const { showNotifications } = useSelector((state: RootState) => state.dashboard)
  const { isConnected, isConnecting, connect } = useSignalR()

  useEffect(() => {
    dispatch(loadCameras())
  }, [dispatch])

  useEffect(() => {
    if (!isConnected && !isConnecting && apiUrl.trim()) {
      const timeoutId = setTimeout(() => {
        connect(apiUrl, authToken)
      }, 500)
      
      return () => clearTimeout(timeoutId)
    }
  }, [apiUrl, authToken, isConnected, isConnecting, connect])

  const handleApiUrlChange = (url: string) => {
    setApiUrl(url)
  }

  const handleAuthTokenChange = (token: string) => {
    setAuthToken(token)
  }

  const handleCreateTestStream = async () => {
    await dispatch(createTestStream('Test Camera'))
  }

  const activeCameras = cameras.filter(camera => 
    selectedCameras.length > 0 
      ? selectedCameras.includes(camera.id)
      : camera.status === 'Active'
  )

  return (
    <DashboardContainer>
      <DashboardContent>
        <DashboardHeader
          apiUrl={apiUrl}
          authToken={authToken}
          onApiUrlChange={handleApiUrlChange}
          onAuthTokenChange={handleAuthTokenChange}
          onCreateTestStream={handleCreateTestStream}
          onRefreshCameras={() => dispatch(loadCameras())}
        />
        
        <ConnectionTest apiUrl={apiUrl} />
        <ConnectionStatus />
        
        <MainGrid>
          <VideoSection>
            <VideoGrid cameras={activeCameras} signalRConnected={isConnected} />
          </VideoSection>
          
          <SidePanel>
            <NotificationPanel />
          </SidePanel>
        </MainGrid>
      </DashboardContent>
    </DashboardContainer>
  )
}