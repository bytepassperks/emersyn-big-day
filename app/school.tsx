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

export default function School() {
  const router = useRouter();
  const { updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [completedActivities, setCompletedActivities] = useState<string[]>([]);

  const schoolActivities = getActivitiesBySegment('school');

  const handleActivity = async (activityId: string, statDeltas: Record<string, number>, coinReward: number, xpReward: number, starReward: number, miniGameRoute?: string) => {
    if (completedActivities.includes(activityId)) return;

    if (miniGameRoute) {
      router.push(miniGameRoute as any);
      return;
    }

    updateStats(statDeltas);
    addCoins(coinReward);
    addXP(xpReward);
    if (starReward > 0) addStars(starReward);
    setCompletedActivities((prev) => [...prev, activityId]);

    if (completedActivities.length === 0) earnSticker('sticker_first_school');
    earnSticker('sticker_art_class');

    await saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${coinReward} coins!`);
  };

  return (
    <ScreenWrapper title="School" emoji="🏫" bgColor={Colors.bgSchool}>
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>📚</Text>
        <Text style={styles.roomDesc}>Time to learn and play!</Text>
      </View>

      <Text style={styles.sectionTitle}>📖 Today's Lessons</Text>
      {schoolActivities.map((activity) => (
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
              activity.starReward,
              activity.miniGameRoute
            )
          }
        />
      ))}

      <View style={styles.miniGameSection}>
        <Text style={styles.sectionTitle}>🎮 School Games</Text>
        <GameButton
          title="Homework Time"
          emoji="📚"
          onPress={() => router.push('/minigames/homework')}
          variant="secondary"
          size="medium"
          style={{ marginHorizontal: 16, marginVertical: 4 }}
        />
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  characterArea: { alignItems: 'center', paddingVertical: 20 },
  characterEmoji: { fontSize: 64 },
  roomDesc: { fontSize: 14, color: Colors.gray500, marginTop: 8 },
  sectionTitle: {
    fontSize: 18, fontWeight: '800', color: Colors.dark,
    paddingHorizontal: 16, paddingTop: 16, paddingBottom: 8,
  },
  miniGameSection: { marginTop: 8 },
});
