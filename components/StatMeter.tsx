import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Colors, getStatColor } from '@/lib/colors';

interface StatMeterProps {
  label: string;
  value: number;
  emoji: string;
  stat: string;
}

export const StatMeter: React.FC<StatMeterProps> = ({ label, value, emoji, stat }) => {
  const color = getStatColor(stat);
  const width = `${Math.min(100, Math.max(0, value))}%` as const;

  return (
    <View style={styles.container}>
      <Text style={styles.emoji}>{emoji}</Text>
      <View style={styles.barContainer}>
        <View style={styles.labelRow}>
          <Text style={styles.label}>{label}</Text>
          <Text style={styles.value}>{Math.round(value)}</Text>
        </View>
        <View style={styles.barBg}>
          <View style={[styles.barFill, { width, backgroundColor: color }]} />
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    marginVertical: 3,
    paddingHorizontal: 4,
  },
  emoji: {
    fontSize: 18,
    width: 28,
    textAlign: 'center',
  },
  barContainer: {
    flex: 1,
    marginLeft: 6,
  },
  labelRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 2,
  },
  label: {
    fontSize: 11,
    fontWeight: '600',
    color: Colors.gray500,
  },
  value: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.dark,
  },
  barBg: {
    height: 8,
    borderRadius: 4,
    backgroundColor: Colors.gray200,
    overflow: 'hidden',
  },
  barFill: {
    height: '100%',
    borderRadius: 4,
  },
});
