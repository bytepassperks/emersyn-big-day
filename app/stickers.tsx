/**
 * Stickers - Sticker album collection display
 */
import React, { useMemo } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Dimensions } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { stickers as stickerData } from '@/content/stickers';

const { width: SW } = Dimensions.get('window');

export default function Stickers() {
  const { stickerAlbum } = useGameStore();

  // Build flat list of earned sticker IDs from the album
  const earned = useMemo(() => {
    const ids: string[] = [];
    stickerAlbum.pages.forEach(page => {
      page.stickers.forEach(s => {
        if (s.earned) ids.push(s.id);
      });
    });
    return ids;
  }, [stickerAlbum]);

  const categories = useMemo(() => {
    const cats: Record<string, typeof stickerData> = {};
    stickerData.forEach(s => {
      if (!cats[s.category]) cats[s.category] = [];
      cats[s.category].push(s);
    });
    return cats;
  }, []);

  const totalEarned = stickerData.filter(s => earned.includes(s.id)).length;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>{'\u2190'}</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Sticker Album</Text>
        <View style={styles.countBadge}>
          <Text style={styles.countText}>{totalEarned}/{stickerData.length}</Text>
        </View>
      </View>

      <ScrollView style={styles.albumScroll} contentContainerStyle={{ paddingBottom: 30 }}>
        {Object.entries(categories).map(([cat, items]) => (
          <View key={cat}>
            <Text style={styles.catTitle}>{cat}</Text>
            <View style={styles.stickerGrid}>
              {items.map(sticker => {
                const isEarned = earned.includes(sticker.id);
                return (
                  <View key={sticker.id} style={[styles.stickerCard, !isEarned && styles.stickerLocked]}>
                    <Text style={styles.stickerEmoji}>{isEarned ? sticker.emoji : '?'}</Text>
                    <Text style={styles.stickerName} numberOfLines={1}>
                      {isEarned ? sticker.name : '???'}
                    </Text>
                  </View>
                );
              })}
            </View>
          </View>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8E1' },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: 16, paddingTop: 50 },
  backBtn: { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fff', alignItems: 'center', justifyContent: 'center' },
  backBtnText: { fontSize: 20, fontWeight: '800' },
  title: { fontSize: 22, fontWeight: '800', color: '#333' },
  countBadge: { backgroundColor: '#ff6b9d', paddingHorizontal: 12, paddingVertical: 6, borderRadius: 14 },
  countText: { fontSize: 14, fontWeight: '800', color: '#fff' },
  albumScroll: { flex: 1, paddingHorizontal: 16 },
  catTitle: { fontSize: 18, fontWeight: '800', color: '#ff9f43', marginTop: 16, marginBottom: 8 },
  stickerGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  stickerCard: { width: (SW - 62) / 4, backgroundColor: '#fff', borderRadius: 14, padding: 10, alignItems: 'center' },
  stickerLocked: { opacity: 0.4, backgroundColor: '#f5f5f5' },
  stickerEmoji: { fontSize: 30 },
  stickerName: { fontSize: 10, fontWeight: '700', color: '#333', marginTop: 4, textAlign: 'center' },
});
