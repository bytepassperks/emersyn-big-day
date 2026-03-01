import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { CoinDisplay } from '@/components/CoinDisplay';
import { RoomCard } from '@/components/RoomCard';
import { Colors } from '@/lib/colors';
import { getBeltDisplayName, getBeltEmoji } from '@/lib/helpers';

export default function Arcade() {
  const router = useRouter();
  const { coins, stars, totalMinigamesPlayed, karateBelt, danceStars } = useGameStore();

  return (
    <ScreenWrapper title="Arcade Hub" emoji="🕹️" bgColor={Colors.bgArcade}>
      <View style={styles.headerArea}>
        <CoinDisplay coins={coins} stars={stars} size="medium" />
        <Text style={styles.gamesPlayed}>🎮 {totalMinigamesPlayed} games played</Text>
      </View>

      <Text style={styles.sectionTitle}>🎮 Mini-Games</Text>
      <View style={styles.gameGrid}>
        <RoomCard
          name="Scooty Dash"
          emoji="🛴"
          description="Endless runner!"
          bgColor={Colors.mintLight}
          onPress={() => router.push('/minigames/endless-runner')}
          badge="🔥"
        />
        <RoomCard
          name="Dance Party"
          emoji="💃"
          description="Rhythm game!"
          bgColor={Colors.coralLight}
          onPress={() => router.push('/minigames/dance-party')}
          badge={`⭐${danceStars}`}
        />
        <RoomCard
          name="Karate Star"
          emoji="🥋"
          description={`${getBeltEmoji(karateBelt)} ${getBeltDisplayName(karateBelt)}`}
          bgColor={Colors.yellowLight}
          onPress={() => router.push('/minigames/karate-star')}
        />
        <RoomCard
          name="Trampoline"
          emoji="🤸"
          description="Bounce & combo!"
          bgColor={Colors.skyLight}
          onPress={() => router.push('/minigames/trampoline')}
        />
        <RoomCard
          name="Pizza Maker"
          emoji="🍕"
          description="Cook a pizza!"
          bgColor={Colors.orangeLight}
          onPress={() => router.push('/cooking')}
        />
        <RoomCard
          name="Homework"
          emoji="📚"
          description="Shapes & counting"
          bgColor={Colors.purpleLight}
          onPress={() => router.push('/minigames/homework')}
        />
      </View>

      <View style={styles.tip}>
        <Text style={styles.tipEmoji}>💡</Text>
        <Text style={styles.tipText}>
          Play mini-games to earn coins and stars! Each game gives different rewards.
        </Text>
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  headerArea: {
    alignItems: 'center', paddingVertical: 16, gap: 8,
  },
  gamesPlayed: {
    fontSize: 13, color: Colors.gray500, fontWeight: '600',
  },
  sectionTitle: {
    fontSize: 20, fontWeight: '800', color: Colors.dark,
    paddingHorizontal: 16, paddingTop: 8, paddingBottom: 8,
  },
  gameGrid: {
    flexDirection: 'row', flexWrap: 'wrap', paddingHorizontal: 8,
  },
  tip: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.yellowLight,
    marginHorizontal: 16, marginTop: 16, padding: 12, borderRadius: 14, gap: 8,
  },
  tipEmoji: { fontSize: 20 },
  tipText: { fontSize: 13, color: Colors.gray500, flex: 1 },
});
