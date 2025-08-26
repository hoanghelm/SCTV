import { environment } from '../config/environment';
import { PersonDetectionNotification, NotificationListResponse } from '../types';

export interface GetNotificationsParams {
  page?: number;
  size?: number;
  cameraId?: string;
  fromDate?: string;
  toDate?: string;
}

class NotificationApiService {
  private baseUrl = environment.NOTIFICATIONS_API_URL;

  async getNotifications(params: GetNotificationsParams = {}): Promise<NotificationListResponse> {
    try {
      const queryParams = new URLSearchParams();
      
      if (params.page) queryParams.append('page', params.page.toString());
      if (params.size) queryParams.append('size', params.size.toString());
      if (params.cameraId) queryParams.append('cameraId', params.cameraId);
      if (params.fromDate) queryParams.append('fromDate', params.fromDate);
      if (params.toDate) queryParams.append('toDate', params.toDate);

      const url = `${this.baseUrl}?${queryParams.toString()}`;
      console.log('Fetching notifications from:', url);

      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      console.log('API Response:', result);
      
      if (result.success && result.result) {
        return {
          items: result.result.items || [],
          totalCount: result.result.totalCount || 0,
          page: result.result.page || 1,
          pageSize: result.result.pageSize || 10,
        };
      }

      throw new Error(result.message || 'Failed to fetch notifications');
    } catch (error) {
      console.error('Error fetching notifications:', error);
      throw error;
    }
  }

  async getNotificationById(id: string): Promise<PersonDetectionNotification> {
    try {
      const url = `${this.baseUrl}/${id}`;
      console.log('Fetching notification by id from:', url);

      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      console.log('API Response for single notification:', result);
      
      if (result.success && result.result) {
        return result.result;
      }

      throw new Error(result.message || 'Failed to fetch notification');
    } catch (error) {
      console.error('Error fetching notification by id:', error);
      throw error;
    }
  }

  getImageUrl(frameStoragePath?: string): string | null {
    if (!frameStoragePath) return null;
    
    // Remove any leading slash and path separators
    const cleanPath = frameStoragePath.replace(/^\/+|\\+/g, '').replace(/\\/g, '/');
    
    // Use the notifications API base URL for serving detection images
    const baseImageUrl = environment.NOTIFICATIONS_API_URL.replace('/api/v1/notifications', '');
    return `${baseImageUrl}/detections/${cleanPath}`;
  }
}

export const notificationApiService = new NotificationApiService();