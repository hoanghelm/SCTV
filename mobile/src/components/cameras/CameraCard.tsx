import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Dimensions,
} from 'react-native';
import { RTCView } from 'react-native-webrtc';
import { Camera, CameraStatus } from '../../types';
import { theme } from '../../utils/theme';
import { useVideo } from '../../hooks/useVideo';

interface CameraCardProps {
  camera: Camera;
  signalRConnected: boolean;
  onPress?: (camera: Camera) => void;
}

const { width: screenWidth } = Dimensions.get('window');
const cardWidth = (screenWidth - theme.spacing.lg * 3) / 2;

export const CameraCard: React.FC<CameraCardProps> = ({
  camera,
  signalRConnected,
  onPress,
}) => {
  const {
    status,
    stats,
    isLoading,
    error,
    localStream,
    remoteStream,
    connect,
    retry,
  } = useVideo(camera.id);

  React.useEffect(() => {
    if (camera.status === CameraStatus.Active && signalRConnected) {
      const priority = camera.priority || 0;
      connect();
    }
  }, [camera.id, camera.status, signalRConnected, connect]);

  const getStatusColor = () => {
    switch (status) {
      case 'connected':
        return theme.colors.success;
      case 'connecting':
        return theme.colors.warning;
      case 'error':
        return theme.colors.error;
      default:
        return theme.colors.secondary;
    }
  };

  const getStatusText = () => {
    if (!signalRConnected) return 'Waiting...';
    if (error) return 'Error';
    if (isLoading || status === 'connecting') return 'Connecting...';
    if (status === 'connected') return 'Live';
    if (status === 'reconnecting') return 'Reconnecting...';
    return 'Offline';
  };

  const handlePress = () => {
    if (onPress) {
      onPress(camera);
    }
  };

  const handleRetry = () => {
    retry();
  };

  return (
    <TouchableOpacity
      style={styles.container}
      onPress={handlePress}
      activeOpacity={0.8}
    >
      <View style={styles.videoContainer}>
        {status === 'connected' && remoteStream ? (
          <RTCView
            style={styles.video}
            streamURL={remoteStream.toURL()}
            objectFit="cover"
            mirror={false}
            zOrder={0}
          />
        ) : (
          <View style={[styles.placeholder, { backgroundColor: theme.colors.surfaceAlt }]}>
            <Text style={styles.placeholderText}>
              {getStatusText()}
            </Text>
            {error && (
              <TouchableOpacity
                style={styles.retryButton}
                onPress={handleRetry}
              >
                <Text style={styles.retryText}>Retry</Text>
              </TouchableOpacity>
            )}
          </View>
        )}
        
        <View style={[styles.statusBadge, { backgroundColor: getStatusColor() }]}>
          <View style={styles.statusDot} />
        </View>
      </View>

      <View style={styles.infoContainer}>
        <Text style={styles.cameraName} numberOfLines={1}>
          {camera.name}
        </Text>
        <Text style={styles.cameraLocation} numberOfLines={1}>
          {camera.location}
        </Text>
        
        {status === 'connected' && stats && (
          <View style={styles.statsContainer}>
            <Text style={styles.statsText}>
              {stats.fps} FPS • {stats.resolution}
            </Text>
          </View>
        )}
      </View>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  container: {
    width: cardWidth,
    backgroundColor: theme.colors.surface,
    borderRadius: theme.borderRadius.lg,
    marginBottom: theme.spacing.md,
    ...theme.shadows.md,
  },
  videoContainer: {
    position: 'relative',
    height: (cardWidth * 3) / 4,
    borderTopLeftRadius: theme.borderRadius.lg,
    borderTopRightRadius: theme.borderRadius.lg,
    overflow: 'hidden',
  },
  video: {
    width: '100%',
    height: '100%',
  },
  placeholder: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: theme.colors.surfaceAlt,
  },
  placeholderText: {
    color: theme.colors.textSecondary,
    fontSize: theme.fontSize.sm,
    fontWeight: theme.fontWeight.medium,
  },
  statusBadge: {
    position: 'absolute',
    top: theme.spacing.sm,
    right: theme.spacing.sm,
    width: 12,
    height: 12,
    borderRadius: 6,
    justifyContent: 'center',
    alignItems: 'center',
  },
  statusDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: 'white',
  },
  infoContainer: {
    padding: theme.spacing.md,
  },
  cameraName: {
    fontSize: theme.fontSize.md,
    fontWeight: theme.fontWeight.semibold,
    color: theme.colors.text,
    marginBottom: theme.spacing.xs,
  },
  cameraLocation: {
    fontSize: theme.fontSize.sm,
    color: theme.colors.textSecondary,
    marginBottom: theme.spacing.sm,
  },
  statsContainer: {
    paddingTop: theme.spacing.xs,
    borderTopWidth: 1,
    borderTopColor: theme.colors.border,
  },
  statsText: {
    fontSize: theme.fontSize.xs,
    color: theme.colors.textSecondary,
  },
  retryButton: {
    marginTop: theme.spacing.sm,
    paddingVertical: theme.spacing.xs,
    paddingHorizontal: theme.spacing.sm,
    backgroundColor: theme.colors.primary,
    borderRadius: theme.borderRadius.sm,
  },
  retryText: {
    color: 'white',
    fontSize: theme.fontSize.xs,
    fontWeight: theme.fontWeight.medium,
  },
});