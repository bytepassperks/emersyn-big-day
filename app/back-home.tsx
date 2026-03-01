import React, { useState } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { ActivityButton } from '@/components/ActivityButton';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getActivitiesBySegment } from '@/content/activities';
import { getRandomEncouragement } from '@/lib/helpers';

export default function BackHome() {
  const router = useRouter();
  const { updateStats, addCoins, addXP, earnSticker, saveGame } = useGameStore();
  const [completedActivities, setCompletedActivities] = useState<string[]>([]);

  const backHomeActivities = getActivitiesBySegment('backHome');
  const allDone = completedActivities.length === backHomeActivities.length;

  const handleActivity = async (activityId: string, statDeltas: Record<string, number>, coinReward: number, xpReward: number) => {
    if (completedActivities.includes(activityId)) return;

    updateStats(statDeltas);
    addCoins(coinReward);
    addXP(xpReward);
    setCompletedActivities((prev) => [...prev, activityId]);

    await saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${coinReward} coins!`);
  };

  const handleAllDone = () => {
    earnSticker('sticker_clean_room');
    Alert.alert('All Clean! 🌟', 'Great job completing the back home routine!');
    router.back();
  };

  return (
    <ScreenWrapper title="Back Home" emoji="🏠" bgColor={Colors.pinkLight}>
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>🏠</Text>
        <Text style={styles.roomDesc}>Complete the back home routine!</Text>
        <Text style={styles.routineOrder}>Shoes → Hands → Face → Clothes → Water</Text>
      </View>

      <Text style={styles.sectionTitle}>📋 Routine Checklist</Text>
      {backHomeActivities.map((activity, index) => {
        const prevCompleted = index === 0 || completedActivities.includes(backHomeActivities[index - 1].id);
        return (
          <ActivityButton
            key={activity.id}
            activity={activity}
            completed={completedActivities.includes(activity.id)}
            disabled={!prevCompleted}
            onPress={() =>
              handleActivity(activity.id, activity.statDeltas, activity.coinReward, activity.xpReward)
            }
          />
        );
      })}

      {/* Progress indicator */}
      <View style={styles.progressRow}>
        {backHomeActivities.map((act, i) => (
          <View
            key={i}
            style={[
              styles.progressDot,
              completedActivities.includes(act.id) && styles.progressDotDone,
            ]}
          />
        ))}
      </View>

      {allDone && (
        <GameButton
          title="All Done! Go Home"
          emoji="🎉"
          onPress={handleAllDone}
          variant="accent"
          size="large"
          style={{ marginHorizontal: 16, marginTop: 16 }}
        />
      )}
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  characterArea: { alignItems: 'center', paddingVertical: 20 },
  characterEmoji: { fontSize: 64 },
  roomDesc: { fontSize: 14, color: Colors.gray500, marginTop: 8 },
  routineOrder: { fontSize: 12, color: Colors.pink, fontWeight: '700', marginTop: 4 },
  sectionTitle: {
    fontSize: 18, fontWeight: '800', color: Colors.dark,
    paddingHorizontal: 16, paddingTop: 16, paddingBottom: 8,
  },
  progressRow: {
    flexDirection: 'row', justifyContent: 'center', gap: 10, paddingVertical: 16,
  },
  progressDot: {
    width: 16, height: 16, borderRadius: 8, backgroundColor: Colors.gray200,
  },
  progressDotDone: {
    backgroundColor: Colors.success,
  },
});
