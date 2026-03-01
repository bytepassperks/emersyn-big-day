/**
 * GameScene.tsx - Main 3D game view with touch interaction
 * Renders the Three.js scene via GLView and handles touch events
 */
import React, { useRef, useCallback, useEffect } from 'react';
import { View, StyleSheet, Dimensions } from 'react-native';
import { GLView, ExpoWebGLRenderingContext } from 'expo-gl';
import { GameEngine, RoomType, GameCallbacks } from '../engine/GameEngine';
import { InteractableInfo } from '../engine/RoomBuilder';
import { NPCCharacter } from '../engine/NPCCharacter';
import { THREE } from 'expo-three';

interface GameSceneProps {
  roomType: RoomType;
  onInteract?: (interactable: InteractableInfo) => void;
  onNPCTap?: (npc: NPCCharacter) => void;
  onFloorTap?: (position: THREE.Vector3) => void;
  onActivityComplete?: (activityId: string) => void;
  onEngineReady?: (engine: GameEngine) => void;
  height?: number;
}

export default function GameScene({
  roomType,
  onInteract,
  onNPCTap,
  onFloorTap,
  onActivityComplete,
  onEngineReady,
  height,
}: GameSceneProps) {
  const engineRef = useRef<GameEngine | null>(null);
  const glRef = useRef<ExpoWebGLRenderingContext | null>(null);
  const { width: screenWidth } = Dimensions.get('window');
  const sceneHeight = height || screenWidth * 0.85;

  const onContextCreate = useCallback(async (gl: ExpoWebGLRenderingContext) => {
    glRef.current = gl;
    const engine = new GameEngine();
    engineRef.current = engine;

    await engine.init(gl, gl.drawingBufferWidth, gl.drawingBufferHeight);
    engine.loadRoom(roomType);

    const callbacks: GameCallbacks = {
      onInteract,
      onNPCTap,
      onFloorTap,
      onActivityComplete,
    };
    engine.setCallbacks(callbacks);

    engine.startLoop();
    onEngineReady?.(engine);
  }, [roomType]);

  // Update callbacks when they change
  useEffect(() => {
    if (engineRef.current) {
      engineRef.current.setCallbacks({
        onInteract,
        onNPCTap,
        onFloorTap,
        onActivityComplete,
      });
    }
  }, [onInteract, onNPCTap, onFloorTap, onActivityComplete]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (engineRef.current) {
        engineRef.current.dispose();
        engineRef.current = null;
      }
    };
  }, []);

  const handleTouch = useCallback((event: any) => {
    if (!engineRef.current || !glRef.current) return;

    const { locationX, locationY } = event.nativeEvent;
    const gl = glRef.current;

    // Convert touch coordinates to GL coordinates
    const scaleX = gl.drawingBufferWidth / screenWidth;
    const scaleY = gl.drawingBufferHeight / sceneHeight;
    const glX = locationX * scaleX;
    const glY = locationY * scaleY;

    engineRef.current.handleTap(
      glX,
      glY,
      gl.drawingBufferWidth,
      gl.drawingBufferHeight
    );
  }, [screenWidth, sceneHeight]);

  return (
    <View style={[styles.container, { height: sceneHeight }]}>
      <GLView
        style={styles.glView}
        onContextCreate={onContextCreate}
        onTouchEnd={handleTouch}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: '100%',
    borderRadius: 16,
    overflow: 'hidden',
  },
  glView: {
    flex: 1,
  },
});
