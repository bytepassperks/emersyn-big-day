/**
 * Settings - Game settings and parent controls
 */
import React, { useState } from 'react';
import { View, Text, StyleSheet, Switch, TouchableOpacity, Alert, ScrollView } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';

export default function Settings() {
  const { level, xp, stars, coins, stats, stickerAlbum, resetGame, saveGame } = useGameStore();
  const totalStickers = stickerAlbum.pages.reduce((sum, p) => sum + p.stickers.filter(s => s.earned).length, 0);
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [musicEnabled, setMusicEnabled] = useState(true);
  const [breakReminder, setBreakReminder] = useState(true);

  const handleReset = () => {
    Alert.alert(
      'Reset Game?',
      'This will erase ALL progress. Are you sure?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Reset',
          style: 'destructive',
          onPress: async () => {
            resetGame();
            await saveGame();
            Alert.alert('Done', 'Game has been reset!');
          },
        },
      ]
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>{'\u2190'}</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Settings</Text>
      </View>

      <ScrollView style={styles.content} contentContainerStyle={{ paddingBottom: 40 }}>
        <Text style={styles.section}>Game Stats</Text>
        <View style={styles.statsCard}>
          <View style={styles.statRow}>
            <Text style={styles.statLabel}>Level</Text>
            <Text style={styles.statValue}>{level}</Text>
          </View>
          <View style={styles.statRow}>
            <Text style={styles.statLabel}>XP</Text>
            <Text style={styles.statValue}>{xp}</Text>
          </View>
          <View style={styles.statRow}>
            <Text style={styles.statLabel}>Stars</Text>
            <Text style={styles.statValue}>{stars}</Text>
          </View>
          <View style={styles.statRow}>
            <Text style={styles.statLabel}>Coins</Text>
            <Text style={styles.statValue}>{'\u20B9'}{coins}</Text>
          </View>
          <View style={styles.statRow}>
            <Text style={styles.statLabel}>Stickers</Text>
            <Text style={styles.statValue}>{totalStickers}</Text>
          </View>
        </View>

        <Text style={styles.section}>Audio</Text>
        <View style={styles.settingCard}>
          <View style={styles.settingRow}>
            <Text style={styles.settingLabel}>Sound Effects</Text>
            <Switch value={soundEnabled} onValueChange={setSoundEnabled} />
          </View>
          <View style={styles.settingRow}>
            <Text style={styles.settingLabel}>Background Music</Text>
            <Switch value={musicEnabled} onValueChange={setMusicEnabled} />
          </View>
        </View>

        <Text style={styles.section}>Parent Controls</Text>
        <View style={styles.settingCard}>
          <View style={styles.settingRow}>
            <Text style={styles.settingLabel}>Break Reminders (30 min)</Text>
            <Switch value={breakReminder} onValueChange={setBreakReminder} />
          </View>
        </View>

        <Text style={styles.section}>Danger Zone</Text>
        <TouchableOpacity style={styles.resetBtn} onPress={handleReset}>
          <Text style={styles.resetBtnText}>Reset All Progress</Text>
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F5F5F5' },
  header: { flexDirection: 'row', alignItems: 'center', padding: 16, paddingTop: 50, gap: 12 },
  backBtn: { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fff', alignItems: 'center', justifyContent: 'center' },
  backBtnText: { fontSize: 20, fontWeight: '800' },
  title: { fontSize: 22, fontWeight: '800', color: '#333' },
  content: { flex: 1, paddingHorizontal: 16 },
  section: { fontSize: 16, fontWeight: '800', color: '#999', marginTop: 20, marginBottom: 8 },
  statsCard: { backgroundColor: '#fff', borderRadius: 16, padding: 16, gap: 10 },
  statRow: { flexDirection: 'row', justifyContent: 'space-between' },
  statLabel: { fontSize: 15, fontWeight: '600', color: '#666' },
  statValue: { fontSize: 15, fontWeight: '800', color: '#333' },
  settingCard: { backgroundColor: '#fff', borderRadius: 16, padding: 16, gap: 12 },
  settingRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  settingLabel: { fontSize: 15, fontWeight: '600', color: '#333' },
  resetBtn: { backgroundColor: '#ff4444', padding: 16, borderRadius: 16, alignItems: 'center' },
  resetBtnText: { fontSize: 16, fontWeight: '800', color: '#fff' },
});
