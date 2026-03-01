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

export default function Park() {
  const router = useRouter();
  const { updateStats, addCoins, addXP, addStars, earnSticker, saveGame } = useGameStore();
  const [completedActivities, setCompletedActivities] = useState<string[]>([]);

  const parkActivities = getActivitiesBySegment('park');

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

    if (completedActivities.length === 0) earnSticker('sticker_first_park');
    if (activityId === 'park_scooty') earnSticker('sticker_scooty_ride');
    if (activityId === 'park_trampoline') earnSticker('sticker_trampoline');
    if (activityId === 'park_skating') earnSticker('sticker_skating');
    if (activityId === 'park_slides') earnSticker('sticker_slide_fun');

    await saveGame();
    Alert.alert(getRandomEncouragement(), `+₹${coinReward} coins!`);
  };

  return (
    <ScreenWrapper title="Park" emoji="🏞️" bgColor={Colors.bgPark}>
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>🌳</Text>
        <Text style={styles.roomDesc}>Sunshine and outdoor fun!</Text>
      </View>

      <Text style={styles.sectionTitle}>🎢 Activities</Text>
      {parkActivities.map((activity) => (
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

      <View style={styles.backHomeSection}>
        <Text style={styles.sectionTitle}>🏠 Back Home Routine</Text>
        <Text style={styles.routineDesc}>
          After playing outside, don't forget to clean up!
        </Text>
        <GameButton
          title="Go Back Home"
          emoji="🏠"
          onPress={() => router.push('/back-home')}
          variant="primary"
          size="medium"
          style={{ marginHorizontal: 16, marginVertical: 8 }}
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
  backHomeSection: { marginTop: 8 },
  routineDesc: {
    fontSize: 13, color: Colors.gray500, paddingHorizontal: 16, marginBottom: 4,
  },
});
