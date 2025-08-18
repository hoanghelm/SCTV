import { createSlice, PayloadAction } from '@reduxjs/toolkit'
import { StreamConnection, PersonDetectionEvent, AlertNotification } from '../../types'

interface StreamingState {
  connections: Record<string, StreamConnection>
  signalRConnection: any | null
  connectionStatus: 'connecting' | 'connected' | 'disconnected'
  detectionEvents: PersonDetectionEvent[]
  notifications: AlertNotification[]
  error: string | null
}

const initialState: StreamingState = {
  connections: {},
  signalRConnection: null,
  connectionStatus: 'disconnected',
  detectionEvents: [],
  notifications: [],
  error: null
}

const streamingSlice = createSlice({
  name: 'streaming',
  initialState,
  reducers: {
    setSignalRConnection: (state, action: PayloadAction<any>) => {
      state.signalRConnection = action.payload
    },
    setConnectionStatus: (state, action: PayloadAction<'connecting' | 'connected' | 'disconnected'>) => {
      state.connectionStatus = action.payload
    },
    addConnection: (state, action: PayloadAction<StreamConnection>) => {
      state.connections[action.payload.id] = action.payload
    },
    updateConnection: (state, action: PayloadAction<{ id: string; updates: Partial<StreamConnection> }>) => {
      const connection = state.connections[action.payload.id]
      if (connection) {
        Object.assign(connection, action.payload.updates)
      }
    },
    removeConnection: (state, action: PayloadAction<string>) => {
      delete state.connections[action.payload]
    },
    addDetectionEvent: (state, action: PayloadAction<PersonDetectionEvent>) => {
      state.detectionEvents = [action.payload, ...state.detectionEvents].slice(0, 100)
    },
    addNotification: (state, action: PayloadAction<AlertNotification>) => {
      state.notifications = [action.payload, ...state.notifications].slice(0, 50)
    },
    clearNotifications: (state) => {
      state.notifications = []
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload
    }
  }
})

export const {
  setSignalRConnection,
  setConnectionStatus,
  addConnection,
  updateConnection,
  removeConnection,
  addDetectionEvent,
  addNotification,
  clearNotifications,
  setError
} = streamingSlice.actions

export default streamingSlice.reducer