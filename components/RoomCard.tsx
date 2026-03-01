import React from 'react';
import { TouchableOpacity, Text, StyleSheet, View } from 'react-native';
import { Colors } from '@/lib/colors';

interface RoomCardProps {
  name: string;
  emoji: string;
  description: string;
  bgColor: string;
  onPress: () => void;
  locked?: boolean;
  badge?: string;
}

export const RoomCard: React.FC<RoomCardProps> = ({
  name,
  emoji,
  description,
  bgColor,
  onPress,
  locked = false,
  badge,
}) => {
  return (
    <TouchableOpacity
      style={[styles.card, { backgroundColor: bgColor }]}
      onPress={onPress}
      disabled={locked}
      activeOpacity={0.7}
    >
      {locked && (
        <View style={styles.lockOverlay}>
          <Text style={styles.lockEmoji}>🔒</Text>
        </View>
      )}
      <Text style={styles.emoji}>{emoji}</Text>
      <Text style={styles.name}>{name}</Text>
      <Text style={styles.description}>{description}</Text>
      {badge && (
        <View style={styles.badge}>
          <Text style={styles.badgeText}>{badge}</Text>
        </View>
      )}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    width: '46%',
    aspectRatio: 1,
    borderRadius: 24,
    padding: 16,
    alignItems: 'center',
    justifyContent: 'center',
    margin: '2%',
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.12,
    shadowRadius: 6,
    elevation: 4,
    position: 'relative',
  },
  lockOverlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: Colors.overlayLight,
    borderRadius: 24,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1,
  },
  lockEmoji: {
    fontSize: 32,
  },
  emoji: {
    fontSize: 42,
    marginBottom: 8,
  },
  name: {
    fontSize: 15,
    fontWeight: '800',
    color: Colors.dark,
    textAlign: 'center',
  },
  description: {
    fontSize: 11,
    color: Colors.gray500,
    textAlign: 'center',
    marginTop: 4,
  },
  badge: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: Colors.pink,
    borderRadius: 10,
    paddingHorizontal: 8,
    paddingVertical: 2,
  },
  badgeText: {
    fontSize: 10,
    fontWeight: '700',
    color: Colors.white,
  },
});
