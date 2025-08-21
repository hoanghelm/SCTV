import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import Icon from 'react-native-vector-icons/MaterialIcons';
import { CamerasScreen } from './src/screens/CamerasScreen';
import { NotificationsScreen } from './src/screens/NotificationsScreen';
import { theme } from './src/utils/theme';

const Tab = createBottomTabNavigator();

function App(): JSX.Element {
  return (
    <NavigationContainer>
      <Tab.Navigator
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
          component={NotificationsScreen}
          options={{
            tabBarLabel: 'Notifications',
          }}
        />
      </Tab.Navigator>
    </NavigationContainer>
  );
}

export default App;
