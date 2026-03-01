import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { Colors } from '@/lib/colors';

export default function Stickers() {
  const { stickerAlbum, achievements } = useGameStore();

  const totalStickers = stickerAlbum.pages.reduce((sum, page) => sum + page.stickers.length, 0);
  const earnedStickers = stickerAlbum.pages.reduce(
    (sum, page) => sum + page.stickers.filter((s) => s.earned).length, 0
  );

  return (
    <ScreenWrapper title="Sticker Album" emoji="🏷️" bgColor={Colors.purpleLight}>
      <View style={styles.headerArea}>
        <Text style={styles.progressText}>
          {earnedStickers} / {totalStickers} stickers earned
        </Text>
        <View style={styles.progressBar}>
          <View style={[styles.progressFill, { width: `${(earnedStickers / totalStickers) * 100}%` }]} />
        </View>
      </View>

      {stickerAlbum.pages.map((page) => {
        const pageEarned = page.stickers.filter((s) => s.earned).length;
        const pageTotal = page.stickers.length;
        return (
          <View key={page.id} style={styles.pageCard}>
            <View style={styles.pageHeader}>
              <Text style={styles.pageEmoji}>{page.emoji}</Text>
              <View style={styles.pageInfo}>
                <Text style={styles.pageName}>{page.name}</Text>
                <Text style={styles.pageProgress}>{pageEarned}/{pageTotal}</Text>
              </View>
              {page.completed && <Text style={styles.completedBadge}>🏆</Text>}
            </View>

            <View style={styles.stickerGrid}>
              {page.stickers.map((sticker) => (
                <View
                  key={sticker.id}
                  style={[styles.stickerSlot, sticker.earned && styles.stickerEarned]}
                >
                  <Text style={styles.stickerEmoji}>
                    {sticker.earned ? sticker.emoji : '❓'}
                  </Text>
                  <Text style={[styles.stickerName, !sticker.earned && styles.stickerNameLocked]}>
                    {sticker.earned ? sticker.name : '???'}
                  </Text>
                </View>
              ))}
            </View>

            {!page.completed && (
              <Text style={styles.rewardPreview}>🎁 Reward: {page.reward}</Text>
            )}
          </View>
        );
      })}

      {/* Achievements */}
      <Text style={styles.sectionTitle}>🏆 Achievements</Text>
      {achievements.map((ach) => (
        <View key={ach.id} style={[styles.achievementCard, ach.earned && styles.achievementEarned]}>
          <Text style={styles.achievementEmoji}>{ach.earned ? ach.emoji : '🔒'}</Text>
          <View style={styles.achievementInfo}>
            <Text style={styles.achievementName}>{ach.name}</Text>
            <Text style={styles.achievementDesc}>{ach.description}</Text>
          </View>
          {ach.earned && <Text style={styles.achievementCheck}>✓</Text>}
        </View>
      ))}
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  headerArea: { paddingHorizontal: 16, paddingVertical: 16 },
  progressText: { fontSize: 16, fontWeight: '700', color: Colors.dark, textAlign: 'center', marginBottom: 8 },
  progressBar: { height: 10, borderRadius: 5, backgroundColor: Colors.gray200, overflow: 'hidden' },
  progressFill: { height: '100%', borderRadius: 5, backgroundColor: Colors.purple },
  pageCard: {
    backgroundColor: Colors.white, marginHorizontal: 16, marginVertical: 6, padding: 16, borderRadius: 20,
    borderWidth: 2, borderColor: Colors.purpleLight,
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.06, shadowRadius: 3, elevation: 2,
  },
  pageHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: 12 },
  pageEmoji: { fontSize: 28, marginRight: 10 },
  pageInfo: { flex: 1 },
  pageName: { fontSize: 16, fontWeight: '800', color: Colors.dark },
  pageProgress: { fontSize: 12, color: Colors.gray400, marginTop: 2 },
  completedBadge: { fontSize: 24 },
  stickerGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  stickerSlot: {
    width: 70, height: 70, borderRadius: 14, backgroundColor: Colors.gray100,
    justifyContent: 'center', alignItems: 'center', borderWidth: 1, borderColor: Colors.gray200,
  },
  stickerEarned: { backgroundColor: Colors.yellowLight, borderColor: Colors.yellow },
  stickerEmoji: { fontSize: 24 },
  stickerName: { fontSize: 8, fontWeight: '700', color: Colors.dark, marginTop: 2, textAlign: 'center' },
  stickerNameLocked: { color: Colors.gray400 },
  rewardPreview: { fontSize: 12, color: Colors.purple, fontWeight: '600', marginTop: 10 },
  sectionTitle: {
    fontSize: 20, fontWeight: '800', color: Colors.dark,
    paddingHorizontal: 16, paddingTop: 20, paddingBottom: 8,
  },
  achievementCard: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.white,
    marginHorizontal: 16, marginVertical: 3, padding: 12, borderRadius: 14,
    borderWidth: 1, borderColor: Colors.gray200,
  },
  achievementEarned: { backgroundColor: Colors.yellowLight, borderColor: Colors.yellow },
  achievementEmoji: { fontSize: 24, marginRight: 10 },
  achievementInfo: { flex: 1 },
  achievementName: { fontSize: 14, fontWeight: '700', color: Colors.dark },
  achievementDesc: { fontSize: 11, color: Colors.gray400, marginTop: 2 },
  achievementCheck: { fontSize: 18, fontWeight: '800', color: Colors.success },
});
