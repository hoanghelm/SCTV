import React, { useState, useEffect } from 'react';
import {
  View,
  StyleSheet,
  Text,
  StatusBar,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { CameraGrid } from '../components/cameras/CameraGrid';
import { cameraService } from '../services/cameraService';
import { signalRService } from '../services/signalRService';
import { videoConnectionManager } from '../services/videoConnectionManager';
import { Camera } from '../types';
import { theme } from '../utils/theme';

export const CamerasScreen: React.FC = () => {
  const [cameras, setCameras] = useState<Camera[]>([]);
  const [signalRConnected, setSignalRConnected] = useState(false);
  const [loading, setLoading] = useState(false);

  const loadCameras = async () => {
    try {
      setLoading(true);
      const camerasData = await cameraService.getCameras();
      const sortedCameras = camerasData
        .filter(camera => camera.status === 'Active')
        .sort((a, b) => (b.priority || 0) - (a.priority || 0));
      
      setCameras(sortedCameras);
      console.log('Loaded cameras:', sortedCameras.length);
    } catch (error) {
      console.error('Failed to load cameras:', error);
    } finally {
      setLoading(false);
    }
  };

  const initializeSignalR = async () => {
    try {
      await signalRService.connect();
      setSignalRConnected(true);
      console.log('SignalR connection established');
    } catch (error) {
      console.error('Failed to connect to SignalR:', error);
      setTimeout(initializeSignalR, 5000);
    }
  };

  useEffect(() => {
    loadCameras();
    initializeSignalR();

    videoConnectionManager.setMaxConcurrentConnections(2);

    return () => {
      signalRService.disconnect();
    };
  }, []);

  const handleRefresh = () => {
    loadCameras();
    if (!signalRConnected) {
      initializeSignalR();
    }
  };

  const handleCameraPress = (camera: Camera) => {
    console.log('Camera pressed:', camera.name);
  };

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <StatusBar
        barStyle="dark-content"
        backgroundColor={theme.colors.surface}
      />
      
      <View style={styles.header}>
        <Text style={styles.title}>Security Cameras</Text>
        <View style={[styles.connectionStatus, {
          backgroundColor: signalRConnected ? theme.colors.success : theme.colors.error
        }]}>
          <Text style={styles.connectionText}>
            {signalRConnected ? 'Connected' : 'Disconnected'}
          </Text>
        </View>
      </View>

      <CameraGrid
        cameras={cameras}
        signalRConnected={signalRConnected}
        loading={loading}
        onRefresh={handleRefresh}
        onCameraPress={handleCameraPress}
      />
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: theme.colors.background,
  },
  header: {
    backgroundColor: theme.colors.surface,
    paddingHorizontal: theme.spacing.lg,
    paddingVertical: theme.spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: theme.colors.border,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...theme.shadows.sm,
  },
  title: {
    fontSize: theme.fontSize.xl,
    fontWeight: theme.fontWeight.bold,
    color: theme.colors.text,
  },
  connectionStatus: {
    paddingHorizontal: theme.spacing.sm,
    paddingVertical: theme.spacing.xs,
    borderRadius: theme.borderRadius.sm,
  },
  connectionText: {
    color: 'white',
    fontSize: theme.fontSize.xs,
    fontWeight: theme.fontWeight.medium,
  },
});