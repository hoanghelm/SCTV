import React, { useEffect } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createStackNavigator } from '@react-navigation/stack';
import Icon from 'react-native-vector-icons/MaterialIcons';
import { CamerasScreen, NotificationsScreen, NotificationDetailScreen } from './src/screens';
import { theme } from './src/utils/theme';
import NotificationService from './src/services/NotificationService';

const Tab = createBottomTabNavigator();
const Stack = createStackNavigator();

function NotificationStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="NotificationsList" component={NotificationsScreen} />
      <Stack.Screen name="NotificationDetail" component={NotificationDetailScreen} />
    </Stack.Navigator>
  );
}

function App(): JSX.Element {
  useEffect(() => {
    NotificationService.initialize();
  }, []);

  return (
    <NavigationContainer>
      <Tab.Navigator
        initialRouteName="Cameras"
        screenOptions={({ route }) => ({
          tabBarIcon: ({ focused, color, size }) => {
            let iconName: string;

            if (route.name === 'Cameras') {
              iconName = 'videocam';
            } else if (route.name === 'Notifications') {
              iconName = 'notifications';
            } else {
              iconName = 'circle';
            }

            return <Icon name={iconName} size={size} color={color} />;
          },
          tabBarActiveTintColor: theme.colors.primary,
          tabBarInactiveTintColor: theme.colors.secondary,
          tabBarStyle: {
            backgroundColor: theme.colors.surface,
            borderTopColor: theme.colors.border,
            paddingBottom: 8,
            paddingTop: 8,
            height: 60,
          },
          tabBarLabelStyle: {
            fontSize: 12,
            fontWeight: '500',
            marginTop: 4,
          },
          headerShown: false,
        })}
      >
        <Tab.Screen 
          name="Cameras" 
          component={CamerasScreen}
          options={{
            tabBarLabel: 'Cameras',
          }}
        />
        <Tab.Screen 
          name="Notifications" 
          component={NotificationStack}
          options={{
            tabBarLabel: 'Notifications',
          }}
        />
      </Tab.Navigator>
    </NavigationContainer>
  );
}

export default App;
