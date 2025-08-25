import { Platform } from 'react-native';

interface EnvironmentConfig {
  API_BASE_URL: string;
}

const getEnvironmentConfig = (): EnvironmentConfig => {
  const isDevelopment = __DEV__;
  
  const getBaseUrl = (): string => {
    if (Platform.OS === 'android') {
      return 'http://10.0.2.2:62162/api/v1/Stream';
    }
    
    return 'https://localhost:44322/api/v1/Stream';
  };

  return {
    API_BASE_URL: getBaseUrl(),
  };
};

export const environment = getEnvironmentConfig();