/**
 * Arcade - Mini-game hub with game selection
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, ScrollView } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';

const { width: SW } = Dimensions.get('window');

const GAMES = [
  { id: 'endless-runner', name: 'Scooty Dash', emoji: '\uD83D\uDEF5', desc: 'Dodge obstacles on your scooty!', color: '#FF6B6B', route: '/minigames/endless-runner' },
  { id: 'dance-party', name: 'Dance Party', emoji: '\uD83D\uDC83', desc: 'Hit the rhythm and dance!', color: '#A855F7', route: '/minigames/dance-party' },
  { id: 'karate-star', name: 'Karate Star', emoji: '\uD83E\uDD4B', desc: 'Earn your karate belts!', color: '#F59E0B', route: '/minigames/karate-star' },
  { id: 'trampoline', name: 'Trampoline Jump', emoji: '\uD83E\uDD38', desc: 'Bounce and do combos!', color: '#10B981', route: '/minigames/trampoline' },
  { id: 'homework', name: 'Brain Puzzles', emoji: '\uD83E\uDDE9', desc: 'Solve fun puzzles!', color: '#3B82F6', route: '/minigames/homework' },
];

export default function Arcade() {
  const { coins, stats, xp, level } = useGameStore();
  const [showScene, setShowScene] = useState(true);
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);

  const xpToNext = level * 100;

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    const game = GAMES.find(g => g.id === interactable.id);
    if (game) {
      router.push(game.route as any);
    }
  }, []);

  if (!showScene) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
            <Text style={styles.backBtnText}>{'\u2190'} Home</Text>
          </TouchableOpacity>
          <Text style={styles.title}>Arcade</Text>
          <TouchableOpacity onPress={() => setShowScene(true)} style={styles.sceneBtn}>
            <Text style={styles.sceneBtnText}>3D View</Text>
          </TouchableOpacity>
        </View>
        <ScrollView style={styles.gameList} contentContainerStyle={{ paddingBottom: 30 }}>
          {GAMES.map(game => (
            <TouchableOpacity
              key={game.id}
              style={[styles.gameCard, { borderLeftColor: game.color }]}
              onPress={() => router.push(game.route as any)}
            >
              <Text style={styles.gameEmoji}>{game.emoji}</Text>
              <View style={{ flex: 1 }}>
                <Text style={styles.gameName}>{game.name}</Text>
                <Text style={styles.gameDesc}>{game.desc}</Text>
              </View>
              <Text style={styles.playBtn}>Play</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="arcade"
          onInteract={handleInteract}
          height={SW * 0.7}
        />
        <GameHUD
          coins={coins}
          stats={stats}
          level={level}
          xp={xp % xpToNext}
          xpToNext={xpToNext}
          roomName="Arcade"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
        />
      </View>
      <TouchableOpacity style={styles.listToggle} onPress={() => setShowScene(false)}>
        <Text style={styles.listToggleText}>View Game List</Text>
      </TouchableOpacity>
      <ScrollView horizontal style={styles.quickGames} showsHorizontalScrollIndicator={false}>
        {GAMES.map(game => (
          <TouchableOpacity
            key={game.id}
            style={[styles.quickCard, { backgroundColor: game.color }]}
            onPress={() => router.push(game.route as any)}
          >
            <Text style={styles.quickEmoji}>{game.emoji}</Text>
            <Text style={styles.quickName}>{game.name}</Text>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F0E4FF' },
  sceneContainer: { flex: 1, position: 'relative' },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: 16, paddingTop: 50 },
  backBtn: { padding: 8, backgroundColor: '#fff', borderRadius: 12 },
  backBtnText: { fontSize: 14, fontWeight: '700', color: '#333' },
  title: { fontSize: 22, fontWeight: '800', color: '#333' },
  sceneBtn: { padding: 8, backgroundColor: '#A855F7', borderRadius: 12 },
  sceneBtnText: { fontSize: 14, fontWeight: '700', color: '#fff' },
  gameList: { flex: 1, paddingHorizontal: 16 },
  gameCard: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
    padding: 16, borderRadius: 16, marginBottom: 10, gap: 12,
    borderLeftWidth: 4,
  },
  gameEmoji: { fontSize: 36 },
  gameName: { fontSize: 18, fontWeight: '800', color: '#333' },
  gameDesc: { fontSize: 13, color: '#999', marginTop: 2 },
  playBtn: { fontSize: 16, fontWeight: '800', color: '#A855F7', backgroundColor: '#F0E4FF', paddingHorizontal: 16, paddingVertical: 8, borderRadius: 12 },
  listToggle: { alignItems: 'center', paddingVertical: 8 },
  listToggleText: { fontSize: 13, fontWeight: '700', color: '#A855F7' },
  quickGames: { paddingHorizontal: 12, paddingBottom: 12 },
  quickCard: { width: 100, height: 90, borderRadius: 16, alignItems: 'center', justifyContent: 'center', marginRight: 10 },
  quickEmoji: { fontSize: 28 },
  quickName: { fontSize: 11, fontWeight: '800', color: '#fff', marginTop: 4 },
});
