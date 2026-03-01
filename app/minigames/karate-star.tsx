/**
 * Karate Star - Quick-time event combat with belt progression
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, Animated } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

const { width: SW } = Dimensions.get('window');

type Move = 'punch' | 'kick' | 'block' | 'jump';
const MOVES: { id: Move; emoji: string; name: string }[] = [
  { id: 'punch', emoji: '\uD83E\uDD4A', name: 'Punch' },
  { id: 'kick', emoji: '\uD83E\uDD3C', name: 'Kick' },
  { id: 'block', emoji: '\uD83D\uDEE1\uFE0F', name: 'Block' },
  { id: 'jump', emoji: '\uD83E\uDD38', name: 'Jump' },
];

const BELTS = [
  { name: 'White Belt', color: '#FFFFFF', combosNeeded: 0 },
  { name: 'Yellow Belt', color: '#F59E0B', combosNeeded: 5 },
  { name: 'Orange Belt', color: '#F97316', combosNeeded: 12 },
  { name: 'Green Belt', color: '#10B981', combosNeeded: 20 },
  { name: 'Blue Belt', color: '#3B82F6', combosNeeded: 30 },
  { name: 'Brown Belt', color: '#92400E', combosNeeded: 45 },
  { name: 'Black Belt', color: '#1F2937', combosNeeded: 60 },
];

const OPPONENTS = [
  { name: 'Training Dummy', emoji: '\uD83E\uDDD1', difficulty: 1 },
  { name: 'Junior Student', emoji: '\uD83E\uDDD2', difficulty: 2 },
  { name: 'Senior Student', emoji: '\uD83E\uDDD3', difficulty: 3 },
  { name: 'Sensei', emoji: '\uD83E\uDDB8', difficulty: 4 },
];

export default function KarateStar() {
  const { addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [gameState, setGameState] = useState<'ready' | 'playing' | 'result'>('ready');
  const [sequence, setSequence] = useState<Move[]>([]);
  const [playerInput, setPlayerInput] = useState<Move[]>([]);
  const [showSequence, setShowSequence] = useState(true);
  const [currentShowIdx, setCurrentShowIdx] = useState(0);
  const [round, setRound] = useState(1);
  const [score, setScore] = useState(0);
  const [streak, setStreak] = useState(0);
  const [maxStreak, setMaxStreak] = useState(0);
  const [opponent] = useState(() => OPPONENTS[Math.floor(Math.random() * OPPONENTS.length)]);
  const [feedback, setFeedback] = useState('');
  const flashAnim = useRef(new Animated.Value(0)).current;

  const getCurrentBelt = () => {
    let belt = BELTS[0];
    for (const b of BELTS) {
      if (streak >= b.combosNeeded) belt = b;
    }
    return belt;
  };

  const generateSequence = (len: number): Move[] => {
    return Array.from({ length: len }, () => MOVES[Math.floor(Math.random() * MOVES.length)].id);
  };

  const startGame = () => {
    setGameState('playing');
    setScore(0);
    setStreak(0);
    setMaxStreak(0);
    setRound(1);
    startRound(1);
  };

  const startRound = (r: number) => {
    const len = Math.min(3 + Math.floor(r / 2), 8);
    const seq = generateSequence(len);
    setSequence(seq);
    setPlayerInput([]);
    setShowSequence(true);
    setCurrentShowIdx(0);

    // Show sequence one by one
    let idx = 0;
    const showInterval = setInterval(() => {
      setCurrentShowIdx(idx);
      idx += 1;
      if (idx >= seq.length) {
        clearInterval(showInterval);
        setTimeout(() => {
          setShowSequence(false);
        }, 600);
      }
    }, 600);
  };

  const handleMove = (move: Move) => {
    if (showSequence) return;

    const nextIdx = playerInput.length;
    const expected = sequence[nextIdx];

    if (move === expected) {
      const newInput = [...playerInput, move];
      setPlayerInput(newInput);

      Animated.sequence([
        Animated.timing(flashAnim, { toValue: 1, duration: 100, useNativeDriver: true }),
        Animated.timing(flashAnim, { toValue: 0, duration: 100, useNativeDriver: true }),
      ]).start();

      if (newInput.length === sequence.length) {
        // Round complete!
        const roundScore = sequence.length * 10 * opponent.difficulty;
        setScore(s => s + roundScore);
        setStreak(s => { const ns = s + 1; setMaxStreak(m => Math.max(m, ns)); return ns; });
        setFeedback('HAI-YA!');
        setTimeout(() => {
          setFeedback('');
          if (round >= 10) {
            endGame();
          } else {
            setRound(r => r + 1);
            startRound(round + 1);
          }
        }, 800);
      }
    } else {
      setStreak(0);
      setFeedback('Miss!');
      setTimeout(() => {
        setFeedback('');
        startRound(round);
      }, 800);
    }
  };

  const endGame = useCallback(async () => {
    setGameState('result');
    const coinsEarned = Math.floor(score / 20) + maxStreak * 3;
    addCoins(coinsEarned);
    addXP(Math.floor(score / 10));
    if (maxStreak >= 5) addStars(1);
    if (maxStreak >= 10) earnSticker('sticker_karate_master');
    await saveGame();
  }, [score, maxStreak, addCoins, addXP, addStars, earnSticker, saveGame]);

  const belt = getCurrentBelt();

  if (gameState === 'ready') {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.karateEmoji}>{'\uD83E\uDD4B'}</Text>
          <Text style={styles.gameTitle}>Karate Star</Text>
          <View style={styles.opponentCard}>
            <Text style={styles.oppEmoji}>{opponent.emoji}</Text>
            <Text style={styles.oppName}>vs {opponent.name}</Text>
          </View>
          <Text style={styles.hint}>Watch the moves, then repeat them!</Text>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Fight!</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (gameState === 'result') {
    const coinsEarned = Math.floor(score / 20) + maxStreak * 3;
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.beltEmoji}>{'\uD83E\uDD4B'}</Text>
          <Text style={[styles.beltName, { color: belt.color === '#FFFFFF' ? '#333' : belt.color }]}>{belt.name}</Text>
          <Text style={styles.finalScore}>Score: {score}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>Max Streak: {maxStreak}</Text>
            <Text style={styles.rewardLine}>{'\u20B9'}{coinsEarned} coins</Text>
            <Text style={styles.rewardLine}>+{Math.floor(score / 10)} XP</Text>
          </View>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Train Again</Text>
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
        <Text style={styles.hudRound}>Round {round}/10</Text>
        <Text style={styles.hudScore}>{score}</Text>
        <View style={[styles.beltBadge, { backgroundColor: belt.color }]}>
          <Text style={styles.beltBadgeText}>{belt.name}</Text>
        </View>
      </View>

      {/* Opponent */}
      <View style={styles.opponentArea}>
        <Animated.Text style={[styles.oppBig, { opacity: flashAnim.interpolate({ inputRange: [0, 1], outputRange: [1, 0.5] }) }]}>
          {opponent.emoji}
        </Animated.Text>
        {feedback !== '' && <Text style={styles.feedbackText}>{feedback}</Text>}
      </View>

      {/* Sequence display */}
      <View style={styles.seqDisplay}>
        {showSequence ? (
          <View style={styles.seqRow}>
            {sequence.map((move, i) => (
              <View key={i} style={[styles.seqItem, i === currentShowIdx && styles.seqItemActive]}>
                <Text style={styles.seqEmoji}>{i <= currentShowIdx ? MOVES.find(m => m.id === move)?.emoji : '?'}</Text>
              </View>
            ))}
          </View>
        ) : (
          <View style={styles.seqRow}>
            {sequence.map((_, i) => (
              <View key={i} style={[styles.seqItem, i < playerInput.length && styles.seqItemDone]}>
                <Text style={styles.seqEmoji}>{i < playerInput.length ? MOVES.find(m => m.id === playerInput[i])?.emoji : '?'}</Text>
              </View>
            ))}
          </View>
        )}
        <Text style={styles.seqHint}>{showSequence ? 'Watch carefully...' : 'Your turn!'}</Text>
      </View>

      {/* Move buttons */}
      <View style={styles.moveRow}>
        {MOVES.map(move => (
          <TouchableOpacity
            key={move.id}
            style={[styles.moveBtn, showSequence && styles.moveBtnDisabled]}
            onPress={() => handleMove(move.id)}
            disabled={showSequence}
          >
            <Text style={styles.moveEmoji}>{move.emoji}</Text>
            <Text style={styles.moveName}>{move.name}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF0E0' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  karateEmoji: { fontSize: 80 },
  gameTitle: { fontSize: 32, fontWeight: '800', color: '#333', marginTop: 12 },
  opponentCard: { marginTop: 16, alignItems: 'center' },
  oppEmoji: { fontSize: 48 },
  oppName: { fontSize: 16, fontWeight: '700', color: '#666', marginTop: 4 },
  hint: { fontSize: 14, color: '#999', marginTop: 12 },
  playBtn: { marginTop: 30, backgroundColor: '#F59E0B', paddingHorizontal: 40, paddingVertical: 16, borderRadius: 20 },
  playBtnText: { fontSize: 22, fontWeight: '800', color: '#fff' },
  backLink: { marginTop: 16 },
  backLinkText: { fontSize: 14, fontWeight: '700', color: '#999' },
  hud: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingTop: 50 },
  hudRound: { fontSize: 16, fontWeight: '800', color: '#666' },
  hudScore: { fontSize: 24, fontWeight: '800', color: '#333' },
  beltBadge: { paddingHorizontal: 12, paddingVertical: 4, borderRadius: 10, borderWidth: 1, borderColor: '#ddd' },
  beltBadgeText: { fontSize: 12, fontWeight: '800', color: '#333' },
  opponentArea: { alignItems: 'center', paddingVertical: 30 },
  oppBig: { fontSize: 80 },
  feedbackText: { fontSize: 28, fontWeight: '800', color: '#F59E0B', marginTop: 8 },
  seqDisplay: { alignItems: 'center', paddingVertical: 20 },
  seqRow: { flexDirection: 'row', gap: 10, justifyContent: 'center' },
  seqItem: { width: 50, height: 50, borderRadius: 14, backgroundColor: '#f0f0f0', alignItems: 'center', justifyContent: 'center' },
  seqItemActive: { backgroundColor: '#F59E0B', transform: [{ scale: 1.2 }] },
  seqItemDone: { backgroundColor: '#10B981' },
  seqEmoji: { fontSize: 24 },
  seqHint: { fontSize: 14, color: '#999', marginTop: 10 },
  moveRow: { flexDirection: 'row', justifyContent: 'center', gap: 12, paddingVertical: 20, paddingBottom: 40 },
  moveBtn: { width: 80, height: 90, backgroundColor: '#fff', borderRadius: 18, alignItems: 'center', justifyContent: 'center', gap: 4, elevation: 2 },
  moveBtnDisabled: { opacity: 0.4 },
  moveEmoji: { fontSize: 32 },
  moveName: { fontSize: 11, fontWeight: '800', color: '#666' },
  beltEmoji: { fontSize: 80 },
  beltName: { fontSize: 28, fontWeight: '800', marginTop: 8 },
  finalScore: { fontSize: 24, fontWeight: '800', color: '#333', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 16, width: '100%', gap: 8 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#333' },
});
