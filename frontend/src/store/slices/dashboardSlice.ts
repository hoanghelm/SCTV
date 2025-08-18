import { createSlice, PayloadAction } from '@reduxjs/toolkit'

interface DashboardState {
  layout: 'grid' | 'list'
  gridColumns: number
  showStats: boolean
  showNotifications: boolean
  darkMode: boolean
  autoRefresh: boolean
  refreshInterval: number
}

const initialState: DashboardState = {
  layout: 'grid',
  gridColumns: 2,
  showStats: true,
  showNotifications: true,
  darkMode: true,
  autoRefresh: true,
  refreshInterval: 5000
}

const dashboardSlice = createSlice({
  name: 'dashboard',
  initialState,
  reducers: {
    setLayout: (state, action: PayloadAction<'grid' | 'list'>) => {
      state.layout = action.payload
    },
    setGridColumns: (state, action: PayloadAction<number>) => {
      state.gridColumns = Math.max(1, Math.min(4, action.payload))
    },
    toggleStats: (state) => {
      state.showStats = !state.showStats
    },
    toggleNotifications: (state) => {
      state.showNotifications = !state.showNotifications
    },
    toggleDarkMode: (state) => {
      state.darkMode = !state.darkMode
    },
    toggleAutoRefresh: (state) => {
      state.autoRefresh = !state.autoRefresh
    },
    setRefreshInterval: (state, action: PayloadAction<number>) => {
      state.refreshInterval = Math.max(1000, action.payload)
    }
  }
})

export const {
  setLayout,
  setGridColumns,
  toggleStats,
  toggleNotifications,
  toggleDarkMode,
  toggleAutoRefresh,
  setRefreshInterval
} = dashboardSlice.actions

export default dashboardSlice.reducer