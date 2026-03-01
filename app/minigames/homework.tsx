import React, { useState, useMemo, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getRandomEncouragement } from '@/lib/helpers';

type GameType = 'shapes' | 'counting' | 'patterns';

interface Question {
  question: string;
  options: string[];
  correctIndex: number;
  emoji: string;
}

const generateShapeQuestion = (): Question => {
  const shapes = [
    { name: 'Circle', emoji: '🔵' },
    { name: 'Square', emoji: '🟥' },
    { name: 'Triangle', emoji: '🔺' },
    { name: 'Star', emoji: '⭐' },
    { name: 'Heart', emoji: '💖' },
    { name: 'Diamond', emoji: '💎' },
  ];
  const correct = shapes[Math.floor(Math.random() * shapes.length)];
  const others = shapes.filter((s) => s.name !== correct.name);
  const shuffled = [...others].sort(() => Math.random() - 0.5).slice(0, 3);
  const options = [...shuffled.map((s) => s.name), correct.name].sort(() => Math.random() - 0.5);
  return {
    question: `What shape is this? ${correct.emoji}`,
    options,
    correctIndex: options.indexOf(correct.name),
    emoji: correct.emoji,
  };
};

const generateCountingQuestion = (): Question => {
  const emojis = ['🍎', '🌟', '🎈', '🦋', '🌸', '🍕'];
  const emoji = emojis[Math.floor(Math.random() * emojis.length)];
  const count = Math.floor(Math.random() * 8) + 2;
  const display = emoji.repeat(count);
  const options = [count, count + 1, count - 1, count + 2]
    .filter((n) => n > 0)
    .slice(0, 4)
    .map(String)
    .sort(() => Math.random() - 0.5);
  return {
    question: `How many? ${display}`,
    options,
    correctIndex: options.indexOf(String(count)),
    emoji,
  };
};

const generatePatternQuestion = (): Question => {
  const patterns = [
    { seq: ['🔴', '🔵', '🔴', '🔵'], answer: '🔴', options: ['🔴', '🔵', '🟢', '🟡'] },
    { seq: ['⭐', '🌙', '⭐', '🌙'], answer: '⭐', options: ['⭐', '🌙', '☀️', '🌈'] },
    { seq: ['🐱', '🐶', '🐱', '🐶'], answer: '🐱', options: ['🐱', '🐶', '🐰', '🐻'] },
    { seq: ['🍎', '🍊', '🍎', '🍊'], answer: '🍎', options: ['🍎', '🍊', '🍇', '🍌'] },
    { seq: ['💖', '💜', '💖', '💜'], answer: '💖', options: ['💖', '💜', '💚', '💛'] },
  ];
  const pattern = patterns[Math.floor(Math.random() * patterns.length)];
  const options = pattern.options.sort(() => Math.random() - 0.5);
  return {
    question: `What comes next? ${pattern.seq.join(' ')} ❓`,
    options,
    correctIndex: options.indexOf(pattern.answer),
    emoji: '🧩',
  };
};

export default function Homework() {
  const router = useRouter();
  const { addCoins, addXP, addStars, earnSticker, recordMiniGameResult, saveGame } = useGameStore();
  const [gameType, setGameType] = useState<GameType | null>(null);
  const [questionNum, setQuestionNum] = useState(0);
  const [score, setScore] = useState(0);
  const [question, setQuestion] = useState<Question | null>(null);
  const [answered, setAnswered] = useState(false);
  const [selectedAnswer, setSelectedAnswer] = useState(-1);
  const totalQuestions = 5;

  const startGame = useCallback((type: GameType) => {
    setGameType(type);
    setQuestionNum(0);
    setScore(0);
    setAnswered(false);
    setSelectedAnswer(-1);
    generateQuestion(type);
  }, []);

  const generateQuestion = (type: GameType) => {
    switch (type) {
      case 'shapes': setQuestion(generateShapeQuestion()); break;
      case 'counting': setQuestion(generateCountingQuestion()); break;
      case 'patterns': setQuestion(generatePatternQuestion()); break;
    }
  };

  const handleAnswer = (index: number) => {
    if (answered || !question) return;
    setSelectedAnswer(index);
    setAnswered(true);

    if (index === question.correctIndex) {
      setScore((prev) => prev + 1);
    }
  };

  const nextQuestion = async () => {
    if (questionNum >= totalQuestions - 1) {
      // Game over
      const coinReward = score * 5;
      const xpReward = score * 8;
      addCoins(coinReward);
      addXP(xpReward);
      addStars(Math.floor(score / 2));
      earnSticker('sticker_homework_5');
      if (score === totalQuestions) earnSticker('sticker_perfect_score');
      recordMiniGameResult({ gameId: `homework_${gameType}`, score, coinsEarned: coinReward, playedAt: Date.now() });
      await saveGame();
      Alert.alert(
        score === totalQuestions ? 'Perfect Score! 🏆' : 'Well Done! 🌟',
        `You got ${score}/${totalQuestions} correct!\n+₹${coinReward} coins · +${xpReward} XP`,
        [{ text: 'OK', onPress: () => { setGameType(null); } }]
      );
      return;
    }

    setQuestionNum((prev) => prev + 1);
    setAnswered(false);
    setSelectedAnswer(-1);
    generateQuestion(gameType!);
  };

  if (!gameType) {
    return (
      <ScreenWrapper title="Homework" emoji="📚" bgColor={Colors.purpleLight}>
        <View style={styles.menuArea}>
          <Text style={styles.menuEmoji}>📚</Text>
          <Text style={styles.menuTitle}>Choose a subject!</Text>
          <GameButton title="Shape Sorting" emoji="🔷" onPress={() => startGame('shapes')} variant="primary" size="large" style={styles.menuBtn} />
          <GameButton title="Counting Fun" emoji="🔢" onPress={() => startGame('counting')} variant="secondary" size="large" style={styles.menuBtn} />
          <GameButton title="Pattern Match" emoji="🧩" onPress={() => startGame('patterns')} variant="accent" size="large" style={styles.menuBtn} />
        </View>
      </ScreenWrapper>
    );
  }

  if (!question) return null;

  return (
    <ScreenWrapper title="Homework" emoji="📚" bgColor={Colors.purpleLight} showBack={false} scrollable={false}>
      <View style={styles.gameArea}>
        <Text style={styles.progress}>Question {questionNum + 1} / {totalQuestions}</Text>
        <View style={styles.scoreRow}>
          <Text style={styles.scoreText}>⭐ Score: {score}</Text>
        </View>

        <View style={styles.questionCard}>
          <Text style={styles.questionText}>{question.question}</Text>
        </View>

        <View style={styles.optionsGrid}>
          {question.options.map((option, i) => {
            const isCorrect = i === question.correctIndex;
            const isSelected = i === selectedAnswer;
            let bgColor = Colors.white;
            if (answered && isCorrect) bgColor = Colors.mintLight;
            if (answered && isSelected && !isCorrect) bgColor = Colors.coralLight;

            return (
              <TouchableOpacity
                key={i}
                style={[styles.optionBtn, { backgroundColor: bgColor }]}
                onPress={() => handleAnswer(i)}
                disabled={answered}
              >
                <Text style={styles.optionText}>{option}</Text>
              </TouchableOpacity>
            );
          })}
        </View>

        {answered && (
          <View style={styles.feedbackArea}>
            <Text style={styles.feedbackText}>
              {selectedAnswer === question.correctIndex
                ? getRandomEncouragement()
                : `The answer was: ${question.options[question.correctIndex]}`}
            </Text>
            <GameButton
              title={questionNum >= totalQuestions - 1 ? 'See Results!' : 'Next →'}
              onPress={nextQuestion}
              variant="primary"
              size="medium"
              style={{ marginTop: 12 }}
            />
          </View>
        )}
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  menuArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 40 },
  menuEmoji: { fontSize: 64 },
  menuTitle: { fontSize: 24, fontWeight: '800', color: Colors.dark, marginVertical: 16 },
  menuBtn: { width: '100%', marginVertical: 6 },
  gameArea: { flex: 1, paddingHorizontal: 20, paddingTop: 16 },
  progress: { fontSize: 14, fontWeight: '600', color: Colors.gray500, textAlign: 'center' },
  scoreRow: { alignItems: 'center', marginVertical: 8 },
  scoreText: { fontSize: 18, fontWeight: '800', color: Colors.purple },
  questionCard: {
    backgroundColor: Colors.white, padding: 24, borderRadius: 20, marginVertical: 16,
    alignItems: 'center', borderWidth: 2, borderColor: Colors.purpleLight,
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.1, shadowRadius: 6, elevation: 4,
  },
  questionText: { fontSize: 22, fontWeight: '700', color: Colors.dark, textAlign: 'center' },
  optionsGrid: { gap: 10 },
  optionBtn: {
    padding: 16, borderRadius: 16, borderWidth: 2, borderColor: Colors.purpleLight,
    alignItems: 'center',
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 2, elevation: 1,
  },
  optionText: { fontSize: 20, fontWeight: '700', color: Colors.dark },
  feedbackArea: { alignItems: 'center', marginTop: 16 },
  feedbackText: { fontSize: 16, fontWeight: '700', color: Colors.dark, textAlign: 'center' },
});
