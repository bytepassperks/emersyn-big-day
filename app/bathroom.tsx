import React, { useState } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { ActivityButton } from '@/components/ActivityButton';
import { Colors } from '@/lib/colors';
import { getRandomEncouragement } from '@/lib/helpers';

const bathroomActivities = [
  { id: 'brush_teeth_b', name: 'Brush Teeth', emoji: '🪥', statDeltas: { cleanliness: 15 }, coinReward: 3, xpReward: 5, starReward: 0 },
  { id: 'wash_face_b', name: 'Wash Face', emoji: '🧼', statDeltas: { cleanliness: 10 }, coinReward: 2, xpReward: 3, starReward: 0 },
  { id: 'wash_hands_b', name: 'Wash Hands', emoji: '🖐️', statDeltas: { cleanliness: 8 }, coinReward: 2, xpReward: 3, starReward: 0 },
  { id: 'bath_bubbles', name: 'Bubble Bath', emoji: '🛁', statDeltas: { cleanliness: 30, fun: 10 }, coinReward: 5, xpReward: 8, starReward: 1 },
  { id: 'hair_wash', name: 'Wash Hair', emoji: '💇', statDeltas: { cleanliness: 12 }, coinReward: 3, xpReward: 5, starReward: 0 },
  { id: 'dry_off', name: 'Dry Off', emoji: '🧖', statDeltas: { cleanliness: 5 }, coinReward: 2, xpReward: 3, starReward: 0 },
];

export default function Bathroom() {
  const { updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [completedActivities, setCompletedActivities] = useState<string[]>([]);

  const handleActivity = async (act: typeof bathroomActivities[0]) => {
    if (completedActivities.includes(act.id)) return;

    updateStats(act.statDeltas);
    addCoins(act.coinReward);
    addXP(act.xpReward);
    if (act.starReward > 0) addStars(act.starReward);

    setCompletedActivities((prev) => [...prev, act.id]);

    if (act.id === 'brush_teeth_b') earnSticker('sticker_brush_teeth');
    if (act.id === 'bath_bubbles') earnSticker('sticker_bath_time');

    await saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${act.coinReward} coins!`);
  };

  return (
    <ScreenWrapper title="Bathroom" emoji="🛁" bgColor={Colors.skyLight}>
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>🚿</Text>
        <Text style={styles.roomDesc}>Time to get squeaky clean!</Text>
      </View>

      <Text style={styles.sectionTitle}>🧼 Activities</Text>
      {bathroomActivities.map((act) => (
        <ActivityButton
          key={act.id}
          activity={act as any}
          completed={completedActivities.includes(act.id)}
          onPress={() => handleActivity(act)}
        />
      ))}

      <View style={styles.tip}>
        <Text style={styles.tipEmoji}>💡</Text>
        <Text style={styles.tipText}>Keeping clean boosts your Cleanliness meter!</Text>
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  characterArea: {
    alignItems: 'center',
    paddingVertical: 20,
  },
  characterEmoji: {
    fontSize: 72,
  },
  roomDesc: {
    fontSize: 14,
    color: Colors.gray500,
    marginTop: 8,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: Colors.dark,
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8,
  },
  tip: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.yellowLight,
    marginHorizontal: 16,
    marginTop: 16,
    padding: 12,
    borderRadius: 14,
    gap: 8,
  },
  tipEmoji: {
    fontSize: 20,
  },
  tipText: {
    fontSize: 13,
    color: Colors.gray500,
    flex: 1,
  },
});
