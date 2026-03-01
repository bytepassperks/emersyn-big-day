import React, { useState, useEffect, useRef, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getBeltDisplayName, getBeltEmoji, getRandomEncouragement } from '@/lib/helpers';

type KaratePhase = 'menu' | 'countdown' | 'playing' | 'results';
type Move = 'punch' | 'kick' | 'block' | 'chop';

interface Sequence {
  moves: Move[];
  timeLimit: number;
}

const MOVE_EMOJIS: Record<Move, string> = { punch: '👊', kick: '🦵', block: '🛡️', chop: '🤚' };
const MOVE_NAMES: Record<Move, string> = { punch: 'Punch', kick: 'Kick', block: 'Block', chop: 'Chop' };
const ALL_MOVES: Move[] = ['punch', 'kick', 'block', 'chop'];

const generateSequence = (difficulty: number): Sequence => {
  const length = Math.min(3 + difficulty, 8);
  const moves: Move[] = Array.from({ length }, () => ALL_MOVES[Math.floor(Math.random() * ALL_MOVES.length)]);
  const timeLimit = Math.max(5000 - difficulty * 300, 2000);
  return { moves, timeLimit };
};

export default function KarateStar() {
  const router = useRouter();
  const { karateBelt, karateXP, addCoins, addXP, addStars, addKarateXP, promoteBelt, earnSticker, recordMiniGameResult, saveGame } = useGameStore();

  const [phase, setPhase] = useState<KaratePhase>('menu');
  const [countdown, setCountdown] = useState(3);
  const [round, setRound] = useState(0);
  const [totalRounds] = useState(5);
  const [score, setScore] = useState(0);
  const [sequence, setSequence] = useState<Sequence | null>(null);
  const [playerMoves, setPlayerMoves] = useState<Move[]>([]);
  const [showSequence, setShowSequence] = useState(true);
  const [currentShowIndex, setCurrentShowIndex] = useState(0);
  const [timeLeft, setTimeLeft] = useState(0);
  const [feedback, setFeedback] = useState('');
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startGame = () => {
    setPhase('countdown');
    setCountdown(3);
    setRound(0);
    setScore(0);
  };

  // Countdown
  useEffect(() => {
    if (phase !== 'countdown') return;
    if (countdown <= 0) {
      startRound(0);
      return;
    }
    const timer = setTimeout(() => setCountdown((c) => c - 1), 1000);
    return () => clearTimeout(timer);
  }, [phase, countdown]);

  const startRound = (roundNum: number) => {
    const seq = generateSequence(roundNum);
    setSequence(seq);
    setPlayerMoves([]);
    setShowSequence(true);
    setCurrentShowIndex(0);
    setRound(roundNum);
    setPhase('playing');
    setTimeLeft(seq.timeLimit);

    // Show sequence one by one
    let idx = 0;
    const showTimer = setInterval(() => {
      idx++;
      if (idx >= seq.moves.length) {
        clearInterval(showTimer);
        setTimeout(() => {
          setShowSequence(false);
          // Start countdown timer
          timerRef.current = setInterval(() => {
            setTimeLeft((prev) => {
              if (prev <= 100) {
                if (timerRef.current) clearInterval(timerRef.current);
                handleRoundEnd(false);
                return 0;
              }
              return prev - 100;
            });
          }, 100);
        }, 500);
      } else {
        setCurrentShowIndex(idx);
      }
    }, 600);
  };

  const handleRoundEnd = useCallback((success: boolean) => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    if (success) {
      const roundScore = 10 + round * 5;
      setScore((prev) => prev + roundScore);
      setFeedback('HAI-YA! 🥋');
    } else {
      setFeedback('Try harder! 💪');
    }

    setTimeout(() => {
      setFeedback('');
      if (round < totalRounds - 1) {
        startRound(round + 1);
      } else {
        endGame();
      }
    }, 1500);
  }, [round, totalRounds]);

  const handleMove = (move: Move) => {
    if (showSequence || !sequence) return;

    const newMoves = [...playerMoves, move];
    setPlayerMoves(newMoves);

    const idx = newMoves.length - 1;
    if (newMoves[idx] !== sequence.moves[idx]) {
      // Wrong move
      handleRoundEnd(false);
      return;
    }

    if (newMoves.length === sequence.moves.length) {
      // Completed sequence correctly
      handleRoundEnd(true);
    }
  };

  const endGame = useCallback(async () => {
    setPhase('results');

    const coinReward = score + 10;
    const xpReward = score;
    const karateXPEarned = Math.floor(score / 2);

    addCoins(coinReward);
    addXP(xpReward);
    addStars(Math.floor(score / 20));
    addKarateXP(karateXPEarned);
    earnSticker('sticker_karate_belt');
    recordMiniGameResult({ gameId: 'karate_star', score, coinsEarned: coinReward, playedAt: Date.now() });

    // Check belt promotion
    promoteBelt();

    await saveGame();
  }, [score]);

  if (phase === 'menu') {
    return (
      <ScreenWrapper title="Karate Star" emoji="🥋" bgColor={Colors.yellowLight}>
        <View style={styles.menuArea}>
          <Text style={styles.menuEmoji}>🥋</Text>
          <Text style={styles.menuTitle}>Karate Star!</Text>

          <View style={styles.beltCard}>
            <Text style={styles.beltEmoji}>{getBeltEmoji(karateBelt)}</Text>
            <Text style={styles.beltName}>{getBeltDisplayName(karateBelt)}</Text>
            <Text style={styles.beltXP}>XP: {karateXP}</Text>
          </View>

          <Text style={styles.menuDesc}>
            Watch the sequence, then repeat the moves!{'\n'}
            Punch, Kick, Block, Chop!
          </Text>

          <GameButton title="Train! 🥋" onPress={startGame} variant="accent" size="large" style={{ marginTop: 20 }} />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'countdown') {
    return (
      <View style={styles.countdownScreen}>
        <Text style={styles.countdownText}>{countdown > 0 ? countdown : 'FIGHT!'}</Text>
      </View>
    );
  }

  if (phase === 'results') {
    return (
      <ScreenWrapper title="Results" emoji="🏆" bgColor={Colors.yellowLight} showBack={false}>
        <View style={styles.resultsArea}>
          <Text style={styles.resultsEmoji}>{getBeltEmoji(karateBelt)}</Text>
          <Text style={styles.resultsTitle}>{getRandomEncouragement()}</Text>
          <Text style={styles.resultsScore}>Score: {score}</Text>
          <Text style={styles.beltProgress}>{getBeltDisplayName(karateBelt)}</Text>

          <View style={styles.rewardCard}>
            <Text style={styles.rewardText}>💰 +₹{score + 10}</Text>
            <Text style={styles.rewardText}>⭐ +{score} XP</Text>
            <Text style={styles.rewardText}>🥋 +{Math.floor(score / 2)} Karate XP</Text>
          </View>

          <GameButton title="Train Again" emoji="🥋" onPress={startGame} variant="accent" size="large" style={{ marginTop: 16 }} />
          <GameButton title="Back" emoji="🏠" onPress={() => router.back()} variant="outline" size="medium" style={{ marginTop: 8 }} />
        </View>
      </ScreenWrapper>
    );
  }

  // Playing
  return (
    <View style={styles.gameScreen}>
      <View style={styles.hud}>
        <Text style={styles.hudText}>Round {round + 1}/{totalRounds}</Text>
        <Text style={styles.hudText}>🏁 {score}</Text>
        <Text style={styles.hudText}>⏱️ {(timeLeft / 1000).toFixed(1)}s</Text>
      </View>

      {/* Sequence Display */}
      <View style={styles.sequenceArea}>
        {showSequence && sequence ? (
          <View style={styles.sequenceRow}>
            <Text style={styles.watchText}>Watch carefully!</Text>
            <Text style={styles.sequenceEmoji}>{MOVE_EMOJIS[sequence.moves[currentShowIndex]]}</Text>
            <Text style={styles.sequenceName}>{MOVE_NAMES[sequence.moves[currentShowIndex]]}</Text>
          </View>
        ) : (
          <View style={styles.sequenceRow}>
            <Text style={styles.watchText}>Your turn! Repeat the sequence!</Text>
            <View style={styles.moveProgress}>
              {sequence?.moves.map((_, i) => (
                <View
                  key={i}
                  style={[
                    styles.moveDot,
                    i < playerMoves.length && styles.moveDotDone,
                  ]}
                />
              ))}
            </View>
          </View>
        )}
        {feedback !== '' && <Text style={styles.feedbackText}>{feedback}</Text>}
      </View>

      {/* Move Buttons */}
      {!showSequence && (
        <View style={styles.moveGrid}>
          {ALL_MOVES.map((move) => (
            <TouchableOpacity
              key={move}
              style={styles.moveBtn}
              onPress={() => handleMove(move)}
              activeOpacity={0.6}
            >
              <Text style={styles.moveBtnEmoji}>{MOVE_EMOJIS[move]}</Text>
              <Text style={styles.moveBtnLabel}>{MOVE_NAMES[move]}</Text>
            </TouchableOpacity>
          ))}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  menuArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  menuEmoji: { fontSize: 64 },
  menuTitle: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  menuDesc: { fontSize: 14, color: Colors.gray500, textAlign: 'center', marginTop: 16 },
  beltCard: {
    backgroundColor: Colors.white, padding: 16, borderRadius: 18, marginTop: 16,
    alignItems: 'center', borderWidth: 2, borderColor: Colors.yellowLight, width: '80%',
  },
  beltEmoji: { fontSize: 40 },
  beltName: { fontSize: 18, fontWeight: '800', color: Colors.dark, marginTop: 4 },
  beltXP: { fontSize: 14, color: Colors.gray500, marginTop: 2 },
  countdownScreen: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.yellowLight },
  countdownText: { fontSize: 72, fontWeight: '800', color: Colors.dark },
  gameScreen: { flex: 1, backgroundColor: Colors.yellowLight, paddingTop: 50 },
  hud: {
    flexDirection: 'row', justifyContent: 'space-around', paddingHorizontal: 20, paddingVertical: 8,
    backgroundColor: Colors.overlay, borderRadius: 16, marginHorizontal: 16,
  },
  hudText: { fontSize: 16, fontWeight: '800', color: Colors.white },
  sequenceArea: { flex: 1, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 20 },
  sequenceRow: { alignItems: 'center' },
  watchText: { fontSize: 18, fontWeight: '700', color: Colors.dark, marginBottom: 16 },
  sequenceEmoji: { fontSize: 100 },
  sequenceName: { fontSize: 24, fontWeight: '800', color: Colors.dark, marginTop: 8 },
  moveProgress: { flexDirection: 'row', gap: 10, marginTop: 16 },
  moveDot: { width: 16, height: 16, borderRadius: 8, backgroundColor: Colors.gray200 },
  moveDotDone: { backgroundColor: Colors.success },
  feedbackText: { fontSize: 28, fontWeight: '800', color: Colors.pink, marginTop: 16 },
  moveGrid: {
    flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'center', gap: 12,
    paddingHorizontal: 20, paddingBottom: 40,
  },
  moveBtn: {
    width: '44%', aspectRatio: 1.2, borderRadius: 20, backgroundColor: Colors.white,
    justifyContent: 'center', alignItems: 'center',
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.15, shadowRadius: 4, elevation: 4,
  },
  moveBtnEmoji: { fontSize: 40 },
  moveBtnLabel: { fontSize: 14, fontWeight: '800', color: Colors.dark, marginTop: 4 },
  resultsArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 20 },
  resultsEmoji: { fontSize: 64 },
  resultsTitle: { fontSize: 22, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  resultsScore: { fontSize: 32, fontWeight: '800', color: Colors.orange, marginTop: 8 },
  beltProgress: { fontSize: 16, color: Colors.gray500, marginTop: 4 },
  rewardCard: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 16,
    width: '100%', borderWidth: 2, borderColor: Colors.yellowLight, gap: 8,
  },
  rewardText: { fontSize: 18, fontWeight: '700', color: Colors.dark },
});
