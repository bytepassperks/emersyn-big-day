/**
 * Scooty Dash - 3-lane endless runner with obstacles, coins, powerups
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, Animated, PanResponder } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

const { width: SW, height: SH } = Dimensions.get('window');
const LANE_WIDTH = SW / 3;
const LANES = [-1, 0, 1]; // left, center, right
const GAME_SPEED_BASE = 4;
const SPAWN_INTERVAL = 1200;

type GameObj = { id: number; type: 'obstacle' | 'coin' | 'powerup'; lane: number; y: number; emoji: string };

const OBSTACLES = ['\uD83D\uDEA7', '\uD83E\uDEA8', '\uD83C\uDF32', '\uD83D\uDEB8', '\uD83D\uDCE6'];
const POWERUPS = ['\u2B50', '\uD83D\uDEE1\uFE0F', '\uD83C\uDF1F'];
const TRACKS = [
  { name: 'City Road', bg: '#E8E8E8', obstacleSet: ['\uD83D\uDEA7', '\uD83D\uDE97', '\uD83D\uDEB8'] },
  { name: 'Park Trail', bg: '#D4E8D4', obstacleSet: ['\uD83C\uDF32', '\uD83E\uDEA8', '\uD83D\uDC15'] },
  { name: 'Beach Path', bg: '#E8DCC8', obstacleSet: ['\uD83C\uDF34', '\uD83E\uDEBB', '\uD83E\uDD80'] },
];

export default function EndlessRunner() {
  const { addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [gameState, setGameState] = useState<'ready' | 'playing' | 'over'>('ready');
  const [score, setScore] = useState(0);
  const [coinsCollected, setCoinsCollected] = useState(0);
  const [playerLane, setPlayerLane] = useState(0);
  const [objects, setObjects] = useState<GameObj[]>([]);
  const [speed, setSpeed] = useState(GAME_SPEED_BASE);
  const [track] = useState(() => TRACKS[Math.floor(Math.random() * TRACKS.length)]);
  const [shield, setShield] = useState(false);
  const [combo, setCombo] = useState(0);
  const idCounter = useRef(0);
  const gameLoop = useRef<ReturnType<typeof setInterval> | null>(null);
  const spawnLoop = useRef<ReturnType<typeof setInterval> | null>(null);
  const playerAnim = useRef(new Animated.Value(0)).current;

  const movePlayer = useCallback((dir: number) => {
    setPlayerLane(prev => {
      const next = prev + dir;
      if (next < -1 || next > 1) return prev;
      Animated.spring(playerAnim, { toValue: next * LANE_WIDTH, useNativeDriver: true }).start();
      return next;
    });
  }, [playerAnim]);

  const panResponder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onPanResponderRelease: (_, gs) => {
        if (Math.abs(gs.dx) > 30) {
          movePlayer(gs.dx > 0 ? 1 : -1);
        }
      },
    })
  ).current;

  const startGame = () => {
    setGameState('playing');
    setScore(0);
    setCoinsCollected(0);
    setObjects([]);
    setSpeed(GAME_SPEED_BASE);
    setPlayerLane(0);
    setShield(false);
    setCombo(0);
    playerAnim.setValue(0);
  };

  const endGame = useCallback(async () => {
    setGameState('over');
    if (gameLoop.current) clearInterval(gameLoop.current);
    if (spawnLoop.current) clearInterval(spawnLoop.current);
    addCoins(coinsCollected);
    addXP(Math.floor(score / 10));
    if (score > 100) addStars(1);
    if (score > 500) earnSticker('sticker_scooty_master');
    await saveGame();
  }, [coinsCollected, score, addCoins, addXP, addStars, earnSticker, saveGame]);

  useEffect(() => {
    if (gameState !== 'playing') return;

    spawnLoop.current = setInterval(() => {
      const rand = Math.random();
      let type: GameObj['type'] = 'obstacle';
      let emoji = track.obstacleSet[Math.floor(Math.random() * track.obstacleSet.length)];

      if (rand > 0.7) {
        type = 'coin';
        emoji = '\uD83D\uDCB0';
      } else if (rand > 0.92) {
        type = 'powerup';
        emoji = POWERUPS[Math.floor(Math.random() * POWERUPS.length)];
      }

      const lane = LANES[Math.floor(Math.random() * LANES.length)];
      idCounter.current += 1;
      setObjects(prev => [...prev, { id: idCounter.current, type, lane, y: -60, emoji }]);
    }, SPAWN_INTERVAL);

    gameLoop.current = setInterval(() => {
      setScore(prev => prev + 1);
      setSpeed(prev => Math.min(prev + 0.002, 12));

      setObjects(prev => {
        const updated = prev.map(obj => ({ ...obj, y: obj.y + speed })).filter(obj => obj.y < SH + 50);
        return updated;
      });
    }, 16);

    return () => {
      if (gameLoop.current) clearInterval(gameLoop.current);
      if (spawnLoop.current) clearInterval(spawnLoop.current);
    };
  }, [gameState, speed, track]);

  // Collision detection
  useEffect(() => {
    if (gameState !== 'playing') return;

    const playerY = SH - 160;
    const hitObjects = objects.filter(obj =>
      obj.lane === playerLane && Math.abs(obj.y - playerY) < 40
    );

    hitObjects.forEach(obj => {
      if (obj.type === 'coin') {
        setCoinsCollected(prev => prev + (combo > 5 ? 2 : 1));
        setCombo(prev => prev + 1);
        setObjects(prev => prev.filter(o => o.id !== obj.id));
      } else if (obj.type === 'powerup') {
        setShield(true);
        setTimeout(() => setShield(false), 5000);
        setObjects(prev => prev.filter(o => o.id !== obj.id));
      } else if (obj.type === 'obstacle') {
        if (shield) {
          setShield(false);
          setObjects(prev => prev.filter(o => o.id !== obj.id));
        } else {
          endGame();
        }
      }
    });
  }, [objects, playerLane, gameState, shield, combo, endGame]);

  if (gameState === 'ready') {
    return (
      <View style={[styles.container, { backgroundColor: track.bg }]}>
        <View style={styles.center}>
          <Text style={styles.scootyEmoji}>{'\uD83D\uDEF5'}</Text>
          <Text style={styles.gameTitle}>Scooty Dash</Text>
          <Text style={styles.trackName}>{track.name}</Text>
          <Text style={styles.hint}>Swipe left/right to dodge obstacles!</Text>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Play!</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (gameState === 'over') {
    return (
      <View style={[styles.container, { backgroundColor: track.bg }]}>
        <View style={styles.center}>
          <Text style={styles.overTitle}>Game Over!</Text>
          <Text style={styles.finalScore}>Score: {score}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>{'\u20B9'}{coinsCollected} coins earned</Text>
            <Text style={styles.rewardLine}>+{Math.floor(score / 10)} XP</Text>
            {score > 100 && <Text style={styles.rewardLine}>+1 Star!</Text>}
          </View>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Play Again</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: track.bg }]} {...panResponder.panHandlers}>
      {/* Lane lines */}
      <View style={styles.laneLines}>
        <View style={styles.laneLine} />
        <View style={styles.laneLine} />
      </View>

      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudScore}>Score: {score}</Text>
        <Text style={styles.hudCoins}>{'\u20B9'}{coinsCollected}</Text>
        {shield && <Text style={styles.hudShield}>{'\uD83D\uDEE1\uFE0F'}</Text>}
        {combo > 3 && <Text style={styles.hudCombo}>x{combo}</Text>}
      </View>

      {/* Objects */}
      {objects.map(obj => (
        <Text
          key={obj.id}
          style={[
            styles.gameObj,
            { top: obj.y, left: SW / 2 + obj.lane * LANE_WIDTH - 20 },
          ]}
        >
          {obj.emoji}
        </Text>
      ))}

      {/* Player */}
      <Animated.View style={[styles.player, { transform: [{ translateX: playerAnim }] }]}>
        <Text style={styles.playerEmoji}>{'\uD83D\uDEF5'}</Text>
        {shield && <View style={styles.shieldGlow} />}
      </Animated.View>

      {/* Touch zones */}
      <View style={styles.touchZones}>
        <TouchableOpacity style={styles.touchZone} onPress={() => movePlayer(-1)} />
        <TouchableOpacity style={styles.touchZone} onPress={() => {}} />
        <TouchableOpacity style={styles.touchZone} onPress={() => movePlayer(1)} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  scootyEmoji: { fontSize: 80 },
  gameTitle: { fontSize: 32, fontWeight: '800', color: '#333', marginTop: 12 },
  trackName: { fontSize: 16, fontWeight: '700', color: '#666', marginTop: 4 },
  hint: { fontSize: 14, color: '#999', marginTop: 12 },
  playBtn: { marginTop: 30, backgroundColor: '#FF6B6B', paddingHorizontal: 40, paddingVertical: 16, borderRadius: 20 },
  playBtnText: { fontSize: 22, fontWeight: '800', color: '#fff' },
  backLink: { marginTop: 16 },
  backLinkText: { fontSize: 14, fontWeight: '700', color: '#999' },
  laneLines: { position: 'absolute', top: 0, bottom: 0, left: 0, right: 0, flexDirection: 'row', justifyContent: 'space-evenly' },
  laneLine: { width: 2, backgroundColor: 'rgba(0,0,0,0.1)' },
  hud: { position: 'absolute', top: 50, left: 0, right: 0, flexDirection: 'row', justifyContent: 'space-between', paddingHorizontal: 20, zIndex: 10 },
  hudScore: { fontSize: 18, fontWeight: '800', color: '#333' },
  hudCoins: { fontSize: 18, fontWeight: '800', color: '#ff9f43' },
  hudShield: { fontSize: 24 },
  hudCombo: { fontSize: 18, fontWeight: '800', color: '#A855F7' },
  gameObj: { position: 'absolute', fontSize: 36, width: 40, textAlign: 'center' },
  player: { position: 'absolute', bottom: 120, left: SW / 2 - 25, width: 50, alignItems: 'center' },
  playerEmoji: { fontSize: 48 },
  shieldGlow: { position: 'absolute', width: 60, height: 60, borderRadius: 30, backgroundColor: 'rgba(168,85,247,0.3)', top: -5 },
  touchZones: { position: 'absolute', bottom: 0, left: 0, right: 0, height: SH * 0.6, flexDirection: 'row' },
  touchZone: { flex: 1 },
  overTitle: { fontSize: 36, fontWeight: '800', color: '#FF6B6B' },
  finalScore: { fontSize: 24, fontWeight: '800', color: '#333', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 16, width: '100%', gap: 8 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#333' },
});
