import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Colors } from '@/lib/colors';
import { formatCoins } from '@/lib/helpers';

interface CoinDisplayProps {
  coins: number;
  stars?: number;
  size?: 'small' | 'medium' | 'large';
}

export const CoinDisplay: React.FC<CoinDisplayProps> = ({ coins, stars, size = 'medium' }) => {
  const fontSize = size === 'small' ? 14 : size === 'large' ? 22 : 18;
  const emojiSize = size === 'small' ? 16 : size === 'large' ? 26 : 20;

  return (
    <View style={styles.container}>
      <View style={styles.coinRow}>
        <Text style={[styles.emoji, { fontSize: emojiSize }]}>💰</Text>
        <Text style={[styles.coinText, { fontSize }]}>{formatCoins(coins)}</Text>
      </View>
      {stars !== undefined && (
        <View style={styles.coinRow}>
          <Text style={[styles.emoji, { fontSize: emojiSize }]}>⭐</Text>
          <Text style={[styles.starText, { fontSize }]}>{stars}</Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  coinRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.yellowLight,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 16,
    gap: 4,
  },
  emoji: {
    marginRight: 2,
  },
  coinText: {
    fontWeight: '800',
    color: Colors.orange,
  },
  starText: {
    fontWeight: '800',
    color: Colors.purple,
  },
});
