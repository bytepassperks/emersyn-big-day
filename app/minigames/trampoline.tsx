import React, { useState, useEffect, useRef } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Dimensions, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getRandomEncouragement } from '@/lib/helpers';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

type TrampolinePhase = 'menu' | 'playing' | 'results';

interface Collectible {
  id: number;
  x: number;
  y: number;
  emoji: string;
  points: number;
  collected: boolean;
}

const COLLECTIBLE_TYPES = [
  { emoji: '⭐', points: 5 },
  { emoji: '💎', points: 10 },
  { emoji: '🌈', points: 15 },
  { emoji: '💰', points: 3 },
  { emoji: '🎵', points: 5 },
  { emoji: '🦋', points: 8 },
];

export default function Trampoline() {
  const router = useRouter();
  const { addCoins, addXP, addStars, earnSticker, recordMiniGameResult, saveGame } = useGameStore();

  const [phase, setPhase] = useState<TrampolinePhase>('menu');
  const [bounceCount, setBounceCount] = useState(0);
  const [combo, setCombo] = useState(0);
  const [maxCombo, setMaxCombo] = useState(0);
  const [score, setScore] = useState(0);
  const [timeLeft, setTimeLeft] = useState(30);
  const [collectibles, setCollectibles] = useState<Collectible[]>([]);
  const [playerY, setPlayerY] = useState(0);
  const [bouncing, setBouncing] = useState(false);
  const nextId = useRef(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startGame = () => {
    setPhase('playing');
    setBounceCount(0);
    setCombo(0);
    setMaxCombo(0);
    setScore(0);
    setTimeLeft(30);
    setCollectibles([]);
    setBouncing(false);
    nextId.current = 0;

    // Spawn initial collectibles
    spawnCollectibles();
  };

  const spawnCollectibles = () => {
    const newCollectibles: Collectible[] = Array.from({ length: 6 }, () => {
      const type = COLLECTIBLE_TYPES[Math.floor(Math.random() * COLLECTIBLE_TYPES.length)];
      return {
        id: nextId.current++,
        x: Math.random() * (SCREEN_WIDTH - 80) + 20,
        y: Math.random() * 200 + 50,
        emoji: type.emoji,
        points: type.points,
        collected: false,
      };
    });
    setCollectibles((prev) => [...prev.filter((c) => !c.collected), ...newCollectibles]);
  };

  // Game timer
  useEffect(() => {
    if (phase !== 'playing') return;

    timerRef.current = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current);
          endGame();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [phase]);

  // Respawn collectibles
  useEffect(() => {
    if (phase !== 'playing') return;
    const uncollected = collectibles.filter((c) => !c.collected);
    if (uncollected.length < 3) {
      spawnCollectibles();
    }
  }, [collectibles, phase]);

  const handleBounce = () => {
    if (phase !== 'playing') return;

    setBouncing(true);
    setBounceCount((prev) => prev + 1);

    const newCombo = combo + 1;
    setCombo(newCombo);
    setMaxCombo((prev) => Math.max(prev, newCombo));

    // Collect nearby items
    setCollectibles((prev) => {
      let collected = false;
      const updated = prev.map((c) => {
        if (!c.collected && c.y < 250) {
          collected = true;
          setScore((s) => s + c.points * (1 + Math.floor(newCombo / 5)));
          return { ...c, collected: true };
        }
        return c;
      });
      return updated;
    });

    setScore((prev) => prev + 1 + Math.floor(newCombo / 3));

    // Bounce animation
    setPlayerY(120);
    setTimeout(() => setPlayerY(60), 150);
    setTimeout(() => setPlayerY(0), 300);
    setTimeout(() => setBouncing(false), 300);
  };

  const endGame = async () => {
    setPhase('results');
    if (timerRef.current) clearInterval(timerRef.current);

    const coinReward = Math.floor(score / 2) + 5;
    const xpReward = Math.floor(score / 3);

    addCoins(coinReward);
    addXP(xpReward);
    addStars(Math.floor(score / 20));
    earnSticker('sticker_trampoline');
    if (maxCombo >= 10) earnSticker('sticker_high_score');
    recordMiniGameResult({ gameId: 'trampoline', score, coinsEarned: coinReward, playedAt: Date.now() });
    await saveGame();
  };

  if (phase === 'menu') {
    return (
      <ScreenWrapper title="Trampoline" emoji="🤸" bgColor={Colors.skyLight}>
        <View style={styles.menuArea}>
          <Text style={styles.menuEmoji}>🤸</Text>
          <Text style={styles.menuTitle}>Trampoline Combo!</Text>
          <Text style={styles.menuDesc}>
            Tap to bounce! Collect stars and build combos!{'\n'}
            30 seconds to get the highest score!
          </Text>
          <GameButton title="Bounce! 🤸" onPress={startGame} variant="primary" size="large" style={{ marginTop: 20 }} />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'results') {
    return (
      <ScreenWrapper title="Results" emoji="🏆" bgColor={Colors.skyLight} showBack={false}>
        <View style={styles.resultsArea}>
          <Text style={styles.resultsEmoji}>🏆</Text>
          <Text style={styles.resultsTitle}>{getRandomEncouragement()}</Text>
          <Text style={styles.resultsScore}>Score: {score}</Text>

          <View style={styles.statsRow}>
            <View style={styles.statItem}><Text style={styles.statValue}>{bounceCount}</Text><Text style={styles.statLabel}>Bounces</Text></View>
            <View style={styles.statItem}><Text style={styles.statValue}>{maxCombo}</Text><Text style={styles.statLabel}>Max Combo</Text></View>
          </View>

          <View style={styles.rewardCard}>
            <Text style={styles.rewardText}>💰 +₹{Math.floor(score / 2) + 5}</Text>
            <Text style={styles.rewardText}>⭐ +{Math.floor(score / 3)} XP</Text>
          </View>

          <GameButton title="Bounce Again" emoji="🤸" onPress={startGame} variant="primary" size="large" style={{ marginTop: 16 }} />
          <GameButton title="Back" emoji="🏠" onPress={() => router.back()} variant="outline" size="medium" style={{ marginTop: 8 }} />
        </View>
      </ScreenWrapper>
    );
  }

  return (
    <View style={styles.gameScreen}>
      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudText}>⏱️ {timeLeft}s</Text>
        <Text style={styles.hudText}>🏁 {score}</Text>
        <Text style={styles.hudText}>🔥 x{combo}</Text>
      </View>

      {/* Game Area */}
      <View style={styles.gameArea}>
        {/* Collectibles */}
        {collectibles.filter((c) => !c.collected).map((c) => (
          <View key={c.id} style={[styles.collectible, { left: c.x, top: c.y }]}>
            <Text style={styles.collectibleEmoji}>{c.emoji}</Text>
          </View>
        ))}

        {/* Player */}
        <View style={[styles.player, { bottom: 100 + playerY }]}>
          <Text style={styles.playerEmoji}>{bouncing ? '🤸' : '🧍'}</Text>
        </View>

        {/* Trampoline */}
        <View style={styles.trampoline}>
          <Text style={styles.trampolineEmoji}>🔵🔵🔵🔵🔵</Text>
        </View>
      </View>

      {/* Bounce Button */}
      <TouchableOpacity style={styles.bounceBtn} onPress={handleBounce} activeOpacity={0.6}>
        <Text style={styles.bounceBtnText}>BOUNCE! 🤸</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  menuArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  menuEmoji: { fontSize: 64 },
  menuTitle: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  menuDesc: { fontSize: 14, color: Colors.gray500, textAlign: 'center', marginTop: 8 },
  gameScreen: { flex: 1, backgroundColor: Colors.skyLight, paddingTop: 50 },
  hud: {
    flexDirection: 'row', justifyContent: 'space-around', paddingVertical: 8,
    backgroundColor: Colors.overlay, borderRadius: 16, marginHorizontal: 16,
  },
  hudText: { fontSize: 18, fontWeight: '800', color: Colors.white },
  gameArea: { flex: 1, position: 'relative', marginHorizontal: 16 },
  collectible: { position: 'absolute' },
  collectibleEmoji: { fontSize: 28 },
  player: { position: 'absolute', alignSelf: 'center', left: '45%' },
  playerEmoji: { fontSize: 48 },
  trampoline: { position: 'absolute', bottom: 60, alignSelf: 'center', left: '20%' },
  trampolineEmoji: { fontSize: 20 },
  bounceBtn: {
    marginHorizontal: 40, marginBottom: 40, paddingVertical: 20, borderRadius: 30,
    backgroundColor: Colors.pink, alignItems: 'center',
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.3, shadowRadius: 6, elevation: 6,
  },
  bounceBtnText: { fontSize: 24, fontWeight: '800', color: Colors.white },
  resultsArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 20 },
  resultsEmoji: { fontSize: 64 },
  resultsTitle: { fontSize: 22, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  resultsScore: { fontSize: 32, fontWeight: '800', color: Colors.sky, marginTop: 8 },
  statsRow: { flexDirection: 'row', gap: 16, marginTop: 16 },
  statItem: { backgroundColor: Colors.white, padding: 16, borderRadius: 14, alignItems: 'center', width: '40%' },
  statValue: { fontSize: 28, fontWeight: '800', color: Colors.dark },
  statLabel: { fontSize: 12, color: Colors.gray500, marginTop: 4 },
  rewardCard: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 16,
    width: '100%', borderWidth: 2, borderColor: Colors.skyLight, gap: 8,
  },
  rewardText: { fontSize: 18, fontWeight: '700', color: Colors.dark },
});
