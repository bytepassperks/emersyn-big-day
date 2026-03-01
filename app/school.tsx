/**
 * School - 3D classroom with learning activities
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
  desk: { stats: { fun: 5 }, coins: 3, xp: 10, sticker: 'sticker_good_student' },
  blackboard: { stats: { fun: 8, popularity: 5 }, coins: 4, xp: 12 },
  bookshelf: { stats: { fun: 10 }, coins: 3, xp: 8 },
  globe: { stats: { fun: 8 }, coins: 2, xp: 6 },
  art_easel: { stats: { fun: 15 }, coins: 4, xp: 8, sticker: 'sticker_little_artist' },
  clock: { stats: { energy: 3 }, coins: 1, xp: 2 },
  locker: { stats: { fun: 3 }, coins: 2, xp: 3 },
  plant: { stats: { fun: 3 }, coins: 1, xp: 2 },
  teacher_desk: { stats: { popularity: 10 }, coins: 5, xp: 10 },
};

export default function School() {
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
  }, [updateStats, addCoins, addXP, earnSticker, saveGame]);

  const handleNPCTap = useCallback((npc: NPCCharacter) => {
    setNpcDialogue({ npcName: npc.name, text: npc.currentDialogue });
  }, []);

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="school"
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
          roomName="School"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>
      <View style={styles.tip}>
        <Text style={styles.tipText}>Tap the desk to study, blackboard to present, easel to draw!</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#E8F4E8' },
  sceneContainer: { flex: 1, position: 'relative' },
  tip: { padding: 12, backgroundColor: '#fff', alignItems: 'center' },
  tipText: { fontSize: 12, color: '#999', fontWeight: '500' },
});
