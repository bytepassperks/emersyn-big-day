/**
 * Home Hub - Main screen with 3D room and navigation to other rooms
 */
import React, { useState, useCallback, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Dimensions,
} from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { getSegmentName, getTimeOfDayEmoji } from '@/lib/helpers';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';
import { NPCCharacter } from '@/engine/NPCCharacter';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

const ROOMS = [
  { id: 'bedroom', name: 'Bedroom', emoji: '\uD83D\uDECF\uFE0F', color: '#FFE4F0', route: '/bedroom' },
  { id: 'bathroom', name: 'Bathroom', emoji: '\uD83D\uDEC1', color: '#E0F2F8', route: '/bathroom' },
  { id: 'kitchen', name: 'Kitchen', emoji: '\uD83C\uDF73', color: '#FFF0D4', route: '/cooking' },
  { id: 'school', name: 'School', emoji: '\uD83C\uDFEB', color: '#F0E6FF', route: '/school' },
  { id: 'park', name: 'Park', emoji: '\uD83C\uDFDE\uFE0F', color: '#C8E6C9', route: '/park' },
  { id: 'arcade', name: 'Arcade', emoji: '\uD83C\uDFAE', color: '#E8D5FF', route: '/arcade' },
  { id: 'studio', name: 'Studio', emoji: '\uD83C\uDFAC', color: '#FFE0E8', route: '/studio' },
  { id: 'shop', name: 'Shop', emoji: '\uD83D\uDECD\uFE0F', color: '#FFF8DC', route: '/shop' },
  { id: 'stickers', name: 'Stickers', emoji: '\u2B50', color: '#FFF5CC', route: '/stickers' },
  { id: 'settings', name: 'Settings', emoji: '\u2699\uFE0F', color: '#F0F0F0', route: '/settings' },
];

export default function HomeHub() {
  const {
    coins, stars, xp, level, stats, playerName,
    currentSegment, dayType, currentDay,
    updateStats, addCoins, addXP, saveGame,
  } = useGameStore();

  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [npcDialogue, setNpcDialogue] = useState<{ npcName: string; text: string } | null>(null);

  const xpToNext = 100;

  useEffect(() => {
    const interval = setInterval(() => { saveGame(); }, 30000);
    return () => clearInterval(interval);
  }, [saveGame]);

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    const rewards: Record<string, { stats: Record<string, number>; coins: number; xp: number }> = {
      couch: { stats: { energy: 10, fun: 5 }, coins: 2, xp: 5 },
      tv: { stats: { fun: 15 }, coins: 3, xp: 5 },
      door_outside: { stats: {}, coins: 0, xp: 0 },
    };
    const reward = rewards[interactable.id] || { stats: { fun: 5 }, coins: 2, xp: 3 };
    if (Object.keys(reward.stats).length > 0) updateStats(reward.stats);
    if (reward.coins > 0) {
      addCoins(reward.coins);
      setCoinDelta(reward.coins);
      setShowCoinAnim(true);
      setTimeout(() => setShowCoinAnim(false), 1000);
    }
    if (reward.xp > 0) addXP(reward.xp);
    saveGame();
  }, [updateStats, addCoins, addXP, saveGame]);

  const handleNPCTap = useCallback((npc: NPCCharacter) => {
    setNpcDialogue({ npcName: npc.name, text: npc.currentDialogue });
  }, []);

  const isWeekend = dayType === 'weekend';

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="home"
          onInteract={handleInteract}
          onNPCTap={handleNPCTap}
          height={SCREEN_WIDTH * 0.75}
        />
        <GameHUD
          coins={coins}
          stats={stats}
          level={level}
          xp={xp % xpToNext}
          xpToNext={xpToNext}
          roomName={`${getTimeOfDayEmoji(currentSegment)} ${playerName}'s Home`}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>

      <View style={styles.dayBar}>
        <Text style={styles.dayText}>
          Day {currentDay} {'\u2022'} {isWeekend ? 'Weekend' : 'Weekday'} {'\u2022'} {getSegmentName(currentSegment)}
        </Text>
        <View style={styles.starsBadge}>
          <Text style={styles.starsText}>{'\u2B50'} {stars}</Text>
        </View>
      </View>

      <ScrollView style={styles.scrollArea} contentContainerStyle={styles.gridContainer} showsVerticalScrollIndicator={false}>
        <Text style={styles.sectionTitle}>Explore Rooms</Text>
        <View style={styles.grid}>
          {ROOMS.map((room) => (
            <TouchableOpacity
              key={room.id}
              style={[styles.roomCard, { backgroundColor: room.color }]}
              onPress={() => router.push(room.route as any)}
              activeOpacity={0.7}
            >
              <Text style={styles.roomEmoji}>{room.emoji}</Text>
              <Text style={styles.roomName}>{room.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF5F8' },
  sceneContainer: { width: '100%', position: 'relative' },
  dayBar: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: 16, paddingVertical: 8, backgroundColor: '#fff',
    borderBottomWidth: 1, borderBottomColor: '#f0e0e8',
  },
  dayText: { fontSize: 13, fontWeight: '600', color: '#666' },
  starsBadge: { backgroundColor: '#FFF5CC', paddingHorizontal: 10, paddingVertical: 3, borderRadius: 12 },
  starsText: { fontSize: 12, fontWeight: '700', color: '#f5a623' },
  scrollArea: { flex: 1 },
  gridContainer: { padding: 12, paddingBottom: 24 },
  sectionTitle: { fontSize: 18, fontWeight: '800', color: '#333', marginBottom: 10, marginLeft: 4 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  roomCard: {
    width: (SCREEN_WIDTH - 44) / 3, aspectRatio: 1, borderRadius: 16,
    alignItems: 'center', justifyContent: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.08, shadowRadius: 4, elevation: 2,
  },
  roomEmoji: { fontSize: 28, marginBottom: 4 },
  roomName: { fontSize: 12, fontWeight: '700', color: '#333' },
});
