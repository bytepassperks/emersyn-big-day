/**
 * Bathroom - 3D interactive bathroom with cleaning activities
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Dimensions, Alert } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { getRandomEncouragement } from '@/lib/helpers';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';
import { NPCCharacter } from '@/engine/NPCCharacter';

const { width: SW } = Dimensions.get('window');

const INTERACTION_REWARDS: Record<string, { stats: Record<string, number>; coins: number; xp: number; sticker?: string }> = {
  bathtub: { stats: { cleanliness: 30, fun: 10 }, coins: 5, xp: 8, sticker: 'sticker_bath_time' },
  sink: { stats: { cleanliness: 15 }, coins: 3, xp: 5, sticker: 'sticker_brush_teeth' },
  toilet: { stats: { cleanliness: 5 }, coins: 1, xp: 2 },
  towel_rack: { stats: { cleanliness: 10 }, coins: 2, xp: 3 },
  rubber_duck: { stats: { fun: 15 }, coins: 3, xp: 5 },
  bath_mat: { stats: { cleanliness: 3 }, coins: 1, xp: 2 },
};

export default function Bathroom() {
  const { coins, stats, xp, level, updateStats, addCoins, addXP, earnSticker, saveGame } = useGameStore();
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [npcDialogue, setNpcDialogue] = useState<{ npcName: string; text: string } | null>(null);

  const xpToNext = 100;

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    const reward = INTERACTION_REWARDS[interactable.id] || { stats: { cleanliness: 5 }, coins: 1, xp: 2 };
    updateStats(reward.stats);
    if (reward.coins > 0) {
      addCoins(reward.coins);
      setCoinDelta(reward.coins);
      setShowCoinAnim(true);
      setTimeout(() => setShowCoinAnim(false), 1000);
    }
    addXP(reward.xp);
    if (reward.sticker) earnSticker(reward.sticker);
    saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${reward.coins} coins!`);
  }, [updateStats, addCoins, addXP, earnSticker, saveGame]);

  const handleNPCTap = useCallback((npc: NPCCharacter) => {
    setNpcDialogue({ npcName: npc.name, text: npc.currentDialogue });
  }, []);

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="bathroom"
          onInteract={handleInteract}
          onNPCTap={handleNPCTap}
          height={SW * 0.85}
        />
        <GameHUD
          coins={coins}
          stats={stats}
          level={level}
          xp={xp % xpToNext}
          xpToNext={xpToNext}
          roomName="Bathroom"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>
      <View style={styles.tip}>
        <Text style={styles.tipText}>Tap the bathtub for a bubble bath, sink to wash up, duck to play!</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#E0F2F8' },
  sceneContainer: { flex: 1, position: 'relative' },
  tip: { padding: 12, backgroundColor: '#fff', alignItems: 'center' },
  tipText: { fontSize: 12, color: '#999', fontWeight: '500' },
});
