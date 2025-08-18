import React, { useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import styled from 'styled-components'
import { theme } from '../../styles/theme'
import { RootState, AppDispatch } from '../../store'
import { setLayout, setGridColumns, toggleStats, toggleNotifications } from '../../store/slices/dashboardSlice'
import { Button } from '../common/Button'
import { Card } from '../common/Card'
import { Input, Label, FormGroup, FormRow, Select } from '../common/Input'
import { AddCameraModal } from '../modals/AddCameraModal'

interface DashboardHeaderProps {
  apiUrl: string
  authToken: string
  onApiUrlChange: (url: string) => void
  onAuthTokenChange: (token: string) => void
  onCreateTestStream: () => void
  onRefreshCameras: () => void
}

const HeaderContainer = styled.div`
  margin-bottom: ${theme.sizes.spacing.lg};
`

const Title = styled.h1`
  text-align: center;
  margin-bottom: ${theme.sizes.spacing.xl};
  color: ${theme.colors.primary};
  font-size: 32px;
  font-weight: 600;
`

const ControlsGrid = styled.div`
  display: grid;
  grid-template-columns: 1fr auto;
  gap: ${theme.sizes.spacing.lg};
  align-items: end;
  
  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
`

const ConfigSection = styled(Card)`
  padding: ${theme.sizes.spacing.lg};
`

const ActionSection = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${theme.sizes.spacing.md};
  min-width: 300px;
`

const PrimaryButtonGroup = styled.div`
  display: flex;
  gap: ${theme.sizes.spacing.md};
  flex-wrap: wrap;
`

const SecondaryButtonGroup = styled.div`
  display: flex;
  gap: ${theme.sizes.spacing.sm};
  flex-wrap: wrap;
`

const ViewControls = styled.div`
  display: flex;
  gap: ${theme.sizes.spacing.md};
  align-items: center;
  flex-wrap: wrap;
  margin-top: ${theme.sizes.spacing.md};
  padding-top: ${theme.sizes.spacing.md};
  border-top: 1px solid ${theme.colors.border};
`

const StatsText = styled.div`
  font-size: 12px;
  color: ${theme.colors.textSecondary};
  text-align: center;
  margin-top: ${theme.sizes.spacing.sm};
`

export const DashboardHeader: React.FC<DashboardHeaderProps> = ({
  apiUrl,
  authToken,
  onApiUrlChange,
  onAuthTokenChange,
  onCreateTestStream,
  onRefreshCameras
}) => {
  const dispatch = useDispatch<AppDispatch>()
  const { layout, gridColumns, showStats, showNotifications } = useSelector((state: RootState) => state.dashboard)
  const { cameras, loading } = useSelector((state: RootState) => state.cameras)
  const [showAddCameraModal, setShowAddCameraModal] = useState(false)

  const handleLayoutChange = (newLayout: 'grid' | 'list') => {
    dispatch(setLayout(newLayout))
  }

  const handleGridColumnsChange = (columns: number) => {
    dispatch(setGridColumns(columns))
  }

  return (
    <HeaderContainer>
      <Title>🎥 CCTV Stream Dashboard</Title>
      
      <ControlsGrid>
        <ConfigSection>
          <FormRow>
            <FormGroup>
              <Label>API URL</Label>
              <Input
                type="text"
                value={apiUrl}
                onChange={(e) => onApiUrlChange(e.target.value)}
                placeholder="https://localhost:44322"
                $fullWidth
              />
            </FormGroup>
            
            <FormGroup>
              <Label>Auth Token</Label>
              <Input
                type="password"
                value={authToken}
                onChange={(e) => onAuthTokenChange(e.target.value)}
                placeholder="Bearer token (optional)"
                $fullWidth
              />
            </FormGroup>
          </FormRow>
          
          <ViewControls>
            <Label>Layout:</Label>
            <Select
              value={layout}
              onChange={(e) => handleLayoutChange(e.target.value as 'grid' | 'list')}
            >
              <option value="grid">Grid</option>
              <option value="list">List</option>
            </Select>
            
            {layout === 'grid' && (
              <>
                <Label>Columns:</Label>
                <Select
                  value={gridColumns}
                  onChange={(e) => handleGridColumnsChange(parseInt(e.target.value))}
                >
                  <option value={1}>1</option>
                  <option value={2}>2</option>
                  <option value={3}>3</option>
                  <option value={4}>4</option>
                </Select>
              </>
            )}
            
            <Button 
              $size="small"
              $variant={showStats ? "primary" : "secondary"}
              onClick={() => dispatch(toggleStats())}
            >
              Stats
            </Button>
            
            <Button 
              $size="small"
              $variant={showNotifications ? "primary" : "secondary"}
              onClick={() => dispatch(toggleNotifications())}
            >
              Alerts
            </Button>
          </ViewControls>
        </ConfigSection>
        
        <ActionSection>
          <PrimaryButtonGroup>
            <Button 
              $variant="success" 
              onClick={() => setShowAddCameraModal(true)}
            >
              ➕ Add Camera
            </Button>
            
            <Button 
              $variant="primary" 
              onClick={onRefreshCameras}
              disabled={loading}
            >
              {loading ? '🔄 Loading...' : '🔄 Refresh'}
            </Button>
          </PrimaryButtonGroup>
          
          <SecondaryButtonGroup>
            <Button 
              $variant="secondary" 
              $size="small"
              onClick={onCreateTestStream}
            >
              🎬 Test Stream
            </Button>
          </SecondaryButtonGroup>
          
          <StatsText>
            📹 {cameras.length} camera(s) • 🔴 {cameras.filter(c => c.status === 'Active').length} active
          </StatsText>
        </ActionSection>
      </ControlsGrid>

      <AddCameraModal 
        isOpen={showAddCameraModal} 
        onClose={() => setShowAddCameraModal(false)} 
      />
    </HeaderContainer>
  )
}