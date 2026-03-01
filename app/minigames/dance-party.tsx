/**
 * Dance Party - Rhythm game with falling arrows and combo system
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, Animated } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

const { width: SW, height: SH } = Dimensions.get('window');

type Arrow = { id: number; dir: 'left' | 'up' | 'down' | 'right'; y: number; hit: boolean };
type Rating = 'perfect' | 'good' | 'miss';

const DIRS: Arrow['dir'][] = ['left', 'up', 'down', 'right'];
const DIR_EMOJI: Record<string, string> = { left: '\u2B05\uFE0F', up: '\u2B06\uFE0F', down: '\u2B07\uFE0F', right: '\u27A1\uFE0F' };
const DIR_COLOR: Record<string, string> = { left: '#FF6B6B', up: '#10B981', down: '#3B82F6', right: '#F59E0B' };
const HIT_ZONE_Y = SH - 200;
const SONGS = [
  { name: 'Happy Dance', bpm: 120, pattern: 'LUDRLUDRLURDLURD' },
  { name: 'Funky Moves', bpm: 140, pattern: 'LLRRUUDDLRLRUDUD' },
  { name: 'Star Steps', bpm: 100, pattern: 'UDLRUDLRLRUDDURL' },
];

export default function DanceParty() {
  const { addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [gameState, setGameState] = useState<'ready' | 'playing' | 'over'>('ready');
  const [arrows, setArrows] = useState<Arrow[]>([]);
  const [score, setScore] = useState(0);
  const [combo, setCombo] = useState(0);
  const [maxCombo, setMaxCombo] = useState(0);
  const [rating, setRating] = useState<Rating | null>(null);
  const [perfects, setPerfects] = useState(0);
  const [goods, setGoods] = useState(0);
  const [misses, setMisses] = useState(0);
  const [song] = useState(() => SONGS[Math.floor(Math.random() * SONGS.length)]);
  const idCounter = useRef(0);
  const gameLoop = useRef<ReturnType<typeof setInterval> | null>(null);
  const spawnLoop = useRef<ReturnType<typeof setInterval> | null>(null);
  const patternIdx = useRef(0);
  const ratingAnim = useRef(new Animated.Value(0)).current;

  const showRating = (r: Rating) => {
    setRating(r);
    ratingAnim.setValue(1);
    Animated.timing(ratingAnim, { toValue: 0, duration: 600, useNativeDriver: true }).start();
  };

  const startGame = () => {
    setGameState('playing');
    setScore(0);
    setCombo(0);
    setMaxCombo(0);
    setArrows([]);
    setPerfects(0);
    setGoods(0);
    setMisses(0);
    patternIdx.current = 0;
  };

  const endGame = useCallback(async () => {
    setGameState('over');
    if (gameLoop.current) clearInterval(gameLoop.current);
    if (spawnLoop.current) clearInterval(spawnLoop.current);
    const coinsEarned = Math.floor(score / 50) + perfects * 2;
    addCoins(coinsEarned);
    addXP(Math.floor(score / 20));
    if (maxCombo >= 10) addStars(1);
    if (perfects >= 15) earnSticker('sticker_dance_star');
    await saveGame();
  }, [score, perfects, maxCombo, addCoins, addXP, addStars, earnSticker, saveGame]);

  useEffect(() => {
    if (gameState !== 'playing') return;

    const beatInterval = 60000 / song.bpm;

    spawnLoop.current = setInterval(() => {
      const dirMap: Record<string, Arrow['dir']> = { L: 'left', U: 'up', D: 'down', R: 'right' };
      const patternChar = song.pattern[patternIdx.current % song.pattern.length];
      const dir = dirMap[patternChar] || DIRS[Math.floor(Math.random() * 4)];
      patternIdx.current += 1;
      idCounter.current += 1;
      setArrows(prev => [...prev, { id: idCounter.current, dir, y: -50, hit: false }]);
    }, beatInterval);

    gameLoop.current = setInterval(() => {
      setArrows(prev => {
        const updated = prev.map(a => ({ ...a, y: a.y + 3 }));
        // Check for missed arrows
        const missed = updated.filter(a => !a.hit && a.y > HIT_ZONE_Y + 60);
        if (missed.length > 0) {
          setCombo(0);
          setMisses(m => m + missed.length);
          showRating('miss');
        }
        return updated.filter(a => a.y < SH + 50 && !(a.y > HIT_ZONE_Y + 60 && !a.hit));
      });
    }, 16);

    // End after pattern repeats twice
    const totalDuration = (song.pattern.length * 2 * beatInterval) + 2000;
    const endTimer = setTimeout(() => endGame(), totalDuration);

    return () => {
      if (gameLoop.current) clearInterval(gameLoop.current);
      if (spawnLoop.current) clearInterval(spawnLoop.current);
      clearTimeout(endTimer);
    };
  }, [gameState, song, endGame]);

  const handleTap = (dir: Arrow['dir']) => {
    if (gameState !== 'playing') return;

    const hittable = arrows.filter(a => !a.hit && a.dir === dir && Math.abs(a.y - HIT_ZONE_Y) < 60);
    if (hittable.length === 0) {
      setCombo(0);
      setMisses(m => m + 1);
      showRating('miss');
      return;
    }

    const closest = hittable.reduce((a, b) => Math.abs(a.y - HIT_ZONE_Y) < Math.abs(b.y - HIT_ZONE_Y) ? a : b);
    const dist = Math.abs(closest.y - HIT_ZONE_Y);

    setArrows(prev => prev.filter(a => a.id !== closest.id));

    if (dist < 20) {
      setScore(s => s + 100 * (combo > 5 ? 2 : 1));
      setCombo(c => { const nc = c + 1; setMaxCombo(m => Math.max(m, nc)); return nc; });
      setPerfects(p => p + 1);
      showRating('perfect');
    } else {
      setScore(s => s + 50);
      setCombo(c => { const nc = c + 1; setMaxCombo(m => Math.max(m, nc)); return nc; });
      setGoods(g => g + 1);
      showRating('good');
    }
  };

  if (gameState === 'ready') {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.danceEmoji}>{'\uD83D\uDC83'}</Text>
          <Text style={styles.gameTitle}>Dance Party</Text>
          <Text style={styles.songName}>{song.name}</Text>
          <Text style={styles.hint}>Tap arrows when they reach the line!</Text>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Dance!</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (gameState === 'over') {
    const coinsEarned = Math.floor(score / 50) + perfects * 2;
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.overTitle}>Show Over!</Text>
          <Text style={styles.finalScore}>Score: {score}</Text>
          <View style={styles.statsBox}>
            <Text style={styles.statLine}>Perfect: {perfects}</Text>
            <Text style={styles.statLine}>Good: {goods}</Text>
            <Text style={styles.statLine}>Miss: {misses}</Text>
            <Text style={styles.statLine}>Max Combo: {maxCombo}</Text>
          </View>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>{'\u20B9'}{coinsEarned} coins</Text>
            <Text style={styles.rewardLine}>+{Math.floor(score / 20)} XP</Text>
          </View>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Dance Again</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudScore}>{score}</Text>
        {combo > 1 && <Text style={styles.hudCombo}>x{combo}</Text>}
      </View>

      {/* Hit zone line */}
      <View style={[styles.hitZone, { top: HIT_ZONE_Y }]} />

      {/* Rating popup */}
      <Animated.View style={[styles.ratingPopup, { opacity: ratingAnim, transform: [{ scale: ratingAnim }] }]}>
        <Text style={[styles.ratingText, rating === 'perfect' && styles.ratingPerfect, rating === 'miss' && styles.ratingMiss]}>
          {rating === 'perfect' ? 'PERFECT!' : rating === 'good' ? 'GOOD' : 'MISS'}
        </Text>
      </Animated.View>

      {/* Arrows */}
      {arrows.map(arrow => (
        <Text
          key={arrow.id}
          style={[styles.arrowNote, { top: arrow.y, left: SW / 2 + (DIRS.indexOf(arrow.dir) - 1.5) * 60, color: DIR_COLOR[arrow.dir] }]}
        >
          {DIR_EMOJI[arrow.dir]}
        </Text>
      ))}

      {/* Tap buttons */}
      <View style={styles.tapRow}>
        {DIRS.map(dir => (
          <TouchableOpacity key={dir} style={[styles.tapBtn, { backgroundColor: DIR_COLOR[dir] + '30' }]} onPress={() => handleTap(dir)}>
            <Text style={[styles.tapEmoji, { color: DIR_COLOR[dir] }]}>{DIR_EMOJI[dir]}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#1a1a2e' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  danceEmoji: { fontSize: 80 },
  gameTitle: { fontSize: 32, fontWeight: '800', color: '#fff', marginTop: 12 },
  songName: { fontSize: 16, fontWeight: '700', color: '#A855F7', marginTop: 4 },
  hint: { fontSize: 14, color: '#999', marginTop: 12 },
  playBtn: { marginTop: 30, backgroundColor: '#A855F7', paddingHorizontal: 40, paddingVertical: 16, borderRadius: 20 },
  playBtnText: { fontSize: 22, fontWeight: '800', color: '#fff' },
  backLink: { marginTop: 16 },
  backLinkText: { fontSize: 14, fontWeight: '700', color: '#666' },
  hud: { position: 'absolute', top: 50, left: 0, right: 0, flexDirection: 'row', justifyContent: 'center', gap: 20, zIndex: 10 },
  hudScore: { fontSize: 28, fontWeight: '800', color: '#fff' },
  hudCombo: { fontSize: 24, fontWeight: '800', color: '#F59E0B' },
  hitZone: { position: 'absolute', left: 0, right: 0, height: 3, backgroundColor: 'rgba(255,255,255,0.3)' },
  ratingPopup: { position: 'absolute', top: SH / 2 - 40, left: 0, right: 0, alignItems: 'center', zIndex: 20 },
  ratingText: { fontSize: 32, fontWeight: '800', color: '#10B981' },
  ratingPerfect: { color: '#F59E0B', fontSize: 36 },
  ratingMiss: { color: '#FF6B6B' },
  arrowNote: { position: 'absolute', fontSize: 36, width: 50, textAlign: 'center' },
  tapRow: { position: 'absolute', bottom: 40, left: 0, right: 0, flexDirection: 'row', justifyContent: 'center', gap: 12 },
  tapBtn: { width: 70, height: 70, borderRadius: 18, alignItems: 'center', justifyContent: 'center' },
  tapEmoji: { fontSize: 32 },
  overTitle: { fontSize: 36, fontWeight: '800', color: '#A855F7' },
  finalScore: { fontSize: 24, fontWeight: '800', color: '#fff', marginTop: 8 },
  statsBox: { backgroundColor: 'rgba(255,255,255,0.1)', padding: 16, borderRadius: 16, marginTop: 12, width: '100%', gap: 4 },
  statLine: { fontSize: 16, fontWeight: '700', color: '#ddd' },
  rewardBox: { backgroundColor: 'rgba(255,255,255,0.1)', padding: 16, borderRadius: 16, marginTop: 12, width: '100%', gap: 4 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#F59E0B' },
});
