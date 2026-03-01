import React from 'react';
import { TouchableOpacity, Text, StyleSheet, View } from 'react-native';
import { Colors } from '@/lib/colors';
import { Activity } from '@/lib/types';

interface ActivityButtonProps {
  activity: Activity;
  onPress: () => void;
  disabled?: boolean;
  completed?: boolean;
}

export const ActivityButton: React.FC<ActivityButtonProps> = ({
  activity,
  onPress,
  disabled = false,
  completed = false,
}) => {
  return (
    <TouchableOpacity
      style={[
        styles.button,
        disabled && styles.buttonDisabled,
        completed && styles.buttonCompleted,
      ]}
      onPress={onPress}
      disabled={disabled || completed}
      activeOpacity={0.7}
    >
      <Text style={styles.emoji}>{activity.emoji}</Text>
      <View style={styles.textContainer}>
        <Text style={[styles.name, completed && styles.nameCompleted]}>
          {activity.name}
        </Text>
        {activity.coinReward > 0 && (
          <Text style={styles.reward}>+₹{activity.coinReward}</Text>
        )}
      </View>
      {completed && <Text style={styles.checkmark}>✓</Text>}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  button: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.white,
    paddingVertical: 14,
    paddingHorizontal: 16,
    borderRadius: 16,
    marginVertical: 4,
    marginHorizontal: 8,
    shadowColor: Colors.pink,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    borderWidth: 2,
    borderColor: Colors.pinkLight,
  },
  buttonDisabled: {
    opacity: 0.5,
    borderColor: Colors.gray200,
  },
  buttonCompleted: {
    backgroundColor: Colors.mintLight,
    borderColor: Colors.mint,
  },
  emoji: {
    fontSize: 28,
    marginRight: 12,
  },
  textContainer: {
    flex: 1,
  },
  name: {
    fontSize: 16,
    fontWeight: '700',
    color: Colors.dark,
  },
  nameCompleted: {
    textDecorationLine: 'line-through',
    color: Colors.gray400,
  },
  reward: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.orange,
    marginTop: 2,
  },
  checkmark: {
    fontSize: 20,
    color: Colors.success,
    fontWeight: '800',
  },
});
