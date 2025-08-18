import { Camera } from '../types'

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:44322'

export class CameraService {
  private authToken: string = ''

  setAuthToken(token: string) {
    this.authToken = token
  }

  private getHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    }
    
    if (this.authToken) {
      headers['Authorization'] = this.authToken.startsWith('Bearer ') 
        ? this.authToken 
        : `Bearer ${this.authToken}`
    }
    
    return headers
  }

  private async fetchWithRetry(url: string, options: RequestInit = {}, retries = 2): Promise<Response> {
    const fetchOptions: RequestInit = {
      ...options,
      headers: {
        ...this.getHeaders(),
        ...options.headers,
      },
      mode: 'cors',
      credentials: 'omit',
    }

    for (let i = 0; i <= retries; i++) {
      try {
        const response = await fetch(url, fetchOptions)
        return response
      } catch (error) {
        if (i === retries) {
          if (error instanceof TypeError && error.message.includes('fetch')) {
            throw new Error(`Network error: Unable to connect to ${url}. Please check if the server is running and accessible.`)
          }
          throw error
        }
        await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)))
      }
    }
    throw new Error('Unexpected error in fetch retry')
  }

  async getCameras(): Promise<Camera[]> {
    try {
      const response = await this.fetchWithRetry(`${API_BASE_URL}/api/v1/stream/cameras`)
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
      
      const data = await response.json()
      return data.result?.items || data.items || []
    } catch (error) {
      console.error('Error fetching cameras:', error)
      throw error
    }
  }

  async getCameraById(cameraId: string): Promise<Camera> {
    try {
      const response = await this.fetchWithRetry(`${API_BASE_URL}/api/v1/stream/camera/${cameraId}`)
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
      
      const data = await response.json()
      return data.result || data
    } catch (error) {
      console.error(`Error fetching camera ${cameraId}:`, error)
      throw error
    }
  }

  async createTestStream(name: string = 'Test Stream'): Promise<void> {
    try {
      const response = await this.fetchWithRetry(`${API_BASE_URL}/api/v1/stream/test/create-stream`, {
        method: 'POST',
        body: JSON.stringify({
          name,
          type: 'pattern'
        })
      })
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
    } catch (error) {
      console.error('Error creating test stream:', error)
      throw error
    }
  }

  async registerCamera(cameraData: {
    name: string
    location: string
    streamUrl: string
    resolution: string
    frameRate: number
  }): Promise<Camera> {
    try {
      const response = await this.fetchWithRetry(`${API_BASE_URL}/api/v1/stream/camera/register`, {
        method: 'POST',
        body: JSON.stringify({
          name: cameraData.name,
          location: cameraData.location,
          streamUrl: cameraData.streamUrl,
          resolution: cameraData.resolution,
          frameRate: cameraData.frameRate,
          testMode: false
        })
      })
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
      
      const data = await response.json()
      return data.result || data
    } catch (error) {
      console.error('Error registering camera:', error)
      throw error
    }
  }

  async testCameraConnection(streamUrl: string): Promise<boolean> {
    try {
      const response = await this.fetchWithRetry(`${API_BASE_URL}/api/v1/stream/camera/test`, {
        method: 'POST',
        body: JSON.stringify({
          streamUrl
        })
      })
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
      }
      
      const data = await response.json()
      return data.success || false
    } catch (error) {
      console.error('Error testing camera connection:', error)
      return false
    }
  }
}

export const cameraService = new CameraService()