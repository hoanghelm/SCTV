import messaging from '@react-native-firebase/messaging';
import { Platform, Alert, PermissionsAndroid } from 'react-native';
import PushNotification, { Importance } from 'react-native-push-notification';

class NotificationService {
  async initialize() {
    try {
      this.createNotificationChannels();
      await this.requestPermission();
      await this.subscribeToTopic();
      this.setupMessageHandlers();
      await this.getToken();
    } catch (error) {
      console.error('Failed to initialize notifications:', error);
    }
  }

  createNotificationChannels() {
    PushNotification.createChannel(
      {
        channelId: 'fcm_default_channel',
        channelName: 'Default',
        channelDescription: 'A default channel for notifications',
        soundName: 'default',
        importance: Importance.HIGH,
        vibrate: true,
      },
      (created) => console.log(`createChannel returned '${created}'`)
    );
  }

  async requestPermission() {
    if (Platform.OS === 'android') {
      if (Platform.Version >= 33) {
        const granted = await PermissionsAndroid.request(
          PermissionsAndroid.PERMISSIONS.POST_NOTIFICATIONS
        );
        return granted === PermissionsAndroid.RESULTS.GRANTED;
      }
      return true;
    }
    
    const authStatus = await messaging().requestPermission();
    const enabled =
      authStatus === messaging.AuthorizationStatus.AUTHORIZED ||
      authStatus === messaging.AuthorizationStatus.PROVISIONAL;
      
    if (!enabled) {
      console.log('Push notifications permission not granted');
    }
    
    return enabled;
  }

  async subscribeToTopic() {
    try {
      await messaging().subscribeToTopic('person_detection');
      console.log('Subscribed to person_detection topic');
    } catch (error) {
      console.error('Failed to subscribe to topic:', error);
    }
  }

  async getToken() {
    try {
      const token = await messaging().getToken();
      console.log('FCM Token:', token);
      return token;
    } catch (error) {
      console.error('Failed to get FCM token:', error);
      return null;
    }
  }

  setupMessageHandlers() {
    messaging().onMessage(async (remoteMessage) => {
      console.log('A new FCM message arrived!', remoteMessage);
      
      if (remoteMessage.notification) {
        PushNotification.localNotification({
          title: remoteMessage.notification.title || 'Notification',
          message: remoteMessage.notification.body || 'New notification received',
          channelId: 'fcm_default_channel',
          playSound: true,
          soundName: 'default',
          importance: 'high',
          priority: 'high',
        });
      }
    });

    messaging().onNotificationOpenedApp((remoteMessage) => {
      console.log('Notification caused app to open from background state:', remoteMessage);
    });

    messaging()
      .getInitialNotification()
      .then((remoteMessage) => {
        if (remoteMessage) {
          console.log('Notification caused app to open from quit state:', remoteMessage);
        }
      });

    messaging().onTokenRefresh((token) => {
      console.log('FCM Token refreshed:', token);
    });
  }
}

export default new NotificationService();