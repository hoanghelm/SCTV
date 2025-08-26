import React, { useState } from 'react';
import {
  View,
  StyleSheet,
  Text,
  StatusBar,
  ScrollView,
  Image,
  TouchableOpacity,
  Dimensions,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { theme } from '../utils/theme';
import { PersonDetectionNotification } from '../types';
import { notificationApiService } from '../services/notificationApiService';

type NotificationDetailRouteProp = RouteProp<{
  NotificationDetail: {
    notification: PersonDetectionNotification;
  };
}, 'NotificationDetail'>;

const { width } = Dimensions.get('window');

export const NotificationDetailScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<NotificationDetailRouteProp>();
  const { notification } = route.params;
  
  const [imageLoading, setImageLoading] = useState(true);
  const [imageError, setImageError] = useState(false);

  const imageUrl = notificationApiService.getImageUrl(notification.frameStoragePath);

  const formatFullDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  };

  const parseDetectionData = (detectionsData: string) => {
    try {
      const data = JSON.parse(detectionsData);
      return Array.isArray(data) ? data : [data];
    } catch (error) {
      console.error('Error parsing detection data:', error);
      return [];
    }
  };

  const detections = parseDetectionData(notification.detectionsData);

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <StatusBar
        barStyle="dark-content"
        backgroundColor={theme.colors.surface}
      />
      
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
          activeOpacity={0.7}
        >
          <Text style={styles.backText}>‹ Back</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Detection Details</Text>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.imageSection}>
          {imageUrl && !imageError ? (
            <View style={styles.imageContainer}>
              {imageLoading && (
                <View style={styles.imageLoader}>
                  <ActivityIndicator size="large" color={theme.colors.primary} />
                </View>
              )}
              <Image
                source={{ uri: imageUrl }}
                style={styles.detectionImage}
                resizeMode="contain"
                onLoadStart={() => setImageLoading(true)}
                onLoadEnd={() => setImageLoading(false)}
                onError={() => {
                  setImageError(true);
                  setImageLoading(false);
                }}
              />
            </View>
          ) : (
            <View style={styles.placeholderContainer}>
              <View style={styles.placeholderImage}>
                <Text style={styles.placeholderIcon}>📷</Text>
                <Text style={styles.placeholderText}>No image available</Text>
              </View>
            </View>
          )}
        </View>

        <View style={styles.detailsSection}>
          <View style={styles.infoCard}>
            <Text style={styles.cardTitle}>Detection Information</Text>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Camera:</Text>
              <Text style={styles.infoValue}>{notification.cameraName}</Text>
            </View>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Event Type:</Text>
              <Text style={styles.infoValue}>{notification.eventType}</Text>
            </View>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Detection Count:</Text>
              <Text style={styles.infoValue}>
                {notification.detectionCount} person{notification.detectionCount !== 1 ? 's' : ''}
              </Text>
            </View>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Timestamp:</Text>
              <Text style={styles.infoValue}>{formatFullDate(notification.eventTimestamp)}</Text>
            </View>

            {notification.frameStoragePath && (
              <View style={styles.infoRow}>
                <Text style={styles.infoLabel}>Image Path:</Text>
                <Text style={styles.infoValue}>{notification.frameStoragePath}</Text>
              </View>
            )}
          </View>

          {detections.length > 0 && (
            <View style={styles.infoCard}>
              <Text style={styles.cardTitle}>Detection Details</Text>
              
              {detections.map((detection: any, index: number) => (
                <View key={index} style={styles.detectionItem}>
                  <Text style={styles.detectionTitle}>Detection {index + 1}</Text>
                  
                  {detection.label && (
                    <View style={styles.infoRow}>
                      <Text style={styles.infoLabel}>Label:</Text>
                      <Text style={styles.infoValue}>{detection.label}</Text>
                    </View>
                  )}
                  
                  {detection.confidence && (
                    <View style={styles.infoRow}>
                      <Text style={styles.infoLabel}>Confidence:</Text>
                      <Text style={styles.infoValue}>
                        {(detection.confidence * 100).toFixed(1)}%
                      </Text>
                    </View>
                  )}
                  
                  {detection.bbox && Array.isArray(detection.bbox) && (
                    <View style={styles.infoRow}>
                      <Text style={styles.infoLabel}>Bounding Box:</Text>
                      <Text style={styles.infoValue}>
                        [{detection.bbox.map((val: number) => val.toFixed(0)).join(', ')}]
                      </Text>
                    </View>
                  )}
                  
                  {index < detections.length - 1 && <View style={styles.separator} />}
                </View>
              ))}
            </View>
          )}

          <View style={styles.infoCard}>
            <Text style={styles.cardTitle}>System Information</Text>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Notification ID:</Text>
              <Text style={styles.infoValue}>{notification.id}</Text>
            </View>
            
            <View style={styles.infoRow}>
              <Text style={styles.infoLabel}>Camera ID:</Text>
              <Text style={styles.infoValue}>{notification.cameraId}</Text>
            </View>

            {notification.createdAt && (
              <View style={styles.infoRow}>
                <Text style={styles.infoLabel}>Created:</Text>
                <Text style={styles.infoValue}>{formatFullDate(notification.createdAt)}</Text>
              </View>
            )}
          </View>
        </View>
      </ScrollView>
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
    ...theme.shadows.sm,
    flexDirection: 'row',
    alignItems: 'center',
  },
  backButton: {
    marginRight: theme.spacing.md,
    paddingVertical: theme.spacing.xs,
    paddingRight: theme.spacing.sm,
  },
  backText: {
    fontSize: theme.fontSize.lg,
    color: theme.colors.primary,
    fontWeight: theme.fontWeight.medium,
  },
  title: {
    fontSize: theme.fontSize.xl,
    fontWeight: theme.fontWeight.bold,
    color: theme.colors.text,
  },
  content: {
    flex: 1,
  },
  imageSection: {
    backgroundColor: theme.colors.surface,
    margin: theme.spacing.md,
    borderRadius: theme.borderRadius.lg,
    ...theme.shadows.sm,
  },
  imageContainer: {
    position: 'relative',
    minHeight: 200,
  },
  imageLoader: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: theme.colors.surface,
    borderRadius: theme.borderRadius.lg,
    zIndex: 1,
  },
  detectionImage: {
    width: '100%',
    height: 250,
    borderRadius: theme.borderRadius.lg,
  },
  placeholderContainer: {
    padding: theme.spacing.lg,
  },
  placeholderImage: {
    height: 200,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: theme.colors.border,
    borderRadius: theme.borderRadius.md,
  },
  placeholderIcon: {
    fontSize: 48,
    marginBottom: theme.spacing.sm,
  },
  placeholderText: {
    fontSize: theme.fontSize.md,
    color: theme.colors.textSecondary,
  },
  detailsSection: {
    paddingBottom: theme.spacing.xl,
  },
  infoCard: {
    backgroundColor: theme.colors.surface,
    margin: theme.spacing.md,
    marginTop: 0,
    padding: theme.spacing.lg,
    borderRadius: theme.borderRadius.lg,
    ...theme.shadows.sm,
  },
  cardTitle: {
    fontSize: theme.fontSize.lg,
    fontWeight: theme.fontWeight.bold,
    color: theme.colors.text,
    marginBottom: theme.spacing.md,
    paddingBottom: theme.spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: theme.colors.border,
  },
  infoRow: {
    flexDirection: 'row',
    marginBottom: theme.spacing.sm,
    alignItems: 'flex-start',
  },
  infoLabel: {
    fontSize: theme.fontSize.sm,
    color: theme.colors.textSecondary,
    fontWeight: theme.fontWeight.medium,
    width: 120,
    flexShrink: 0,
  },
  infoValue: {
    fontSize: theme.fontSize.sm,
    color: theme.colors.text,
    flex: 1,
    lineHeight: 18,
  },
  detectionItem: {
    marginBottom: theme.spacing.md,
  },
  detectionTitle: {
    fontSize: theme.fontSize.md,
    fontWeight: theme.fontWeight.semibold,
    color: theme.colors.primary,
    marginBottom: theme.spacing.sm,
  },
  separator: {
    height: 1,
    backgroundColor: theme.colors.border,
    marginTop: theme.spacing.md,
  },
});