import React from 'react';
import { TouchableOpacity, Text, StyleSheet, ViewStyle, TextStyle } from 'react-native';
import { Colors } from '@/lib/colors';

interface GameButtonProps {
  title: string;
  emoji?: string;
  onPress: () => void;
  variant?: 'primary' | 'secondary' | 'accent' | 'outline' | 'danger';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
}

export const GameButton: React.FC<GameButtonProps> = ({
  title,
  emoji,
  onPress,
  variant = 'primary',
  size = 'medium',
  disabled = false,
  style,
  textStyle,
}) => {
  const bgColor = {
    primary: Colors.pink,
    secondary: Colors.purple,
    accent: Colors.yellow,
    outline: 'transparent',
    danger: Colors.coral,
  }[variant];

  const txtColor = variant === 'outline' ? Colors.pink : variant === 'accent' ? Colors.dark : Colors.white;
  const borderColor = variant === 'outline' ? Colors.pink : bgColor;

  const paddingV = size === 'small' ? 8 : size === 'large' ? 18 : 14;
  const paddingH = size === 'small' ? 16 : size === 'large' ? 32 : 24;
  const fontSize = size === 'small' ? 14 : size === 'large' ? 20 : 16;
  const emojiSize = size === 'small' ? 16 : size === 'large' ? 24 : 20;

  return (
    <TouchableOpacity
      style={[
        styles.button,
        {
          backgroundColor: bgColor,
          borderColor,
          paddingVertical: paddingV,
          paddingHorizontal: paddingH,
        },
        disabled && styles.disabled,
        style,
      ]}
      onPress={onPress}
      disabled={disabled}
      activeOpacity={0.7}
    >
      {emoji && <Text style={[styles.emoji, { fontSize: emojiSize }]}>{emoji}</Text>}
      <Text style={[styles.text, { color: txtColor, fontSize }, textStyle]}>{title}</Text>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  button: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 20,
    borderWidth: 2,
    gap: 8,
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.15,
    shadowRadius: 5,
    elevation: 4,
  },
  disabled: {
    opacity: 0.5,
  },
  emoji: {
    textAlign: 'center',
  },
  text: {
    fontWeight: '800',
    textAlign: 'center',
  },
});
