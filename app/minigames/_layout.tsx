import { Stack } from 'expo-router';

export default function MinigamesLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: false,
        animation: 'slide_from_bottom',
        contentStyle: { backgroundColor: '#FFE4F0' },
      }}
    />
  );
}
