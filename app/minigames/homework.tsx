/**
 * Brain Puzzles - Math and word puzzles with difficulty scaling
 */
import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Dimensions } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

const { width: SW } = Dimensions.get('window');

type PuzzleType = 'math' | 'pattern' | 'emoji';

type Puzzle = {
  question: string;
  options: string[];
  correctIdx: number;
  type: PuzzleType;
};

const generateMathPuzzle = (difficulty: number): Puzzle => {
  const max = difficulty * 5 + 10;
  const a = Math.floor(Math.random() * max) + 1;
  const b = Math.floor(Math.random() * max) + 1;
  const ops = difficulty > 3 ? ['+', '-', '*'] : ['+', '-'];
  const op = ops[Math.floor(Math.random() * ops.length)];

  let answer: number;
  switch (op) {
    case '+': answer = a + b; break;
    case '-': answer = Math.max(a, b) - Math.min(a, b); break;
    case '*': answer = a * b; break;
    default: answer = a + b;
  }

  const question = op === '-' ? `${Math.max(a, b)} ${op} ${Math.min(a, b)} = ?` : `${a} ${op} ${b} = ?`;
  const correctIdx = Math.floor(Math.random() * 4);
  const options = Array.from({ length: 4 }, (_, i) => {
    if (i === correctIdx) return String(answer);
    let wrong = answer + (Math.floor(Math.random() * 10) - 5);
    while (wrong === answer || wrong < 0) wrong = answer + Math.floor(Math.random() * 10) + 1;
    return String(wrong);
  });

  return { question, options, correctIdx, type: 'math' };
};

const generatePatternPuzzle = (): Puzzle => {
  const patterns = [
    { seq: [2, 4, 6, 8], answer: '10', q: '2, 4, 6, 8, ?' },
    { seq: [1, 3, 5, 7], answer: '9', q: '1, 3, 5, 7, ?' },
    { seq: [3, 6, 9, 12], answer: '15', q: '3, 6, 9, 12, ?' },
    { seq: [5, 10, 15, 20], answer: '25', q: '5, 10, 15, 20, ?' },
    { seq: [1, 4, 9, 16], answer: '25', q: '1, 4, 9, 16, ?' },
    { seq: [2, 6, 12, 20], answer: '30', q: '2, 6, 12, 20, ?' },
    { seq: [1, 1, 2, 3, 5], answer: '8', q: '1, 1, 2, 3, 5, ?' },
  ];
  const p = patterns[Math.floor(Math.random() * patterns.length)];
  const correctIdx = Math.floor(Math.random() * 4);
  const options = Array.from({ length: 4 }, (_, i) => {
    if (i === correctIdx) return p.answer;
    let wrong = parseInt(p.answer) + (Math.floor(Math.random() * 6) - 3);
    while (String(wrong) === p.answer || wrong < 0) wrong = parseInt(p.answer) + Math.floor(Math.random() * 5) + 1;
    return String(wrong);
  });
  return { question: p.q, options, correctIdx, type: 'pattern' };
};

const generateEmojiPuzzle = (): Puzzle => {
  const puzzles = [
    { q: 'Which emoji is a fruit?', opts: ['\uD83C\uDF4E', '\uD83D\uDE97', '\uD83C\uDFE0', '\uD83D\uDCDA'], correct: 0 },
    { q: 'Which emoji is an animal?', opts: ['\uD83C\uDF32', '\uD83D\uDC31', '\uD83C\uDF54', '\u2708\uFE0F'], correct: 1 },
    { q: 'Which emoji is a vehicle?', opts: ['\uD83C\uDF4C', '\uD83C\uDF3B', '\uD83D\uDE8C', '\uD83C\uDF8E'], correct: 2 },
    { q: 'Which is NOT food?', opts: ['\uD83C\uDF55', '\uD83C\uDF54', '\uD83D\uDC36', '\uD83C\uDF69'], correct: 2 },
    { q: 'Which emoji is weather?', opts: ['\uD83C\uDF27\uFE0F', '\uD83D\uDC1F', '\uD83C\uDF81', '\uD83D\uDCBB'], correct: 0 },
  ];
  const p = puzzles[Math.floor(Math.random() * puzzles.length)];
  return { question: p.q, options: p.opts, correctIdx: p.correct, type: 'emoji' };
};

export default function Homework() {
  const { addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [gameState, setGameState] = useState<'ready' | 'playing' | 'over'>('ready');
  const [puzzle, setPuzzle] = useState<Puzzle | null>(null);
  const [round, setRound] = useState(0);
  const [score, setScore] = useState(0);
  const [correct, setCorrect] = useState(0);
  const [streak, setStreak] = useState(0);
  const [maxStreak, setMaxStreak] = useState(0);
  const [selected, setSelected] = useState<number | null>(null);
  const [showResult, setShowResult] = useState(false);
  const totalRounds = 10;

  const nextPuzzle = useCallback((difficulty: number) => {
    const rand = Math.random();
    if (rand < 0.5) return generateMathPuzzle(difficulty);
    if (rand < 0.8) return generatePatternPuzzle();
    return generateEmojiPuzzle();
  }, []);

  const startGame = () => {
    setGameState('playing');
    setScore(0);
    setCorrect(0);
    setStreak(0);
    setMaxStreak(0);
    setRound(1);
    setPuzzle(nextPuzzle(1));
    setSelected(null);
    setShowResult(false);
  };

  const handleAnswer = (idx: number) => {
    if (showResult || !puzzle) return;
    setSelected(idx);
    setShowResult(true);

    if (idx === puzzle.correctIdx) {
      const pts = 100 * (1 + Math.floor(streak / 3));
      setScore(s => s + pts);
      setCorrect(c => c + 1);
      setStreak(s => { const ns = s + 1; setMaxStreak(m => Math.max(m, ns)); return ns; });
    } else {
      setStreak(0);
    }

    setTimeout(() => {
      if (round >= totalRounds) {
        endGame();
      } else {
        setRound(r => r + 1);
        setPuzzle(nextPuzzle(Math.ceil(round / 3)));
        setSelected(null);
        setShowResult(false);
      }
    }, 1000);
  };

  const endGame = useCallback(async () => {
    setGameState('over');
    const coinsEarned = correct * 5 + maxStreak * 3;
    addCoins(coinsEarned);
    addXP(correct * 8);
    if (correct >= 8) addStars(1);
    if (correct === totalRounds) earnSticker('sticker_brainiac');
    await saveGame();
  }, [correct, maxStreak, addCoins, addXP, addStars, earnSticker, saveGame]);

  if (gameState === 'ready') {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.puzzleEmoji}>{'\uD83E\uDDE9'}</Text>
          <Text style={styles.gameTitle}>Brain Puzzles</Text>
          <Text style={styles.hint}>Math, patterns, and emoji puzzles!</Text>
          <TouchableOpacity style={styles.playBtn} onPress={startGame}>
            <Text style={styles.playBtnText}>Start!</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.backLink} onPress={() => router.back()}>
            <Text style={styles.backLinkText}>Back to Arcade</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (gameState === 'over') {
    const coinsEarned = correct * 5 + maxStreak * 3;
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.overTitle}>{correct >= 8 ? 'Brilliant!' : 'Good Try!'}</Text>
          <Text style={styles.finalScore}>{correct}/{totalRounds} correct</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>Score: {score}</Text>
            <Text style={styles.rewardLine}>Max Streak: {maxStreak}</Text>
            <Text style={styles.rewardLine}>{'\u20B9'}{coinsEarned} coins</Text>
            <Text style={styles.rewardLine}>+{correct * 8} XP</Text>
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

  if (!puzzle) return null;

  return (
    <View style={styles.container}>
      {/* HUD */}
      <View style={styles.hud}>
        <Text style={styles.hudRound}>{round}/{totalRounds}</Text>
        <Text style={styles.hudScore}>{score}</Text>
        {streak > 1 && <Text style={styles.hudStreak}>{'\uD83D\uDD25'} {streak}</Text>}
      </View>

      {/* Question */}
      <View style={styles.questionArea}>
        <View style={styles.typeBadge}>
          <Text style={styles.typeText}>
            {puzzle.type === 'math' ? 'Math' : puzzle.type === 'pattern' ? 'Pattern' : 'Emoji'}
          </Text>
        </View>
        <Text style={styles.questionText}>{puzzle.question}</Text>
      </View>

      {/* Options */}
      <View style={styles.optionsGrid}>
        {puzzle.options.map((opt, i) => {
          let bg = '#fff';
          if (showResult && i === puzzle.correctIdx) bg = '#D4EDDA';
          else if (showResult && i === selected && i !== puzzle.correctIdx) bg = '#F8D7DA';
          else if (selected === i) bg = '#E8E8E8';

          return (
            <TouchableOpacity
              key={i}
              style={[styles.optionBtn, { backgroundColor: bg }]}
              onPress={() => handleAnswer(i)}
              disabled={showResult}
            >
              <Text style={styles.optionText}>{opt}</Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#E8F0FF' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  puzzleEmoji: { fontSize: 80 },
  gameTitle: { fontSize: 32, fontWeight: '800', color: '#333', marginTop: 12 },
  hint: { fontSize: 14, color: '#999', marginTop: 12 },
  playBtn: { marginTop: 30, backgroundColor: '#3B82F6', paddingHorizontal: 40, paddingVertical: 16, borderRadius: 20 },
  playBtnText: { fontSize: 22, fontWeight: '800', color: '#fff' },
  backLink: { marginTop: 16 },
  backLinkText: { fontSize: 14, fontWeight: '700', color: '#999' },
  hud: { flexDirection: 'row', justifyContent: 'space-between', paddingHorizontal: 16, paddingTop: 50 },
  hudRound: { fontSize: 18, fontWeight: '800', color: '#666' },
  hudScore: { fontSize: 24, fontWeight: '800', color: '#333' },
  hudStreak: { fontSize: 20, fontWeight: '800', color: '#F59E0B' },
  questionArea: { alignItems: 'center', paddingVertical: 40, paddingHorizontal: 20 },
  typeBadge: { backgroundColor: '#3B82F6', paddingHorizontal: 14, paddingVertical: 4, borderRadius: 10 },
  typeText: { fontSize: 13, fontWeight: '800', color: '#fff' },
  questionText: { fontSize: 28, fontWeight: '800', color: '#333', marginTop: 16, textAlign: 'center' },
  optionsGrid: { paddingHorizontal: 20, gap: 12 },
  optionBtn: {
    padding: 20, borderRadius: 18, alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 2,
  },
  optionText: { fontSize: 22, fontWeight: '800', color: '#333' },
  overTitle: { fontSize: 32, fontWeight: '800', color: '#3B82F6' },
  finalScore: { fontSize: 22, fontWeight: '800', color: '#333', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 16, width: '100%', gap: 8 },
  rewardLine: { fontSize: 16, fontWeight: '700', color: '#333' },
});
