import React, { useState } from 'react'
import { useDispatch } from 'react-redux'
import styled from 'styled-components'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import { Input, Label, FormGroup, FormRow } from '../common/Input'
import { theme } from '../../styles/theme'
import { AppDispatch } from '../../store'
import { loadCameras } from '../../store/slices/camerasSlice'
import { cameraService } from '../../services/cameraService'

interface AddCameraModalProps {
  isOpen: boolean
  onClose: () => void
}

interface CameraFormData {
  name: string
  location: string
  streamUrl: string
  resolution: string
  frameRate: number
}

const ButtonGroup = styled.div`
  display: flex;
  gap: ${theme.sizes.spacing.md};
  justify-content: flex-end;
  margin-top: ${theme.sizes.spacing.lg};
  padding-top: ${theme.sizes.spacing.lg};
  border-top: 1px solid ${theme.colors.border};
`

const TestResult = styled.div<{ success: boolean }>`
  padding: ${theme.sizes.spacing.sm};
  border-radius: ${theme.sizes.borderRadius};
  margin-top: ${theme.sizes.spacing.sm};
  font-size: 12px;
  background-color: ${props => props.success 
    ? `rgba(0, 186, 124, 0.1)` 
    : `rgba(249, 24, 128, 0.1)`
  };
  color: ${props => props.success ? theme.colors.success : theme.colors.error};
  border: 1px solid ${props => props.success ? theme.colors.success : theme.colors.error};
`

const TestButtonContainer = styled.div`
  display: flex;
  align-items: end;
  gap: ${theme.sizes.spacing.sm};
`

export const AddCameraModal: React.FC<AddCameraModalProps> = ({ isOpen, onClose }) => {
  const dispatch = useDispatch<AppDispatch>()
  const [isLoading, setIsLoading] = useState(false)
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null)
  const [formData, setFormData] = useState<CameraFormData>({
    name: '',
    location: '',
    streamUrl: '',
    resolution: '1920x1080',
    frameRate: 30
  })

  const handleInputChange = (field: keyof CameraFormData, value: string | number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }))
    setTestResult(null)
  }

  const handleTestConnection = async () => {
    if (!formData.streamUrl.trim()) {
      setTestResult({ success: false, message: 'Please enter a stream URL first' })
      return
    }

    setIsLoading(true)
    try {
      const success = await cameraService.testCameraConnection(formData.streamUrl)
      setTestResult({
        success,
        message: success 
          ? 'Connection test successful! Camera is reachable.' 
          : 'Connection test failed. Please check the stream URL.'
      })
    } catch (error) {
      setTestResult({
        success: false,
        message: error instanceof Error ? error.message : 'Connection test failed'
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleSubmit = async () => {
    if (!formData.name.trim() || !formData.location.trim() || !formData.streamUrl.trim()) {
      setTestResult({ success: false, message: 'Please fill in all required fields' })
      return
    }

    setIsLoading(true)
    try {
      await cameraService.registerCamera(formData)
      await dispatch(loadCameras())
      
      setTestResult({ success: true, message: 'Camera added successfully!' })
      
      setTimeout(() => {
        onClose()
        setFormData({
          name: '',
          location: '',
          streamUrl: '',
          resolution: '1920x1080',
          frameRate: 30
        })
        setTestResult(null)
      }, 1500)
    } catch (error) {
      setTestResult({
        success: false,
        message: error instanceof Error ? error.message : 'Failed to add camera'
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleCancel = () => {
    onClose()
    setFormData({
      name: '',
      location: '',
      streamUrl: '',
      resolution: '1920x1080',
      frameRate: 30
    })
    setTestResult(null)
  }

  return (
    <Modal isOpen={isOpen} onClose={handleCancel} title="Add New Camera" width="600px">
      <FormGroup>
        <Label>Camera Name *</Label>
        <Input
          type="text"
          placeholder="Enter camera name"
          value={formData.name}
          onChange={(e) => handleInputChange('name', e.target.value)}
          $fullWidth
        />
      </FormGroup>

      <FormGroup>
        <Label>Location *</Label>
        <Input
          type="text"
          placeholder="Enter camera location"
          value={formData.location}
          onChange={(e) => handleInputChange('location', e.target.value)}
          $fullWidth
        />
      </FormGroup>

      <FormGroup>
        <Label>Stream URL *</Label>
        <TestButtonContainer>
          <Input
            type="text"
            placeholder="rtsp://username:password@192.168.1.100:554/stream"
            value={formData.streamUrl}
            onChange={(e) => handleInputChange('streamUrl', e.target.value)}
            style={{ flex: 1 }}
          />
          <Button
            $variant="secondary"
            $size="medium"
            onClick={handleTestConnection}
            disabled={isLoading || !formData.streamUrl.trim()}
          >
            {isLoading ? 'Testing...' : 'Test'}
          </Button>
        </TestButtonContainer>
        {testResult && (
          <TestResult success={testResult.success}>
            {testResult.message}
          </TestResult>
        )}
      </FormGroup>

      <FormRow>
        <FormGroup>
          <Label>Resolution</Label>
          <Input
            type="text"
            placeholder="1920x1080"
            value={formData.resolution}
            onChange={(e) => handleInputChange('resolution', e.target.value)}
            $fullWidth
          />
        </FormGroup>

        <FormGroup>
          <Label>Frame Rate (FPS)</Label>
          <Input
            type="number"
            min="1"
            max="60"
            value={formData.frameRate}
            onChange={(e) => handleInputChange('frameRate', parseInt(e.target.value) || 30)}
            $fullWidth
          />
        </FormGroup>
      </FormRow>

      <ButtonGroup>
        <Button $variant="secondary" onClick={handleCancel} disabled={isLoading}>
          Cancel
        </Button>
        <Button 
          $variant="primary" 
          onClick={handleSubmit} 
          disabled={isLoading || !formData.name.trim() || !formData.location.trim() || !formData.streamUrl.trim()}
        >
          {isLoading ? 'Adding...' : 'Add Camera'}
        </Button>
      </ButtonGroup>
    </Modal>
  )
}