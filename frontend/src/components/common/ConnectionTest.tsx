import React, { useState } from 'react'
import styled from 'styled-components'
import { theme } from '../../styles/theme'
import { Button } from './Button'
import { Card } from './Card'

interface ConnectionTestProps {
  apiUrl: string
}

const TestContainer = styled(Card)`
  margin-bottom: ${theme.sizes.spacing.md};
  padding: ${theme.sizes.spacing.md};
`

const TestResult = styled.div<{ success: boolean }>`
  padding: ${theme.sizes.spacing.sm};
  margin-top: ${theme.sizes.spacing.sm};
  border-radius: ${theme.sizes.borderRadius};
  font-size: 12px;
  background-color: ${props => props.success 
    ? `rgba(0, 186, 124, 0.1)` 
    : `rgba(249, 24, 128, 0.1)`
  };
  color: ${props => props.success ? theme.colors.success : theme.colors.error};
  border: 1px solid ${props => props.success ? theme.colors.success : theme.colors.error};
`

const TestControls = styled.div`
  display: flex;
  gap: ${theme.sizes.spacing.md};
  align-items: center;
  margin-bottom: ${theme.sizes.spacing.sm};
`

export const ConnectionTest: React.FC<ConnectionTestProps> = ({ apiUrl }) => {
  const [testing, setTesting] = useState(false)
  const [result, setResult] = useState<{ success: boolean; message: string } | null>(null)

  const testConnection = async () => {
    setTesting(true)
    setResult(null)
    
    try {
      // Test basic API connectivity
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), 5000)
      
      const response = await fetch(`${apiUrl}/api/v1/stream/cameras`, {
        method: 'GET',
        mode: 'cors',
        credentials: 'omit',
        headers: {
          'Accept': 'application/json',
        },
        signal: controller.signal
      })
      
      clearTimeout(timeoutId)
      
      if (response.ok) {
        setResult({ 
          success: true, 
          message: `✅ API connection successful (${response.status})` 
        })
      } else {
        setResult({ 
          success: false, 
          message: `❌ API responded with ${response.status}: ${response.statusText}` 
        })
      }
    } catch (error) {
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          setResult({ 
            success: false, 
            message: '⏱️ Connection timeout - API server may be down' 
          })
        } else if (error.message.includes('CORS')) {
          setResult({ 
            success: false, 
            message: '🚫 CORS error - Check backend CORS configuration' 
          })
        } else if (error.message.includes('fetch')) {
          setResult({ 
            success: false, 
            message: '🌐 Network error - Cannot reach API server' 
          })
        } else {
          setResult({ 
            success: false, 
            message: `❌ Error: ${error.message}` 
          })
        }
      } else {
        setResult({ 
          success: false, 
          message: '❌ Unknown connection error' 
        })
      }
    } finally {
      setTesting(false)
    }
  }

  return (
    <TestContainer>
      <TestControls>
        <span style={{ fontSize: '14px', fontWeight: '500' }}>
          Connection Test:
        </span>
        <Button 
          $size="small" 
          $variant="secondary" 
          onClick={testConnection}
          disabled={testing}
        >
          {testing ? '🔄 Testing...' : '🔍 Test API'}
        </Button>
      </TestControls>
      
      {result && (
        <TestResult success={result.success}>
          {result.message}
        </TestResult>
      )}
    </TestContainer>
  )
}