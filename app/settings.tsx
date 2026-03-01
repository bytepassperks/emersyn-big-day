import React, { useState } from 'react';
import { View, Text, StyleSheet, Switch, Alert } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { ParentGate } from '@/components/ParentGate';
import { Colors } from '@/lib/colors';

export default function Settings() {
  const {
    playerName, level, currentDay, coins, stars,
    musicEnabled, sfxEnabled, breakReminderMinutes,
    toggleMusic, toggleSFX, setBreakReminder,
    saveGame, resetGame,
  } = useGameStore();

  const [showParentGate, setShowParentGate] = useState(false);
  const [gateAction, setGateAction] = useState<'reset' | 'settings' | null>(null);

  const handleReset = () => {
    setGateAction('reset');
    setShowParentGate(true);
  };

  const handleParentGateSuccess = () => {
    setShowParentGate(false);
    if (gateAction === 'reset') {
      Alert.alert(
        'Reset Game?',
        'This will erase ALL progress, coins, items, and stickers. Are you sure?',
        [
          { text: 'Cancel', style: 'cancel' },
          {
            text: 'Reset Everything',
            style: 'destructive',
            onPress: () => {
              resetGame();
              Alert.alert('Game Reset', 'All progress has been reset.');
            },
          },
        ]
      );
    }
  };

  return (
    <ScreenWrapper title="Settings" emoji="⚙️" bgColor={Colors.gray100}>
      {/* Player Info */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>👧 Player Info</Text>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Name</Text>
          <Text style={styles.infoValue}>{playerName}</Text>
        </View>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Level</Text>
          <Text style={styles.infoValue}>{level}</Text>
        </View>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Day</Text>
          <Text style={styles.infoValue}>{currentDay}</Text>
        </View>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Coins</Text>
          <Text style={styles.infoValue}>₹{coins}</Text>
        </View>
        <View style={styles.infoRow}>
          <Text style={styles.infoLabel}>Stars</Text>
          <Text style={styles.infoValue}>⭐ {stars}</Text>
        </View>
      </View>

      {/* Audio Settings */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>🔊 Audio</Text>
        <View style={styles.toggleRow}>
          <Text style={styles.toggleLabel}>🎵 Music</Text>
          <Switch
            value={musicEnabled}
            onValueChange={() => { toggleMusic(); saveGame(); }}
            trackColor={{ false: Colors.gray300, true: Colors.pinkLight }}
            thumbColor={musicEnabled ? Colors.pink : Colors.gray400}
          />
        </View>
        <View style={styles.toggleRow}>
          <Text style={styles.toggleLabel}>🔔 Sound Effects</Text>
          <Switch
            value={sfxEnabled}
            onValueChange={() => { toggleSFX(); saveGame(); }}
            trackColor={{ false: Colors.gray300, true: Colors.pinkLight }}
            thumbColor={sfxEnabled ? Colors.pink : Colors.gray400}
          />
        </View>
      </View>

      {/* Break Reminders */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>⏰ Break Reminders</Text>
        <Text style={styles.sectionDesc}>Gentle reminders to take a break</Text>
        {[0, 15, 30, 45, 60].map((minutes) => (
          <GameButton
            key={minutes}
            title={minutes === 0 ? 'Off' : `Every ${minutes} min`}
            onPress={() => { setBreakReminder(minutes); saveGame(); }}
            variant={breakReminderMinutes === minutes ? 'primary' : 'outline'}
            size="small"
            style={styles.breakBtn}
          />
        ))}
      </View>

      {/* Parent Controls */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>🔒 Parent Controls</Text>
        <Text style={styles.sectionDesc}>Requires solving a math problem</Text>
        <GameButton
          title="Reset All Game Data"
          emoji="🗑️"
          onPress={handleReset}
          variant="danger"
          size="medium"
          style={{ marginTop: 8 }}
        />
      </View>

      {/* Save */}
      <View style={styles.section}>
        <GameButton
          title="Save Game Now"
          emoji="💾"
          onPress={async () => {
            await saveGame();
            Alert.alert('Saved! 💾', 'Your game has been saved.');
          }}
          variant="secondary"
          size="medium"
        />
      </View>

      {/* About */}
      <View style={styles.aboutSection}>
        <Text style={styles.aboutTitle}>Emersyn's Big Day</Text>
        <Text style={styles.aboutVersion}>Version 1.0.0</Text>
        <Text style={styles.aboutDesc}>
          A fun, safe, and educational game made with love 💖
        </Text>
        <Text style={styles.aboutSafety}>
          🛡️ No ads · No chat · No real social media · No data collection
        </Text>
      </View>

      <ParentGate
        visible={showParentGate}
        onSuccess={handleParentGateSuccess}
        onCancel={() => setShowParentGate(false)}
      />
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  section: {
    backgroundColor: Colors.white, marginHorizontal: 16, marginVertical: 6,
    padding: 16, borderRadius: 18,
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 2, elevation: 1,
  },
  sectionTitle: { fontSize: 18, fontWeight: '800', color: Colors.dark, marginBottom: 8 },
  sectionDesc: { fontSize: 12, color: Colors.gray400, marginBottom: 8 },
  infoRow: {
    flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 6,
    borderBottomWidth: 1, borderBottomColor: Colors.gray100,
  },
  infoLabel: { fontSize: 15, color: Colors.gray500 },
  infoValue: { fontSize: 15, fontWeight: '700', color: Colors.dark },
  toggleRow: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: 8,
  },
  toggleLabel: { fontSize: 15, fontWeight: '600', color: Colors.dark },
  breakBtn: { marginVertical: 3 },
  aboutSection: {
    alignItems: 'center', paddingVertical: 24, paddingHorizontal: 16,
  },
  aboutTitle: { fontSize: 20, fontWeight: '800', color: Colors.pink },
  aboutVersion: { fontSize: 13, color: Colors.gray400, marginTop: 4 },
  aboutDesc: { fontSize: 14, color: Colors.gray500, textAlign: 'center', marginTop: 8 },
  aboutSafety: { fontSize: 12, color: Colors.success, textAlign: 'center', marginTop: 8 },
});
