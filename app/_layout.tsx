import React, { useEffect } from 'react';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useGameStore } from '@/store/gameStore';

export default function RootLayout() {
  const loadGame = useGameStore((s) => s.loadGame);

  useEffect(() => {
    loadGame();
  }, []);

  return (
    <>
      <StatusBar style="dark" />
      <Stack
        screenOptions={{
          headerShown: false,
          animation: 'slide_from_right',
          contentStyle: { backgroundColor: '#FFE4F0' },
        }}
      />
    </>
  );
}
