/**
 * Creator Studio - Weekend creator activities (makeup, reels, social)
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, ScrollView } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';

const { width: SW } = Dimensions.get('window');

const INTERACTION_REWARDS: Record<string, { stats: Record<string, number>; coins: number; xp: number; sticker?: string }> = {
  vanity: { stats: { fun: 20, popularity: 10 }, coins: 8, xp: 12, sticker: 'sticker_makeup_artist' },
  camera: { stats: { fun: 15, popularity: 15 }, coins: 10, xp: 15, sticker: 'sticker_content_creator' },
  wardrobe: { stats: { fun: 10 }, coins: 5, xp: 8 },
  ring_light: { stats: { fun: 5 }, coins: 3, xp: 5 },
  phone_stand: { stats: { popularity: 8 }, coins: 4, xp: 6 },
  props: { stats: { fun: 12 }, coins: 4, xp: 6 },
  backdrop: { stats: { fun: 8 }, coins: 3, xp: 5 },
  mirror: { stats: { fun: 5 }, coins: 2, xp: 3 },
};

type Activity = { id: string; name: string; emoji: string; desc: string; rewards: { stats: Record<string, number>; coins: number; xp: number } };

const ACTIVITIES: Activity[] = [
  { id: 'makeup', name: 'Makeup Time', emoji: '\uD83D\uDC84', desc: 'Try on cute looks!', rewards: { stats: { fun: 20, popularity: 10 }, coins: 8, xp: 12 } },
  { id: 'photoshoot', name: 'Photo Shoot', emoji: '\uD83D\uDCF8', desc: 'Strike a pose!', rewards: { stats: { fun: 15, popularity: 15 }, coins: 10, xp: 15 } },
  { id: 'reel', name: 'Make a Reel', emoji: '\uD83C\uDFAC', desc: 'Create fun videos!', rewards: { stats: { fun: 18, popularity: 20 }, coins: 12, xp: 18 } },
  { id: 'dressup', name: 'Dress Up', emoji: '\uD83D\uDC57', desc: 'Try outfits!', rewards: { stats: { fun: 15 }, coins: 6, xp: 8 } },
  { id: 'dance_video', name: 'Dance Video', emoji: '\uD83D\uDC83', desc: 'Record a dance!', rewards: { stats: { fun: 20, popularity: 12 }, coins: 10, xp: 15 } },
];

export default function Studio() {
  const { coins, stats, xp, level, updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [phase, setPhase] = useState<'scene' | 'activity' | 'result'>('scene');
  const [activeActivity, setActiveActivity] = useState<Activity | null>(null);
  const [likes, setLikes] = useState(0);

  const xpToNext = 100;

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

  const startActivity = (activity: Activity) => {
    setActiveActivity(activity);
    setPhase('activity');
  };

  const [completing, setCompleting] = useState(false);

  const completeActivity = async () => {
    if (!activeActivity || completing) return;
    setCompleting(true);
    const r = activeActivity.rewards;
    updateStats(r.stats);
    addCoins(r.coins);
    addXP(r.xp);
    addStars(1);
    const randomLikes = Math.floor(Math.random() * 500) + 100;
    setLikes(randomLikes);
    if (activeActivity.id === 'reel') earnSticker('sticker_viral_reel');
    if (activeActivity.id === 'makeup') earnSticker('sticker_glam_queen');
    setPhase('result');
    await saveGame();
    setCompleting(false);
  };

  if (phase === 'activity' && activeActivity) {
    return (
      <View style={styles.container}>
        <View style={styles.activityView}>
          <Text style={styles.actEmoji}>{activeActivity.emoji}</Text>
          <Text style={styles.actName}>{activeActivity.name}</Text>
          <Text style={styles.actDesc}>{activeActivity.desc}</Text>
          <View style={styles.progressBar}>
            <View style={[styles.progressFill, { width: '100%' }]} />
          </View>
          <TouchableOpacity style={styles.doneBtn} onPress={completeActivity}>
            <Text style={styles.doneBtnText}>Done!</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (phase === 'result' && activeActivity) {
    return (
      <View style={styles.container}>
        <View style={styles.resultView}>
          <Text style={styles.actEmoji}>{activeActivity.emoji}</Text>
          <Text style={styles.resultTitle}>Amazing!</Text>
          <View style={styles.likesBox}>
            <Text style={styles.likesText}>{'\u2764\uFE0F'} {likes} likes</Text>
          </View>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>+{'\u20B9'}{activeActivity.rewards.coins} coins</Text>
            <Text style={styles.rewardLine}>+{activeActivity.rewards.xp} XP</Text>
          </View>
          <TouchableOpacity style={styles.doneBtn} onPress={() => { setPhase('scene'); setActiveActivity(null); }}>
            <Text style={styles.doneBtnText}>Back to Studio</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="studio"
          onInteract={handleInteract}
          height={SW * 0.6}
        />
        <GameHUD
          coins={coins}
          stats={stats}
          level={level}
          xp={xp % xpToNext}
          xpToNext={xpToNext}
          roomName="Creator Studio"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
        />
      </View>
      <ScrollView horizontal style={styles.actList} showsHorizontalScrollIndicator={false}>
        {ACTIVITIES.map(act => (
          <TouchableOpacity key={act.id} style={styles.actCard} onPress={() => startActivity(act)}>
            <Text style={styles.actCardEmoji}>{act.emoji}</Text>
            <Text style={styles.actCardName}>{act.name}</Text>
            <Text style={styles.actCardCoins}>+{'\u20B9'}{act.rewards.coins}</Text>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFE4F0' },
  sceneContainer: { flex: 1, position: 'relative' },
  actList: { paddingHorizontal: 12, paddingVertical: 10 },
  actCard: { width: 100, backgroundColor: '#fff', borderRadius: 16, padding: 12, alignItems: 'center', marginRight: 10 },
  actCardEmoji: { fontSize: 28 },
  actCardName: { fontSize: 11, fontWeight: '800', color: '#333', marginTop: 4, textAlign: 'center' },
  actCardCoins: { fontSize: 10, fontWeight: '700', color: '#ff6b9d', marginTop: 2 },
  activityView: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  actEmoji: { fontSize: 72 },
  actName: { fontSize: 26, fontWeight: '800', color: '#333', marginTop: 12 },
  actDesc: { fontSize: 14, color: '#999', marginTop: 6 },
  progressBar: { width: '80%', height: 8, backgroundColor: '#eee', borderRadius: 4, marginTop: 20 },
  progressFill: { height: '100%', backgroundColor: '#ff6b9d', borderRadius: 4 },
  doneBtn: { marginTop: 24, backgroundColor: '#ff6b9d', paddingHorizontal: 30, paddingVertical: 14, borderRadius: 16 },
  doneBtnText: { fontSize: 18, fontWeight: '800', color: '#fff' },
  resultView: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  resultTitle: { fontSize: 30, fontWeight: '800', color: '#333', marginTop: 12 },
  likesBox: { backgroundColor: '#fff', padding: 16, borderRadius: 16, marginTop: 16 },
  likesText: { fontSize: 24, fontWeight: '800', color: '#ff6b9d' },
  rewardBox: { backgroundColor: '#fff', padding: 16, borderRadius: 16, marginTop: 12, gap: 6 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#333' },
});
