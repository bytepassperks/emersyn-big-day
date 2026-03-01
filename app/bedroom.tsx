/**
 * Bedroom - 3D interactive bedroom with morning/bedtime activities
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
  bed: { stats: { energy: 30 }, coins: 5, xp: 10, sticker: 'sticker_bedtime' },
  wardrobe: { stats: { fun: 10 }, coins: 3, xp: 5, sticker: 'sticker_first_wake' },
  desk: { stats: { fun: 5 }, coins: 2, xp: 8 },
  toybox: { stats: { fun: 15 }, coins: 3, xp: 5 },
  lamp: { stats: { energy: 5 }, coins: 1, xp: 2 },
  nightstand: { stats: { energy: 5 }, coins: 1, xp: 3 },
  window: { stats: { fun: 5, energy: 5 }, coins: 2, xp: 3 },
  rug: { stats: { fun: 3 }, coins: 1, xp: 2 },
};

export default function Bedroom() {
  const { coins, stats, xp, level, updateStats, addCoins, addXP, earnSticker, saveGame } = useGameStore();
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [npcDialogue, setNpcDialogue] = useState<{ npcName: string; text: string } | null>(null);

  const xpToNext = level * 100;

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    const reward = INTERACTION_REWARDS[interactable.id] || { stats: { fun: 3 }, coins: 1, xp: 2 };
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
          roomType="bedroom"
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
          roomName="Emersyn's Bedroom"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>
      <View style={styles.tip}>
        <Text style={styles.tipText}>Tap the bed to sleep, wardrobe to dress up, toys to play!</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFE4F0' },
  sceneContainer: { flex: 1, position: 'relative' },
  tip: { padding: 12, backgroundColor: '#fff', alignItems: 'center' },
  tipText: { fontSize: 12, color: '#999', fontWeight: '500' },
});
