import React, { useState, useEffect, useRef, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Dimensions, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getRandomEncouragement } from '@/lib/helpers';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

type DancePhase = 'menu' | 'countdown' | 'playing' | 'results';
type Arrow = '⬆️' | '⬇️' | '⬅️' | '➡️';

interface Note {
  id: number;
  arrow: Arrow;
  targetTime: number;
  hit: boolean | null;
}

const ARROWS: Arrow[] = ['⬆️', '⬇️', '⬅️', '➡️'];
const ARROW_LABELS: Record<Arrow, string> = { '⬆️': 'UP', '⬇️': 'DOWN', '⬅️': 'LEFT', '➡️': 'RIGHT' };
const SONG_DURATION = 20000; // 20 seconds
const NOTE_INTERVAL = 800;

const generateSong = (): Note[] => {
  const notes: Note[] = [];
  let id = 0;
  for (let t = 2000; t < SONG_DURATION; t += NOTE_INTERVAL + Math.random() * 400) {
    notes.push({
      id: id++,
      arrow: ARROWS[Math.floor(Math.random() * ARROWS.length)],
      targetTime: t,
      hit: null,
    });
  }
  return notes;
};

export default function DanceParty() {
  const router = useRouter();
  const { addCoins, addXP, addStars, addDanceStars, earnSticker, recordMiniGameResult, saveGame } = useGameStore();

  const [phase, setPhase] = useState<DancePhase>('menu');
  const [countdown, setCountdown] = useState(3);
  const [notes, setNotes] = useState<Note[]>([]);
  const [currentNoteIndex, setCurrentNoteIndex] = useState(0);
  const [score, setScore] = useState(0);
  const [combo, setCombo] = useState(0);
  const [maxCombo, setMaxCombo] = useState(0);
  const [perfect, setPerfect] = useState(0);
  const [good, setGood] = useState(0);
  const [miss, setMiss] = useState(0);
  const [showFeedback, setShowFeedback] = useState('');
  const startTime = useRef(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startGame = () => {
    setPhase('countdown');
    setCountdown(3);
    setNotes(generateSong());
    setCurrentNoteIndex(0);
    setScore(0);
    setCombo(0);
    setMaxCombo(0);
    setPerfect(0);
    setGood(0);
    setMiss(0);
  };

  // Countdown
  useEffect(() => {
    if (phase !== 'countdown') return;
    if (countdown <= 0) {
      setPhase('playing');
      startTime.current = Date.now();
      return;
    }
    const timer = setTimeout(() => setCountdown((c) => c - 1), 1000);
    return () => clearTimeout(timer);
  }, [phase, countdown]);

  // Game timer - advance and check misses
  useEffect(() => {
    if (phase !== 'playing') return;

    timerRef.current = setInterval(() => {
      const elapsed = Date.now() - startTime.current;

      // Check for missed notes
      if (currentNoteIndex < notes.length) {
        const currentNote = notes[currentNoteIndex];
        if (elapsed > currentNote.targetTime + 500) {
          setNotes((prev) => {
            const updated = [...prev];
            updated[currentNoteIndex] = { ...updated[currentNoteIndex], hit: false };
            return updated;
          });
          setCurrentNoteIndex((prev) => prev + 1);
          setMiss((prev) => prev + 1);
          setCombo(0);
          setShowFeedback('Miss! 😢');
          setTimeout(() => setShowFeedback(''), 500);
        }
      }

      // Check if song is over
      if (elapsed > SONG_DURATION) {
        if (timerRef.current) clearInterval(timerRef.current);
        endGame();
      }
    }, 100);

    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [phase, currentNoteIndex, notes]);

  const endGame = useCallback(async () => {
    setPhase('results');
    const totalNotes = notes.length;
    const hitRate = totalNotes > 0 ? (perfect + good) / totalNotes : 0;
    const coinReward = score + Math.floor(hitRate * 20);
    const xpReward = Math.floor(score / 2);

    addCoins(coinReward);
    addXP(xpReward);
    addStars(Math.floor(score / 20));
    addDanceStars(Math.floor(hitRate * 5));
    earnSticker('sticker_dance_star');
    if (hitRate >= 1) earnSticker('sticker_high_score');
    recordMiniGameResult({ gameId: 'dance_party', score, coinsEarned: coinReward, playedAt: Date.now() });
    await saveGame();
  }, [notes, score, perfect, good]);

  const handleTap = (arrow: Arrow) => {
    if (phase !== 'playing' || currentNoteIndex >= notes.length) return;

    const currentNote = notes[currentNoteIndex];
    const elapsed = Date.now() - startTime.current;
    const diff = Math.abs(elapsed - currentNote.targetTime);

    if (currentNote.arrow === arrow && diff < 500) {
      let feedback = '';
      let points = 0;

      if (diff < 150) {
        feedback = 'Perfect! ✨';
        points = 10;
        setPerfect((prev) => prev + 1);
      } else if (diff < 300) {
        feedback = 'Good! 👍';
        points = 5;
        setGood((prev) => prev + 1);
      } else {
        feedback = 'OK 👌';
        points = 2;
        setGood((prev) => prev + 1);
      }

      const newCombo = combo + 1;
      setCombo(newCombo);
      setMaxCombo((prev) => Math.max(prev, newCombo));
      setScore((prev) => prev + points * (1 + Math.floor(newCombo / 5)));
      setShowFeedback(feedback);
      setTimeout(() => setShowFeedback(''), 400);

      setNotes((prev) => {
        const updated = [...prev];
        updated[currentNoteIndex] = { ...updated[currentNoteIndex], hit: true };
        return updated;
      });
      setCurrentNoteIndex((prev) => prev + 1);
    }
  };

  if (phase === 'menu') {
    return (
      <ScreenWrapper title="Dance Party" emoji="💃" bgColor={Colors.coralLight}>
        <View style={styles.menuArea}>
          <Text style={styles.menuEmoji}>💃</Text>
          <Text style={styles.menuTitle}>Dance Party!</Text>
          <Text style={styles.menuDesc}>Tap the arrows to the rhythm!</Text>

          <View style={styles.instructions}>
            <Text style={styles.instructionText}>⬆️ ⬇️ ⬅️ ➡️</Text>
            <Text style={styles.instructionDesc}>
              When you see an arrow, tap the matching button!{'\n'}
              Time it perfectly for bonus points!
            </Text>
          </View>

          <GameButton title="Dance! 🎵" onPress={startGame} variant="primary" size="large" style={{ marginTop: 20 }} />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'countdown') {
    return (
      <View style={styles.countdownScreen}>
        <Text style={styles.countdownText}>{countdown > 0 ? countdown : 'GO!'}</Text>
      </View>
    );
  }

  if (phase === 'results') {
    return (
      <ScreenWrapper title="Results" emoji="🏆" bgColor={Colors.coralLight} showBack={false}>
        <View style={styles.resultsArea}>
          <Text style={styles.resultsEmoji}>🏆</Text>
          <Text style={styles.resultsTitle}>{getRandomEncouragement()}</Text>
          <Text style={styles.resultsScore}>Score: {score}</Text>

          <View style={styles.statsGrid}>
            <View style={styles.statItem}><Text style={styles.statValue}>{perfect}</Text><Text style={styles.statLabel}>Perfect ✨</Text></View>
            <View style={styles.statItem}><Text style={styles.statValue}>{good}</Text><Text style={styles.statLabel}>Good 👍</Text></View>
            <View style={styles.statItem}><Text style={styles.statValue}>{miss}</Text><Text style={styles.statLabel}>Miss 😢</Text></View>
            <View style={styles.statItem}><Text style={styles.statValue}>{maxCombo}</Text><Text style={styles.statLabel}>Max Combo 🔥</Text></View>
          </View>

          <GameButton title="Dance Again" emoji="🎵" onPress={startGame} variant="primary" size="large" style={{ marginTop: 16 }} />
          <GameButton title="Back" emoji="🏠" onPress={() => router.back()} variant="outline" size="medium" style={{ marginTop: 8 }} />
        </View>
      </ScreenWrapper>
    );
  }

  // Playing
  const currentNote = currentNoteIndex < notes.length ? notes[currentNoteIndex] : null;

  return (
    <View style={styles.gameScreen}>
      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudText}>🏁 {score}</Text>
        <Text style={styles.hudText}>🔥 {combo}</Text>
      </View>

      {/* Current Arrow */}
      <View style={styles.arrowArea}>
        {currentNote && (
          <Text style={styles.bigArrow}>{currentNote.arrow}</Text>
        )}
        {showFeedback !== '' && (
          <Text style={styles.feedback}>{showFeedback}</Text>
        )}
      </View>

      {/* Tap Buttons */}
      <View style={styles.buttonGrid}>
        <View style={styles.buttonRow}>
          <TouchableOpacity style={styles.arrowBtn} onPress={() => handleTap('⬆️')}>
            <Text style={styles.arrowBtnText}>⬆️</Text>
          </TouchableOpacity>
        </View>
        <View style={styles.buttonRow}>
          <TouchableOpacity style={styles.arrowBtn} onPress={() => handleTap('⬅️')}>
            <Text style={styles.arrowBtnText}>⬅️</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.arrowBtn} onPress={() => handleTap('⬇️')}>
            <Text style={styles.arrowBtnText}>⬇️</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.arrowBtn} onPress={() => handleTap('➡️')}>
            <Text style={styles.arrowBtnText}>➡️</Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  menuArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  menuEmoji: { fontSize: 64 },
  menuTitle: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  menuDesc: { fontSize: 16, color: Colors.gray500, marginTop: 4 },
  instructions: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 20, width: '100%',
    alignItems: 'center', borderWidth: 2, borderColor: Colors.coralLight,
  },
  instructionText: { fontSize: 28, letterSpacing: 8 },
  instructionDesc: { fontSize: 14, color: Colors.gray500, textAlign: 'center', marginTop: 8 },
  countdownScreen: {
    flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.coralLight,
  },
  countdownText: { fontSize: 80, fontWeight: '800', color: Colors.white },
  gameScreen: { flex: 1, backgroundColor: Colors.coralLight, paddingTop: 50 },
  hud: {
    flexDirection: 'row', justifyContent: 'space-around', paddingHorizontal: 20, paddingVertical: 8,
    backgroundColor: Colors.overlay, borderRadius: 16, marginHorizontal: 16,
  },
  hudText: { fontSize: 18, fontWeight: '800', color: Colors.white },
  arrowArea: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  bigArrow: { fontSize: 120 },
  feedback: { fontSize: 24, fontWeight: '800', color: Colors.white, marginTop: 12 },
  buttonGrid: { paddingBottom: 40, alignItems: 'center' },
  buttonRow: { flexDirection: 'row', gap: 12, marginVertical: 6 },
  arrowBtn: {
    width: 80, height: 80, borderRadius: 20, backgroundColor: Colors.white,
    justifyContent: 'center', alignItems: 'center',
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.2, shadowRadius: 4, elevation: 4,
  },
  arrowBtnText: { fontSize: 36 },
  resultsArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 20 },
  resultsEmoji: { fontSize: 64 },
  resultsTitle: { fontSize: 22, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  resultsScore: { fontSize: 32, fontWeight: '800', color: Colors.coral, marginTop: 8 },
  statsGrid: {
    flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginTop: 20, justifyContent: 'center',
  },
  statItem: {
    backgroundColor: Colors.white, padding: 12, borderRadius: 14, width: '45%', alignItems: 'center',
  },
  statValue: { fontSize: 28, fontWeight: '800', color: Colors.dark },
  statLabel: { fontSize: 12, color: Colors.gray500, marginTop: 4 },
});
