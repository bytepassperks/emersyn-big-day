import React, { useState, useEffect, useRef, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Dimensions, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getRandomEncouragement } from '@/lib/helpers';

const { width: SCREEN_WIDTH } = Dimensions.get('window');
const LANE_WIDTH = SCREEN_WIDTH / 3;
const GAME_HEIGHT = 500;
const PLAYER_SIZE = 50;
const OBSTACLE_SIZE = 40;
const POWERUP_SIZE = 35;
const TICK_MS = 50;

type Lane = 0 | 1 | 2;
type GamePhase = 'menu' | 'playing' | 'gameover';

interface Obstacle {
  id: number;
  lane: Lane;
  y: number;
  type: 'cone' | 'rock' | 'puddle';
  emoji: string;
}

interface Powerup {
  id: number;
  lane: Lane;
  y: number;
  type: 'coin' | 'star' | 'shield';
  emoji: string;
}

const OBSTACLE_EMOJIS: Record<string, string> = { cone: '🔶', rock: '🪨', puddle: '💧' };
const POWERUP_EMOJIS: Record<string, string> = { coin: '💰', star: '⭐', shield: '🛡️' };
const TRACKS = [
  { id: 'park', name: 'Park Path', emoji: '🌳', color: Colors.bgPark },
  { id: 'city', name: 'City Road', emoji: '🏙️', color: Colors.skyLight },
  { id: 'beach', name: 'Beach Run', emoji: '🏖️', color: Colors.yellowLight },
];

export default function EndlessRunner() {
  const router = useRouter();
  const { addCoins, addXP, addStars, earnSticker, recordMiniGameResult, saveGame } = useGameStore();

  const [phase, setPhase] = useState<GamePhase>('menu');
  const [selectedTrack, setSelectedTrack] = useState(0);
  const [lane, setLane] = useState<Lane>(1);
  const [score, setScore] = useState(0);
  const [coinsCollected, setCoinsCollected] = useState(0);
  const [obstacles, setObstacles] = useState<Obstacle[]>([]);
  const [powerups, setPowerups] = useState<Powerup[]>([]);
  const [speed, setSpeed] = useState(5);
  const [hasShield, setHasShield] = useState(false);

  const gameLoop = useRef<ReturnType<typeof setInterval> | null>(null);
  const obstacleId = useRef(0);
  const frameCount = useRef(0);

  const startGame = () => {
    setPhase('playing');
    setLane(1);
    setScore(0);
    setCoinsCollected(0);
    setObstacles([]);
    setPowerups([]);
    setSpeed(5);
    setHasShield(false);
    frameCount.current = 0;
    obstacleId.current = 0;
  };

  const endGame = useCallback(async () => {
    if (gameLoop.current) {
      clearInterval(gameLoop.current);
      gameLoop.current = null;
    }
    setPhase('gameover');

    const coinReward = coinsCollected + Math.floor(score / 10);
    const xpReward = Math.floor(score / 5);
    addCoins(coinReward);
    addXP(xpReward);
    addStars(Math.floor(score / 50));
    earnSticker('sticker_first_game');
    if (score >= 100) earnSticker('sticker_runner_1000');
    if (score >= 200) earnSticker('sticker_high_score');
    recordMiniGameResult({ gameId: 'scooty_dash', score, coinsEarned: coinReward, playedAt: Date.now() });
    await saveGame();
  }, [coinsCollected, score]);

  useEffect(() => {
    if (phase !== 'playing') return;

    gameLoop.current = setInterval(() => {
      frameCount.current++;

      // Move obstacles down
      setObstacles((prev) => {
        const moved = prev.map((o) => ({ ...o, y: o.y + speed })).filter((o) => o.y < GAME_HEIGHT + 50);
        return moved;
      });

      // Move powerups down
      setPowerups((prev) => {
        const moved = prev.map((p) => ({ ...p, y: p.y + speed })).filter((p) => p.y < GAME_HEIGHT + 50);
        return moved;
      });

      // Spawn obstacles
      if (frameCount.current % 20 === 0) {
        const types: Array<'cone' | 'rock' | 'puddle'> = ['cone', 'rock', 'puddle'];
        const type = types[Math.floor(Math.random() * types.length)];
        const newLane = Math.floor(Math.random() * 3) as Lane;
        setObstacles((prev) => [
          ...prev,
          { id: obstacleId.current++, lane: newLane, y: -OBSTACLE_SIZE, type, emoji: OBSTACLE_EMOJIS[type] },
        ]);
      }

      // Spawn powerups
      if (frameCount.current % 30 === 0) {
        const types: Array<'coin' | 'star' | 'shield'> = ['coin', 'coin', 'coin', 'star', 'shield'];
        const type = types[Math.floor(Math.random() * types.length)];
        const newLane = Math.floor(Math.random() * 3) as Lane;
        setPowerups((prev) => [
          ...prev,
          { id: obstacleId.current++, lane: newLane, y: -POWERUP_SIZE, type, emoji: POWERUP_EMOJIS[type] },
        ]);
      }

      // Increase score
      setScore((prev) => prev + 1);

      // Speed up gradually
      if (frameCount.current % 200 === 0) {
        setSpeed((prev) => Math.min(prev + 0.5, 12));
      }
    }, TICK_MS);

    return () => {
      if (gameLoop.current) clearInterval(gameLoop.current);
    };
  }, [phase, speed]);

  // Collision detection
  useEffect(() => {
    if (phase !== 'playing') return;

    const playerY = GAME_HEIGHT - 80;

    // Check obstacle collisions
    for (const obs of obstacles) {
      if (obs.lane === lane && Math.abs(obs.y - playerY) < PLAYER_SIZE) {
        if (hasShield) {
          setHasShield(false);
          setObstacles((prev) => prev.filter((o) => o.id !== obs.id));
        } else {
          endGame();
          return;
        }
      }
    }

    // Check powerup collisions
    for (const pu of powerups) {
      if (pu.lane === lane && Math.abs(pu.y - playerY) < PLAYER_SIZE) {
        setPowerups((prev) => prev.filter((p) => p.id !== pu.id));
        if (pu.type === 'coin') setCoinsCollected((prev) => prev + 3);
        if (pu.type === 'star') setScore((prev) => prev + 10);
        if (pu.type === 'shield') setHasShield(true);
      }
    }
  }, [obstacles, powerups, lane, phase, hasShield, endGame]);

  const moveLane = (direction: 'left' | 'right') => {
    if (direction === 'left' && lane > 0) setLane((prev) => (prev - 1) as Lane);
    if (direction === 'right' && lane < 2) setLane((prev) => (prev + 1) as Lane);
  };

  if (phase === 'menu') {
    return (
      <ScreenWrapper title="Scooty Dash" emoji="🛴" bgColor={Colors.mintLight}>
        <View style={styles.menuArea}>
          <Text style={styles.menuEmoji}>🛴</Text>
          <Text style={styles.menuTitle}>Scooty Dash!</Text>
          <Text style={styles.menuDesc}>Dodge obstacles and collect coins!</Text>

          <Text style={styles.trackTitle}>Choose Track:</Text>
          {TRACKS.map((track, i) => (
            <GameButton
              key={track.id}
              title={`${track.emoji} ${track.name}`}
              onPress={() => setSelectedTrack(i)}
              variant={selectedTrack === i ? 'primary' : 'outline'}
              size="medium"
              style={styles.trackBtn}
            />
          ))}

          <GameButton
            title="Start! 🏁"
            onPress={startGame}
            variant="accent"
            size="large"
            style={{ marginTop: 20 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'gameover') {
    const totalCoins = coinsCollected + Math.floor(score / 10);
    return (
      <ScreenWrapper title="Game Over" emoji="🏁" bgColor={Colors.mintLight} showBack={false} scrollable={false}>
        <View style={styles.gameOverArea}>
          <Text style={styles.gameOverEmoji}>🏁</Text>
          <Text style={styles.gameOverTitle}>Game Over!</Text>
          <Text style={styles.gameOverScore}>Score: {score}</Text>
          <Text style={styles.gameOverEncouragement}>{getRandomEncouragement()}</Text>

          <View style={styles.rewardCard}>
            <Text style={styles.rewardText}>💰 +₹{totalCoins}</Text>
            <Text style={styles.rewardText}>⭐ +{Math.floor(score / 5)} XP</Text>
            <Text style={styles.rewardText}>🌟 +{Math.floor(score / 50)} Stars</Text>
          </View>

          <GameButton title="Play Again" emoji="🔄" onPress={startGame} variant="primary" size="large" style={{ marginTop: 16 }} />
          <GameButton title="Back" emoji="🏠" onPress={() => router.back()} variant="outline" size="medium" style={{ marginTop: 8 }} />
        </View>
      </ScreenWrapper>
    );
  }

  const track = TRACKS[selectedTrack];

  return (
    <View style={[styles.gameScreen, { backgroundColor: track.color }]}>
      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudText}>🏁 {score}</Text>
        <Text style={styles.hudText}>💰 {coinsCollected}</Text>
        {hasShield && <Text style={styles.hudText}>🛡️</Text>}
      </View>

      {/* Game Area */}
      <View style={styles.gameArea}>
        {/* Lane lines */}
        <View style={[styles.laneLine, { left: LANE_WIDTH }]} />
        <View style={[styles.laneLine, { left: LANE_WIDTH * 2 }]} />

        {/* Player */}
        <View
          style={[
            styles.player,
            {
              left: lane * LANE_WIDTH + (LANE_WIDTH - PLAYER_SIZE) / 2,
              bottom: 60,
            },
          ]}
        >
          <Text style={styles.playerEmoji}>🛴</Text>
          {hasShield && <Text style={styles.shieldEmoji}>🛡️</Text>}
        </View>

        {/* Obstacles */}
        {obstacles.map((obs) => (
          <View
            key={obs.id}
            style={[
              styles.obstacle,
              {
                left: obs.lane * LANE_WIDTH + (LANE_WIDTH - OBSTACLE_SIZE) / 2,
                top: obs.y,
              },
            ]}
          >
            <Text style={styles.obstacleEmoji}>{obs.emoji}</Text>
          </View>
        ))}

        {/* Powerups */}
        {powerups.map((pu) => (
          <View
            key={pu.id}
            style={[
              styles.powerup,
              {
                left: pu.lane * LANE_WIDTH + (LANE_WIDTH - POWERUP_SIZE) / 2,
                top: pu.y,
              },
            ]}
          >
            <Text style={styles.powerupEmoji}>{pu.emoji}</Text>
          </View>
        ))}
      </View>

      {/* Controls */}
      <View style={styles.controls}>
        <TouchableOpacity style={styles.controlBtn} onPress={() => moveLane('left')}>
          <Text style={styles.controlText}>⬅️</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.controlBtn} onPress={() => moveLane('right')}>
          <Text style={styles.controlText}>➡️</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  menuArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  menuEmoji: { fontSize: 64 },
  menuTitle: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  menuDesc: { fontSize: 16, color: Colors.gray500, marginTop: 4 },
  trackTitle: { fontSize: 18, fontWeight: '800', color: Colors.dark, marginTop: 24, marginBottom: 8 },
  trackBtn: { width: '100%', marginVertical: 4 },
  gameScreen: { flex: 1, paddingTop: 50 },
  hud: {
    flexDirection: 'row', justifyContent: 'space-around', paddingHorizontal: 20, paddingVertical: 8,
    backgroundColor: Colors.overlay, borderRadius: 16, marginHorizontal: 16,
  },
  hudText: { fontSize: 18, fontWeight: '800', color: Colors.white },
  gameArea: {
    flex: 1, marginHorizontal: 16, marginTop: 8, position: 'relative', overflow: 'hidden',
    borderRadius: 20, backgroundColor: 'rgba(255,255,255,0.3)',
  },
  laneLine: {
    position: 'absolute', top: 0, bottom: 0, width: 2,
    backgroundColor: 'rgba(255,255,255,0.4)',
  },
  player: {
    position: 'absolute', width: PLAYER_SIZE, height: PLAYER_SIZE,
    justifyContent: 'center', alignItems: 'center',
  },
  playerEmoji: { fontSize: 36 },
  shieldEmoji: { fontSize: 16, position: 'absolute', top: -8, right: -8 },
  obstacle: {
    position: 'absolute', width: OBSTACLE_SIZE, height: OBSTACLE_SIZE,
    justifyContent: 'center', alignItems: 'center',
  },
  obstacleEmoji: { fontSize: 30 },
  powerup: {
    position: 'absolute', width: POWERUP_SIZE, height: POWERUP_SIZE,
    justifyContent: 'center', alignItems: 'center',
  },
  powerupEmoji: { fontSize: 26 },
  controls: {
    flexDirection: 'row', justifyContent: 'space-around', paddingVertical: 16, paddingHorizontal: 40,
  },
  controlBtn: {
    width: 80, height: 80, borderRadius: 40, backgroundColor: Colors.white,
    justifyContent: 'center', alignItems: 'center',
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.2, shadowRadius: 4, elevation: 4,
  },
  controlText: { fontSize: 32 },
  // Game over
  gameOverArea: { flex: 1, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 20 },
  gameOverEmoji: { fontSize: 64 },
  gameOverTitle: { fontSize: 32, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  gameOverScore: { fontSize: 24, fontWeight: '800', color: Colors.purple, marginTop: 8 },
  gameOverEncouragement: { fontSize: 16, color: Colors.gray500, marginTop: 8 },
  rewardCard: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 16,
    width: '100%', borderWidth: 2, borderColor: Colors.mintLight, gap: 8,
  },
  rewardText: { fontSize: 18, fontWeight: '700', color: Colors.dark },
});
