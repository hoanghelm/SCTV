import React from 'react'
import { useSelector } from 'react-redux'
import styled, { css } from 'styled-components'
import { theme } from '../../styles/theme'
import { RootState } from '../../store'
import { Camera } from '../../types'
import { VideoStream } from '../video/VideoStream'

interface VideoGridProps {
  cameras: Camera[]
  signalRConnected?: boolean
}

interface GridContainerProps {
  $columns: number
  $layout: 'grid' | 'list'
}

const getGridColumns = (columns: number, layout: string) => {
  if (layout === 'list') {
    return css`
      grid-template-columns: 1fr;
    `
  }
  
  return css`
    grid-template-columns: repeat(auto-fit, minmax(600px, 1fr));
    
    @media (min-width: 1400px) {
      grid-template-columns: repeat(${Math.min(columns, 3)}, 1fr);
    }
    
    @media (max-width: 900px) {
      grid-template-columns: 1fr;
    }
  `
}

const GridContainer = styled.div<GridContainerProps>`
  display: grid;
  gap: ${theme.sizes.spacing.lg};
  
  ${props => getGridColumns(props.$columns, props.$layout)}
`

const EmptyState = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  background-color: ${theme.colors.surface};
  border-radius: ${theme.sizes.borderRadiusLarge};
  color: ${theme.colors.textSecondary};
  text-align: center;
  padding: ${theme.sizes.spacing.xl};
  box-shadow: ${theme.shadows.card};
`

const EmptyStateTitle = styled.h3`
  font-size: 18px;
  margin-bottom: ${theme.sizes.spacing.md};
  color: ${theme.colors.text};
`

const EmptyStateText = styled.p`
  font-size: 14px;
  line-height: 1.5;
  max-width: 400px;
`

export const VideoGrid: React.FC<VideoGridProps> = ({ cameras, signalRConnected = false }) => {
  const { layout, gridColumns, showStats } = useSelector((state: RootState) => state.dashboard)

  if (cameras.length === 0) {
    return (
      <EmptyState>
        <EmptyStateTitle>No Active Cameras</EmptyStateTitle>
        <EmptyStateText>
          No cameras are currently active or selected. 
          Add cameras to your system or create a test stream to get started.
        </EmptyStateText>
      </EmptyState>
    )
  }

  return (
    <GridContainer $columns={gridColumns} $layout={layout}>
      {cameras.map(camera => (
        <VideoStream
          key={camera.id}
          camera={camera}
          showStats={showStats}
          signalRConnected={signalRConnected}
        />
      ))}
    </GridContainer>
  )
}