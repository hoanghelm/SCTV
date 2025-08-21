import React from 'react';
import {
  View,
  FlatList,
  StyleSheet,
  Text,
  RefreshControl,
} from 'react-native';
import { Camera } from '../../types';
import { CameraCard } from './CameraCard';
import { theme } from '../../utils/theme';

interface CameraGridProps {
  cameras: Camera[];
  signalRConnected: boolean;
  loading?: boolean;
  onRefresh?: () => void;
  onCameraPress?: (camera: Camera) => void;
}

export const CameraGrid: React.FC<CameraGridProps> = ({
  cameras,
  signalRConnected,
  loading = false,
  onRefresh,
  onCameraPress,
}) => {
  const renderCamera = ({ item }: { item: Camera }) => (
    <CameraCard
      camera={item}
      signalRConnected={signalRConnected}
      onPress={onCameraPress}
    />
  );

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      <Text style={styles.emptyTitle}>No Active Cameras</Text>
      <Text style={styles.emptyText}>
        No cameras are currently active or available. Pull down to refresh.
      </Text>
    </View>
  );

  return (
    <View style={styles.container}>
      <FlatList
        data={cameras}
        renderItem={renderCamera}
        keyExtractor={(item) => item.id}
        numColumns={2}
        columnWrapperStyle={styles.row}
        contentContainerStyle={styles.contentContainer}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={loading}
            onRefresh={onRefresh}
            colors={[theme.colors.primary]}
            tintColor={theme.colors.primary}
          />
        }
        ListEmptyComponent={renderEmptyState}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: theme.colors.background,
  },
  contentContainer: {
    padding: theme.spacing.lg,
    paddingBottom: theme.spacing.xxl,
  },
  row: {
    justifyContent: 'space-between',
  },
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: theme.spacing.xxl,
  },
  emptyTitle: {
    fontSize: theme.fontSize.lg,
    fontWeight: theme.fontWeight.semibold,
    color: theme.colors.text,
    marginBottom: theme.spacing.sm,
  },
  emptyText: {
    fontSize: theme.fontSize.md,
    color: theme.colors.textSecondary,
    textAlign: 'center',
    lineHeight: 22,
    paddingHorizontal: theme.spacing.xl,
  },
});