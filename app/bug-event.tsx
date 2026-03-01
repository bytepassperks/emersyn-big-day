import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { getRandomBugEvent } from '@/content/activities';
import { BugEvent, BugAction } from '@/lib/types';
import { getRandomEncouragement } from '@/lib/helpers';

export default function BugEventScreen() {
  const router = useRouter();
  const { handleBugEvent, addCoins, addXP, earnSticker, saveGame } = useGameStore();
  const [event, setEvent] = useState<BugEvent | null>(null);
  const [resolved, setResolved] = useState(false);
  const [selectedAction, setSelectedAction] = useState<BugAction | null>(null);

  useEffect(() => {
    setEvent(getRandomBugEvent());
  }, []);

  if (!event) return null;

  const bugEmoji = event.type === 'mosquito' ? '🦟' : event.type === 'fly' ? '🪰' : '🐜';

  const handleAction = async (action: BugAction) => {
    setSelectedAction(action);

    const success = Math.random() * 100 < action.effectiveness;
    handleBugEvent(event.id, action.id, success);

    if (success) {
      addCoins(10);
      addXP(15);
      earnSticker('sticker_first_bug');
      earnSticker('sticker_' + action.id.replace(/_[a-z]$/, ''));
    } else {
      addCoins(5);
      addXP(8);
    }

    setResolved(true);
    await saveGame();
  };

  if (resolved && selectedAction) {
    const success = Math.random() * 100 < selectedAction.effectiveness;
    return (
      <ScreenWrapper title="Bug Event" emoji="🦸" bgColor={Colors.mintLight} showBack={false} scrollable={false}>
        <View style={styles.resultArea}>
          <Text style={styles.resultEmoji}>{success ? '🎉' : '😅'}</Text>
          <Text style={styles.resultTitle}>
            {success ? 'Great job!' : 'Almost got it!'}
          </Text>
          <Text style={styles.resultDesc}>
            {success
              ? `You used "${selectedAction.name}" and it worked! The ${event.type} is gone!`
              : `The ${event.type} escaped, but you were really brave trying!`
            }
          </Text>
          <Text style={styles.encouragement}>{getRandomEncouragement()}</Text>

          <View style={styles.rewardCard}>
            <Text style={styles.rewardText}>💰 +₹{success ? 10 : 5} coins</Text>
            <Text style={styles.rewardText}>⭐ +{success ? 15 : 8} XP</Text>
            <Text style={styles.rewardText}>🦸 Brave Badge progress!</Text>
          </View>

          <GameButton
            title="Continue"
            emoji="✨"
            onPress={() => router.back()}
            variant="primary"
            size="large"
            style={{ marginTop: 20 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  return (
    <ScreenWrapper title="Bug Alert!" emoji="⚠️" bgColor={Colors.yellowLight} showBack={false} scrollable={false}>
      <View style={styles.eventArea}>
        <Text style={styles.bugEmoji}>{bugEmoji}</Text>
        <Text style={styles.eventTitle}>Oh no! A {event.type}!</Text>
        <Text style={styles.eventDesc}>
          A {event.type} appeared in the {event.location}! What should we do?
        </Text>
        <Text style={styles.encourageText}>
          Don't worry, you can handle this! 💪
        </Text>

        <Text style={styles.actionsTitle}>Choose an action:</Text>
        {event.actions.map((action) => (
          <GameButton
            key={action.id}
            title={action.name}
            emoji={action.emoji}
            onPress={() => handleAction(action)}
            variant="primary"
            size="medium"
            style={styles.actionBtn}
          />
        ))}
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  eventArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  bugEmoji: { fontSize: 80 },
  eventTitle: { fontSize: 26, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  eventDesc: { fontSize: 16, color: Colors.gray500, textAlign: 'center', marginTop: 8 },
  encourageText: { fontSize: 14, color: Colors.pink, fontWeight: '700', marginTop: 8 },
  actionsTitle: {
    fontSize: 18, fontWeight: '800', color: Colors.dark, marginTop: 24, marginBottom: 12,
  },
  actionBtn: { width: '100%', marginVertical: 4 },
  // Result
  resultArea: { flex: 1, justifyContent: 'center', alignItems: 'center', paddingHorizontal: 20 },
  resultEmoji: { fontSize: 80 },
  resultTitle: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  resultDesc: { fontSize: 16, color: Colors.gray500, textAlign: 'center', marginTop: 8 },
  encouragement: { fontSize: 16, color: Colors.pink, fontWeight: '700', marginTop: 8 },
  rewardCard: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 20,
    width: '100%', borderWidth: 2, borderColor: Colors.mintLight, gap: 8,
  },
  rewardText: { fontSize: 18, fontWeight: '700', color: Colors.dark },
});
