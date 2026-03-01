import React, { useState } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { ActivityButton } from '@/components/ActivityButton';
import { Colors } from '@/lib/colors';
import { getActivitiesBySegment } from '@/content/activities';
import { getRandomEncouragement } from '@/lib/helpers';

export default function Bedroom() {
  const { stats, updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [completedActivities, setCompletedActivities] = useState<string[]>([]);

  const morningActivities = getActivitiesBySegment('morning').filter(
    (a) => ['wake_up', 'make_bed', 'get_dressed'].includes(a.id)
  );
  const bedtimeActivities = getActivitiesBySegment('bedtime');

  const handleActivity = async (activityId: string, statDeltas: Record<string, number>, coinReward: number, xpReward: number, starReward: number) => {
    if (completedActivities.includes(activityId)) return;

    updateStats(statDeltas);
    addCoins(coinReward);
    addXP(xpReward);
    if (starReward > 0) addStars(starReward);

    setCompletedActivities((prev) => [...prev, activityId]);

    // Sticker drops
    if (activityId === 'wake_up') earnSticker('sticker_first_wake');
    if (activityId === 'go_sleep') earnSticker('sticker_bedtime');
    if (activityId === 'bath_time') earnSticker('sticker_bath_time');

    await saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${coinReward} coins!`);
  };

  return (
    <ScreenWrapper title="Bedroom" emoji="🛏️" bgColor={Colors.bgBedroom}>
      {/* Character */}
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>👧</Text>
        <Text style={styles.roomDesc}>Emersyn's cozy bedroom</Text>
      </View>

      {/* Morning Activities */}
      <Text style={styles.sectionTitle}>🌅 Morning</Text>
      {morningActivities.map((activity) => (
        <ActivityButton
          key={activity.id}
          activity={activity}
          completed={completedActivities.includes(activity.id)}
          onPress={() =>
            handleActivity(
              activity.id,
              activity.statDeltas,
              activity.coinReward,
              activity.xpReward,
              activity.starReward
            )
          }
        />
      ))}

      {/* Bedtime Activities */}
      <Text style={styles.sectionTitle}>🌙 Bedtime</Text>
      {bedtimeActivities.map((activity) => (
        <ActivityButton
          key={activity.id}
          activity={activity}
          completed={completedActivities.includes(activity.id)}
          onPress={() =>
            handleActivity(
              activity.id,
              activity.statDeltas,
              activity.coinReward,
              activity.xpReward,
              activity.starReward
            )
          }
        />
      ))}

      {/* Room Decor Preview */}
      <View style={styles.decorSection}>
        <Text style={styles.sectionTitle}>🏠 Room Decor</Text>
        <View style={styles.decorGrid}>
          {['🛏️', '💡', '🖼️', '🧸', '🌈', '⭐'].map((emoji, i) => (
            <View key={i} style={styles.decorSlot}>
              <Text style={styles.decorEmoji}>{emoji}</Text>
            </View>
          ))}
        </View>
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
  decorSection: {
    marginTop: 8,
  },
  decorGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: 16,
    gap: 10,
  },
  decorSlot: {
    width: 60,
    height: 60,
    borderRadius: 14,
    backgroundColor: Colors.white,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: Colors.gray200,
    borderStyle: 'dashed',
  },
  decorEmoji: {
    fontSize: 28,
  },
});
