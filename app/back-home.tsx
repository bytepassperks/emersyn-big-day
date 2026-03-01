/**
 * Back Home - Post-outing routine (shoes off, wash hands, face wash, change, water)
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Dimensions } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { getRandomEncouragement } from '@/lib/helpers';

const { width: SW } = Dimensions.get('window');

type Step = { id: string; name: string; emoji: string; stat: Record<string, number>; coins: number; xp: number };

const STEPS: Step[] = [
  { id: 'shoes', name: 'Take off shoes', emoji: '\uD83D\uDC5F', stat: { cleanliness: 5 }, coins: 2, xp: 3 },
  { id: 'hands', name: 'Wash hands', emoji: '\uD83D\uDE4C', stat: { cleanliness: 15 }, coins: 3, xp: 5 },
  { id: 'face', name: 'Wash face', emoji: '\uD83E\uDDD1', stat: { cleanliness: 15 }, coins: 3, xp: 5 },
  { id: 'change', name: 'Change clothes', emoji: '\uD83D\uDC57', stat: { cleanliness: 10 }, coins: 3, xp: 5 },
  { id: 'water', name: 'Drink water', emoji: '\uD83D\uDCA7', stat: { energy: 10 }, coins: 2, xp: 3 },
];

export default function BackHome() {
  const { updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [currentStep, setCurrentStep] = useState(0);
  const [completed, setCompleted] = useState(false);
  const [totalCoins, setTotalCoins] = useState(0);

  const handleStep = useCallback(async () => {
    const step = STEPS[currentStep];
    updateStats(step.stat);
    addCoins(step.coins);
    addXP(step.xp);
    setTotalCoins(prev => prev + step.coins);

    if (currentStep < STEPS.length - 1) {
      setCurrentStep(prev => prev + 1);
    } else {
      addStars(1);
      earnSticker('sticker_tidy_emersyn');
      setCompleted(true);
      await saveGame();
    }
  }, [currentStep, updateStats, addCoins, addXP, addStars, earnSticker, saveGame]);

  if (completed) {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.doneEmoji}>{'\u2728'}</Text>
          <Text style={styles.doneTitle}>All Clean!</Text>
          <Text style={styles.doneMsg}>{getRandomEncouragement()}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>+{'\u20B9'}{totalCoins} coins</Text>
            <Text style={styles.rewardLine}>+1 Star</Text>
            <Text style={styles.rewardLine}>Tidy Emersyn sticker!</Text>
          </View>
          <TouchableOpacity style={styles.homeBtn} onPress={() => router.replace('/')}>
            <Text style={styles.homeBtnText}>Back Home</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  const step = STEPS[currentStep];

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>{'\u2190'}</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Back Home Routine</Text>
      </View>
      <View style={styles.center}>
        <Text style={styles.stepEmoji}>{step.emoji}</Text>
        <Text style={styles.stepName}>{step.name}</Text>
        <Text style={styles.stepCount}>Step {currentStep + 1} of {STEPS.length}</Text>
        <View style={styles.dotsRow}>
          {STEPS.map((_, i) => (
            <View key={i} style={[styles.dot, i <= currentStep && styles.dotActive]} />
          ))}
        </View>
        <TouchableOpacity style={styles.doBtn} onPress={handleStep}>
          <Text style={styles.doBtnText}>
            {currentStep < STEPS.length - 1 ? 'Done! Next' : 'All Done!'}
          </Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#E4F0F8' },
  header: { flexDirection: 'row', alignItems: 'center', padding: 16, paddingTop: 50, gap: 12 },
  backBtn: { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fff', alignItems: 'center', justifyContent: 'center' },
  backBtnText: { fontSize: 20, fontWeight: '800' },
  title: { fontSize: 20, fontWeight: '800', color: '#333' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  stepEmoji: { fontSize: 80 },
  stepName: { fontSize: 26, fontWeight: '800', color: '#333', marginTop: 16 },
  stepCount: { fontSize: 14, color: '#999', marginTop: 8 },
  dotsRow: { flexDirection: 'row', gap: 10, marginTop: 16 },
  dot: { width: 14, height: 14, borderRadius: 7, backgroundColor: '#ddd' },
  dotActive: { backgroundColor: '#5ca7d8' },
  doBtn: { marginTop: 30, backgroundColor: '#5ca7d8', paddingHorizontal: 36, paddingVertical: 16, borderRadius: 18 },
  doBtnText: { fontSize: 20, fontWeight: '800', color: '#fff' },
  doneEmoji: { fontSize: 80 },
  doneTitle: { fontSize: 30, fontWeight: '800', color: '#333', marginTop: 12 },
  doneMsg: { fontSize: 15, color: '#999', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 20, width: '100%', gap: 8 },
  rewardLine: { fontSize: 18, fontWeight: '700', color: '#333' },
  homeBtn: { marginTop: 24, backgroundColor: '#5ca7d8', paddingHorizontal: 30, paddingVertical: 14, borderRadius: 16 },
  homeBtnText: { fontSize: 18, fontWeight: '800', color: '#fff' },
});
