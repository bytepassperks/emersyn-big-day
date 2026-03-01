/**
 * Park - 3D outdoor playground with activities
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Dimensions } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';
import { NPCCharacter } from '@/engine/NPCCharacter';

const { width: SW } = Dimensions.get('window');

const INTERACTION_REWARDS: Record<string, { stats: Record<string, number>; coins: number; xp: number; sticker?: string }> = {
  swing: { stats: { fun: 20, energy: -5 }, coins: 5, xp: 8, sticker: 'sticker_swing_master' },
  slide: { stats: { fun: 15 }, coins: 4, xp: 6 },
  seesaw: { stats: { fun: 15, popularity: 5 }, coins: 4, xp: 6 },
  sandbox: { stats: { fun: 10 }, coins: 3, xp: 5 },
  bench: { stats: { energy: 15 }, coins: 2, xp: 3 },
  tree: { stats: { fun: 8, energy: 5 }, coins: 2, xp: 4 },
  flowers: { stats: { fun: 5 }, coins: 2, xp: 3 },
  fountain: { stats: { fun: 10, cleanliness: 5 }, coins: 3, xp: 5 },
  kite: { stats: { fun: 20 }, coins: 5, xp: 8 },
  bicycle: { stats: { fun: 18, energy: -8 }, coins: 5, xp: 10, sticker: 'sticker_cyclist' },
};

export default function Park() {
  const { coins, stats, xp, level, updateStats, addCoins, addXP, earnSticker, saveGame } = useGameStore();
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [npcDialogue, setNpcDialogue] = useState<{ npcName: string; text: string } | null>(null);

  const xpToNext = level * 100;

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    const reward = INTERACTION_REWARDS[interactable.id] || { stats: { fun: 5 }, coins: 2, xp: 3 };
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
  }, [updateStats, addCoins, addXP, earnSticker, saveGame]);

  const handleNPCTap = useCallback((npc: NPCCharacter) => {
    setNpcDialogue({ npcName: npc.name, text: npc.currentDialogue });
  }, []);

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="park"
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
          roomName="Park"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>
      <View style={styles.tip}>
        <Text style={styles.tipText}>Tap the swings, slide, or sandbox to play!</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#D4F0D4' },
  sceneContainer: { flex: 1, position: 'relative' },
  tip: { padding: 12, backgroundColor: '#fff', alignItems: 'center' },
  tipText: { fontSize: 12, color: '#999', fontWeight: '500' },
});
