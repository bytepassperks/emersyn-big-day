import React, { useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  SafeAreaView,
  TouchableOpacity,
} from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { StatMeter } from '@/components/StatMeter';
import { CoinDisplay } from '@/components/CoinDisplay';
import { RoomCard } from '@/components/RoomCard';
import { Colors } from '@/lib/colors';
import { getTimeOfDayEmoji, getSegmentName } from '@/lib/helpers';
import { Scene3D } from '@/components/Scene3D';

export default function HomeHub() {
  const router = useRouter();
  const {
    playerName,
    coins,
    stars,
    level,
    stats,
    currentDay,
    dayType,
    currentSegment,
    segmentsCompleted,
    saveGame,
  } = useGameStore();

  // Auto-save every 30 seconds
  useEffect(() => {
    const interval = setInterval(() => {
      saveGame();
    }, 30000);
    return () => clearInterval(interval);
  }, [saveGame]);

  const isWeekend = dayType === 'weekend';

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Header */}
        <View style={styles.header}>
          <View>
            <Text style={styles.greeting}>
              {getTimeOfDayEmoji(currentSegment)} {getSegmentName(currentSegment)}
            </Text>
            <Text style={styles.playerName}>{playerName}</Text>
            <Text style={styles.dayInfo}>
              Day {currentDay} {isWeekend ? '🎉 Weekend!' : '📅 Weekday'} · Level {level}
            </Text>
          </View>
          <TouchableOpacity
            style={styles.settingsBtn}
            onPress={() => router.push('/settings')}
          >
            <Text style={styles.settingsEmoji}>⚙️</Text>
          </TouchableOpacity>
        </View>

        {/* Coin & Stars Display */}
        <View style={styles.currencyRow}>
          <CoinDisplay coins={coins} stars={stars} size="medium" />
        </View>

        {/* 3D Character Scene */}
        <Scene3D
          sceneType="home"
          characterVisible={true}
          characterAnimation="idle"
          height={240}
        />

        {/* Stats */}
        <View style={styles.statsContainer}>
          <StatMeter label="Hunger" value={stats.hunger} emoji="🍕" stat="hunger" />
          <StatMeter label="Energy" value={stats.energy} emoji="⚡" stat="energy" />
          <StatMeter label="Clean" value={stats.cleanliness} emoji="✨" stat="cleanliness" />
          <StatMeter label="Fun" value={stats.fun} emoji="🎉" stat="fun" />
          <StatMeter label="Popular" value={stats.popularity} emoji="💖" stat="popularity" />
        </View>

        {/* Room Navigation */}
        <Text style={styles.sectionTitle}>🏠 Where to go?</Text>
        <View style={styles.roomGrid}>
          <RoomCard
            name="Bedroom"
            emoji="🛏️"
            description="Rest & get ready"
            bgColor={Colors.bgBedroom}
            onPress={() => router.push('/bedroom')}
          />
          <RoomCard
            name="Bathroom"
            emoji="🛁"
            description="Clean up!"
            bgColor={Colors.skyLight}
            onPress={() => router.push('/bathroom')}
          />
          <RoomCard
            name="Kitchen"
            emoji="🍳"
            description="Cook yummy food"
            bgColor={Colors.bgKitchen}
            onPress={() => router.push('/cooking')}
          />
          {!isWeekend && (
            <RoomCard
              name="School"
              emoji="🏫"
              description="Learn & play"
              bgColor={Colors.bgSchool}
              onPress={() => router.push('/school')}
            />
          )}
          <RoomCard
            name="Park"
            emoji="🏞️"
            description="Outdoor fun!"
            bgColor={Colors.bgPark}
            onPress={() => router.push('/park')}
          />
          <RoomCard
            name="Arcade"
            emoji="🕹️"
            description="Mini-games!"
            bgColor={Colors.bgArcade}
            onPress={() => router.push('/arcade')}
            badge="NEW"
          />
          <RoomCard
            name="Shop"
            emoji="🛍️"
            description="Buy cute things"
            bgColor={Colors.bgShop}
            onPress={() => router.push('/shop')}
          />
          {isWeekend && (
            <RoomCard
              name="Studio"
              emoji="🎬"
              description="Create reels!"
              bgColor={Colors.bgStudio}
              onPress={() => router.push('/studio')}
              badge="WEEKEND"
            />
          )}
          <RoomCard
            name="Stickers"
            emoji="🏷️"
            description="Collection album"
            bgColor={Colors.purpleLight}
            onPress={() => router.push('/stickers')}
          />
          <RoomCard
            name="Homework"
            emoji="📚"
            description="Shapes & counting"
            bgColor={Colors.mintLight}
            onPress={() => router.push('/minigames/homework')}
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: Colors.pinkLight,
  },
  scroll: {
    flex: 1,
  },
  scrollContent: {
    paddingBottom: 40,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 8,
  },
  greeting: {
    fontSize: 24,
    fontWeight: '800',
    color: Colors.dark,
  },
  playerName: {
    fontSize: 16,
    fontWeight: '600',
    color: Colors.pink,
    marginTop: 2,
  },
  dayInfo: {
    fontSize: 13,
    color: Colors.gray500,
    marginTop: 4,
  },
  settingsBtn: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: Colors.white,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  settingsEmoji: {
    fontSize: 22,
  },
  currencyRow: {
    paddingHorizontal: 20,
    paddingVertical: 8,
  },
  characterArea: {
    alignItems: 'center',
    paddingVertical: 16,
  },
  characterPlaceholder: {
    width: 140,
    height: 140,
    borderRadius: 70,
    backgroundColor: Colors.white,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 3,
    borderColor: Colors.pink,
    shadowColor: Colors.pink,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 5,
  },
  characterEmoji: {
    fontSize: 64,
  },
  characterLabel: {
    fontSize: 14,
    fontWeight: '700',
    color: Colors.pink,
    marginTop: 4,
  },
  statsContainer: {
    backgroundColor: Colors.white,
    marginHorizontal: 16,
    borderRadius: 20,
    padding: 12,
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 4,
    elevation: 3,
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: '800',
    color: Colors.dark,
    paddingHorizontal: 20,
    paddingTop: 20,
    paddingBottom: 8,
  },
  roomGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: 8,
  },
});
