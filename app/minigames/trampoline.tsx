/**
 * Trampoline - Jump timing game with combo multipliers and tricks
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, Animated } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

const { width: SW, height: SH } = Dimensions.get('window');

const TRICKS = [
  { id: 'flip', name: 'Flip', emoji: '\uD83E\uDD38', points: 50 },
  { id: 'spin', name: 'Spin', emoji: '\uD83C\uDF00', points: 30 },
  { id: 'star', name: 'Star Jump', emoji: '\u2B50', points: 40 },
  { id: 'split', name: 'Split', emoji: '\uD83E\uDDB8', points: 60 },
];

export default function Trampoline() {
  const { addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [gameState, setGameState] = useState<'ready' | 'playing' | 'over'>('ready');
  const [score, setScore] = useState(0);
  const [combo, setCombo] = useState(0);
  const [maxCombo, setMaxCombo] = useState(0);
  const [jumpsLeft, setJumpsLeft] = useState(20);
  const [currentTrick, setCurrentTrick] = useState<typeof TRICKS[0] | null>(null);
  const [isJumping, setIsJumping] = useState(false);
  const [showTrick, setShowTrick] = useState(false);
  const [feedback, setFeedback] = useState('');
  const bounceAnim = useRef(new Animated.Value(0)).current;
  const scaleAnim = useRef(new Animated.Value(1)).current;
  const trickTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const startGame = () => {
    setGameState('playing');
    setScore(0);
    setCombo(0);
    setMaxCombo(0);
    setJumpsLeft(20);
    setFeedback('');
    setCurrentTrick(null);
    setShowTrick(false);
  };

  const handleJump = () => {
    if (isJumping || jumpsLeft <= 0) return;
    setIsJumping(true);
    setJumpsLeft(j => j - 1);

    // Bounce animation
    Animated.sequence([
      Animated.timing(bounceAnim, { toValue: -150, duration: 300, useNativeDriver: true }),
      Animated.timing(bounceAnim, { toValue: 0, duration: 400, useNativeDriver: true }),
    ]).start(() => setIsJumping(false));

    // Random trick prompt
    if (Math.random() > 0.4) {
      const trick = TRICKS[Math.floor(Math.random() * TRICKS.length)];
      setCurrentTrick(trick);
      setShowTrick(true);

      trickTimer.current = setTimeout(() => {
        // Missed trick
        setShowTrick(false);
        setCurrentTrick(null);
        setCombo(0);
        setFeedback('Too slow!');
        setTimeout(() => setFeedback(''), 600);
      }, 1500);
    } else {
      // Normal jump
      const pts = 10 * (1 + Math.floor(combo / 3));
      setScore(s => s + pts);
      setCombo(c => { const nc = c + 1; setMaxCombo(m => Math.max(m, nc)); return nc; });
      setFeedback(`+${pts}`);
      setTimeout(() => setFeedback(''), 400);
    }
  };

  const handleTrick = (trick: typeof TRICKS[0]) => {
    if (!showTrick || !currentTrick) return;

    if (trickTimer.current) clearTimeout(trickTimer.current);
    setShowTrick(false);

    if (trick.id === currentTrick.id) {
      const pts = trick.points * (1 + Math.floor(combo / 3));
      setScore(s => s + pts);
      setCombo(c => { const nc = c + 1; setMaxCombo(m => Math.max(m, nc)); return nc; });
      setFeedback(`${trick.name}! +${pts}`);

      Animated.sequence([
        Animated.timing(scaleAnim, { toValue: 1.3, duration: 150, useNativeDriver: true }),
        Animated.timing(scaleAnim, { toValue: 1, duration: 150, useNativeDriver: true }),
      ]).start();
    } else {
      setCombo(0);
      setFeedback('Wrong trick!');
    }

    setCurrentTrick(null);
    setTimeout(() => setFeedback(''), 600);
  };

  useEffect(() => {
    if (gameState === 'playing' && jumpsLeft <= 0 && !isJumping) {
      endGame();
    }
  }, [jumpsLeft, isJumping, gameState]);

  const endGame = useCallback(async () => {
    setGameState('over');
    const coinsEarned = Math.floor(score / 30) + maxCombo * 2;
    addCoins(coinsEarned);
    addXP(Math.floor(score / 15));
    if (maxCombo >= 8) addStars(1);
    if (score > 500) earnSticker('sticker_trampoline_pro');
    await saveGame();
  }, [score, maxCombo, addCoins, addXP, addStars, earnSticker, saveGame]);

  if (gameState === 'ready') {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.jumpEmoji}>{'\uD83E\uDD38'}</Text>
          <Text style={styles.gameTitle}>Trampoline Jump</Text>
          <Text style={styles.hint}>Tap to bounce! Match the trick when prompted!</Text>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Jump!</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (gameState === 'over') {
    const coinsEarned = Math.floor(score / 30) + maxCombo * 2;
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.overTitle}>Great Jumping!</Text>
          <Text style={styles.finalScore}>Score: {score}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>Max Combo: {maxCombo}</Text>
            <Text style={styles.rewardLine}>{'\u20B9'}{coinsEarned} coins</Text>
            <Text style={styles.rewardLine}>+{Math.floor(score / 15)} XP</Text>
          </View>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Jump Again</Text>
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
        <Text style={styles.hudJumps}>{jumpsLeft} jumps</Text>
        {combo > 1 && <Text style={styles.hudCombo}>x{combo}</Text>}
      </View>

      {/* Character */}
      <View style={styles.characterArea}>
        <Animated.View style={{ transform: [{ translateY: bounceAnim }, { scale: scaleAnim }] }}>
          <Text style={styles.characterEmoji}>{'\uD83E\uDDD2'}</Text>
        </Animated.View>
        {feedback !== '' && <Text style={styles.feedbackText}>{feedback}</Text>}
      </View>

      {/* Trick prompt */}
      {showTrick && currentTrick && (
        <View style={styles.trickPrompt}>
          <Text style={styles.trickLabel}>Do this trick!</Text>
          <Text style={styles.trickEmoji}>{currentTrick.emoji}</Text>
          <Text style={styles.trickName}>{currentTrick.name}</Text>
        </View>
      )}

      {/* Trick buttons (shown when trick is prompted) */}
      {showTrick && (
        <View style={styles.trickRow}>
          {TRICKS.map(trick => (
            <TouchableOpacity key={trick.id} style={styles.trickBtn} onPress={() => handleTrick(trick)}>
              <Text style={styles.trickBtnEmoji}>{trick.emoji}</Text>
              <Text style={styles.trickBtnName}>{trick.name}</Text>
            </TouchableOpacity>
          ))}
        </View>
      )}

      {/* Trampoline / Jump button */}
      <View style={styles.trampolineArea}>
        <TouchableOpacity style={styles.trampolineBtn} onPress={handleJump} disabled={isJumping}>
          <View style={styles.trampoline} />
          <Text style={styles.tapHint}>{isJumping ? 'Wheee!' : 'TAP!'}</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#E8F8E8' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  jumpEmoji: { fontSize: 80 },
  gameTitle: { fontSize: 32, fontWeight: '800', color: '#333', marginTop: 12 },
  hint: { fontSize: 14, color: '#999', marginTop: 12, textAlign: 'center' },
  playBtn: { marginTop: 30, backgroundColor: '#10B981', paddingHorizontal: 40, paddingVertical: 16, borderRadius: 20 },
  playBtnText: { fontSize: 22, fontWeight: '800', color: '#fff' },
  backLink: { marginTop: 16 },
  backLinkText: { fontSize: 14, fontWeight: '700', color: '#999' },
  hud: { flexDirection: 'row', justifyContent: 'space-between', paddingHorizontal: 16, paddingTop: 50 },
  hudScore: { fontSize: 24, fontWeight: '800', color: '#333' },
  hudJumps: { fontSize: 16, fontWeight: '700', color: '#666' },
  hudCombo: { fontSize: 20, fontWeight: '800', color: '#10B981' },
  characterArea: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  characterEmoji: { fontSize: 72 },
  feedbackText: { fontSize: 24, fontWeight: '800', color: '#10B981', marginTop: 8, position: 'absolute', top: '30%' },
  trickPrompt: { alignItems: 'center', paddingVertical: 10 },
  trickLabel: { fontSize: 14, fontWeight: '800', color: '#F59E0B' },
  trickEmoji: { fontSize: 48 },
  trickName: { fontSize: 16, fontWeight: '800', color: '#333' },
  trickRow: { flexDirection: 'row', justifyContent: 'center', gap: 10, paddingHorizontal: 16 },
  trickBtn: { width: 75, height: 80, backgroundColor: '#fff', borderRadius: 16, alignItems: 'center', justifyContent: 'center' },
  trickBtnEmoji: { fontSize: 28 },
  trickBtnName: { fontSize: 10, fontWeight: '800', color: '#666', marginTop: 2 },
  trampolineArea: { alignItems: 'center', paddingBottom: 40 },
  trampolineBtn: { alignItems: 'center' },
  trampoline: { width: SW * 0.6, height: 20, backgroundColor: '#10B981', borderRadius: 10 },
  tapHint: { fontSize: 18, fontWeight: '800', color: '#10B981', marginTop: 8 },
  overTitle: { fontSize: 32, fontWeight: '800', color: '#10B981' },
  finalScore: { fontSize: 24, fontWeight: '800', color: '#333', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 16, width: '100%', gap: 8 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#333' },
});
