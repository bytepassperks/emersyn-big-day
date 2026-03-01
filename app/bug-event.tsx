/**
 * Bug Event - Gentle, empowering bug encounter
 */
import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Animated } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { getRandomEncouragement } from '@/lib/helpers';

type BugType = { id: string; name: string; emoji: string };

const BUGS: BugType[] = [
  { id: 'mosquito', name: 'Mosquito', emoji: '\uD83E\uDD9F' },
  { id: 'fly', name: 'House Fly', emoji: '\uD83E\uDEB0' },
  { id: 'spider', name: 'Tiny Spider', emoji: '\uD83D\uDD77\uFE0F' },
  { id: 'ant', name: 'Ant', emoji: '\uD83D\uDC1C' },
  { id: 'ladybug', name: 'Ladybug', emoji: '\uD83D\uDC1E' },
];

type Action = { id: string; name: string; emoji: string; stat: Record<string, number>; coins: number };

const ACTIONS: Action[] = [
  { id: 'window', name: 'Close Window', emoji: '\uD83E\uDE9F', stat: { cleanliness: 5 }, coins: 3 },
  { id: 'fan', name: 'Turn on Fan', emoji: '\uD83C\uDF2C\uFE0F', stat: { energy: 5 }, coins: 3 },
  { id: 'net', name: 'Place Net', emoji: '\uD83E\uDD4F', stat: { cleanliness: 8 }, coins: 4 },
  { id: 'shoo', name: 'Shoo Bug', emoji: '\uD83D\uDC4B', stat: { fun: 5 }, coins: 5 },
];

export default function BugEvent() {
  const { updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [bug] = useState(() => BUGS[Math.floor(Math.random() * BUGS.length)]);
  const [actionsUsed, setActionsUsed] = useState<string[]>([]);
  const [phase, setPhase] = useState<'encounter' | 'done'>('encounter');
  const [totalCoins, setTotalCoins] = useState(0);
  const [bounceAnim] = useState(() => new Animated.Value(0));

  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(bounceAnim, { toValue: -20, duration: 500, useNativeDriver: true }),
        Animated.timing(bounceAnim, { toValue: 0, duration: 500, useNativeDriver: true }),
      ])
    );
    loop.start();
    return () => loop.stop();
  }, [bounceAnim]);

  const handleAction = useCallback(async (action: Action) => {
    if (actionsUsed.includes(action.id)) return;
    updateStats(action.stat);
    addCoins(action.coins);
    addXP(5);
    setTotalCoins(prev => prev + action.coins);
    setActionsUsed(prev => [...prev, action.id]);

    if (actionsUsed.length >= 2) {
      addStars(1);
      earnSticker('sticker_brave_badge');
      setPhase('done');
      await saveGame();
    }
  }, [actionsUsed, updateStats, addCoins, addXP, addStars, earnSticker, saveGame]);

  if (phase === 'done') {
    return (
      <View style={styles.container}>
        <View style={styles.center}>
          <Text style={styles.doneEmoji}>{'\uD83C\uDFC5'}</Text>
          <Text style={styles.doneTitle}>Brave Badge Earned!</Text>
          <Text style={styles.doneMsg}>{getRandomEncouragement()}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>+{'\u20B9'}{totalCoins} coins</Text>
            <Text style={styles.rewardLine}>+1 Star</Text>
            <Text style={styles.rewardLine}>Brave Badge sticker!</Text>
          </View>
          <TouchableOpacity style={styles.homeBtn} onPress={() => router.replace('/')}>
            <Text style={styles.homeBtnText}>Back Home</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>{'\u2190'}</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Bug Alert!</Text>
      </View>
      <View style={styles.center}>
        <Animated.Text style={[styles.bugEmoji, { transform: [{ translateY: bounceAnim }] }]}>
          {bug.emoji}
        </Animated.Text>
        <Text style={styles.bugName}>A {bug.name} appeared!</Text>
        <Text style={styles.bugHint}>Use actions to handle it bravely!</Text>
        <View style={styles.actionGrid}>
          {ACTIONS.map(action => {
            const used = actionsUsed.includes(action.id);
            return (
              <TouchableOpacity
                key={action.id}
                style={[styles.actionBtn, used && styles.actionUsed]}
                onPress={() => handleAction(action)}
                disabled={used}
              >
                <Text style={styles.actionEmoji}>{action.emoji}</Text>
                <Text style={styles.actionName}>{action.name}</Text>
                {!used && <Text style={styles.actionCoins}>+{'\u20B9'}{action.coins}</Text>}
                {used && <Text style={styles.actionDone}>Done!</Text>}
              </TouchableOpacity>
            );
          })}
        </View>
        <Text style={styles.progressText}>{actionsUsed.length}/3 actions to earn Brave Badge</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF0E0' },
  header: { flexDirection: 'row', alignItems: 'center', padding: 16, paddingTop: 50, gap: 12 },
  backBtn: { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fff', alignItems: 'center', justifyContent: 'center' },
  backBtnText: { fontSize: 20, fontWeight: '800' },
  title: { fontSize: 22, fontWeight: '800', color: '#333' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  bugEmoji: { fontSize: 80 },
  bugName: { fontSize: 24, fontWeight: '800', color: '#333', marginTop: 16 },
  bugHint: { fontSize: 14, color: '#999', marginTop: 6 },
  actionGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginTop: 24, justifyContent: 'center' },
  actionBtn: { width: 140, backgroundColor: '#fff', padding: 16, borderRadius: 16, alignItems: 'center' },
  actionUsed: { opacity: 0.5 },
  actionEmoji: { fontSize: 32 },
  actionName: { fontSize: 14, fontWeight: '700', color: '#333', marginTop: 6 },
  actionCoins: { fontSize: 13, fontWeight: '800', color: '#ff9f43', marginTop: 4 },
  actionDone: { fontSize: 13, fontWeight: '800', color: '#10b981', marginTop: 4 },
  progressText: { fontSize: 13, color: '#999', marginTop: 16 },
  doneEmoji: { fontSize: 80 },
  doneTitle: { fontSize: 28, fontWeight: '800', color: '#333', marginTop: 12 },
  doneMsg: { fontSize: 15, color: '#999', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 20, width: '100%', gap: 8 },
  rewardLine: { fontSize: 18, fontWeight: '700', color: '#333' },
  homeBtn: { marginTop: 24, backgroundColor: '#ff9f43', paddingHorizontal: 30, paddingVertical: 14, borderRadius: 16 },
  homeBtnText: { fontSize: 18, fontWeight: '800', color: '#fff' },
});
