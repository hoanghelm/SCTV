# SCTV Mobile App

React Native mobile application for SCTV security camera monitoring system.

## Features

- **Camera Grid**: Auto-connecting camera list with async loading
- **Tab Navigation**: Clean bottom tab navigation (Cameras/Notifications)
- **WebRTC Streaming**: Mock video streaming with connection management
- **Priority Loading**: Smart camera loading with queue management
- **Modern UI**: Following frontend design patterns with consistent theming

## Setup

1. **Install Dependencies**
   ```bash
   npm install
   ```

2. **Link Vector Icons (Android)**
   ```bash
   npx react-native link react-native-vector-icons
   ```

3. **Run Metro**
   ```bash
   npm start
   ```

4. **Run Android**
   ```bash
   npm run android
   ```

5. **Run iOS**
   ```bash
   npm run ios
   ```

## Architecture

```
src/
├── components/
│   └── cameras/          # Camera UI components
├── screens/              # Screen components
├── services/             # API and connection services
├── hooks/                # Custom React hooks
├── types/                # TypeScript definitions
└── utils/                # Theme and utilities
```

## Key Components

- **CameraCard**: Individual camera display with status
- **CameraGrid**: Grid layout with pull-to-refresh
- **useVideo**: WebRTC connection management hook
- **videoConnectionManager**: Async connection pooling
- **signalRService**: Mock SignalR connection service

## Configuration

- **Connection Pool**: Max 2 concurrent mobile connections
- **Auto-Connect**: Cameras connect automatically when active
- **Priority Loading**: Higher priority cameras load first
- **Mock Data**: Uses demo cameras when API unavailable

## Camera Status

- 🟢 **Live**: Connected and streaming
- 🟡 **Connecting**: Establishing connection
- 🔴 **Error**: Connection failed
- ⚪ **Offline**: Camera inactive

Connection status is displayed in real-time with retry functionality.