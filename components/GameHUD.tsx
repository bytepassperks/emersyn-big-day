/**
 * GameHUD.tsx - Heads-up display overlay for the game scene
 * Shows stats, coins, action buttons, NPC dialogue, and activity labels
 */
import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Animated,
  Dimensions,
} from 'react-native';
import { InteractableInfo } from '../engine/RoomBuilder';
import { NPCCharacter } from '../engine/NPCCharacter';

interface GameHUDProps {
  coins: number;
  stats: {
    hunger: number;
    energy: number;
    cleanliness: number;
    fun: number;
    popularity: number;
  };
  level: number;
  xp: number;
  xpToNext: number;
  roomName: string;
  onBack?: () => void;
  interactables?: InteractableInfo[];
  activeNPCDialogue?: { npcName: string; text: string } | null;
  showCoinAnimation?: boolean;
  coinDelta?: number;
}

export default function GameHUD({
  coins,
  stats,
  level,
  xp,
  xpToNext,
  roomName,
  onBack,
  activeNPCDialogue,
  showCoinAnimation,
  coinDelta,
}: GameHUDProps) {
  const coinAnim = useRef(new Animated.Value(0)).current;
  const dialogueAnim = useRef(new Animated.Value(0)).current;
  const [showDialogue, setShowDialogue] = useState(false);

  // Coin collect animation
  useEffect(() => {
    if (showCoinAnimation) {
      coinAnim.setValue(0);
      Animated.sequence([
        Animated.timing(coinAnim, { toValue: 1, duration: 300, useNativeDriver: true }),
        Animated.timing(coinAnim, { toValue: 0, duration: 500, useNativeDriver: true }),
      ]).start();
    }
  }, [showCoinAnimation, coinDelta]);

  // NPC dialogue
  useEffect(() => {
    if (activeNPCDialogue) {
      setShowDialogue(true);
      dialogueAnim.setValue(0);
      Animated.timing(dialogueAnim, { toValue: 1, duration: 300, useNativeDriver: true }).start();
      const timer = setTimeout(() => {
        Animated.timing(dialogueAnim, { toValue: 0, duration: 300, useNativeDriver: true }).start(() => {
          setShowDialogue(false);
        });
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [activeNPCDialogue]);

  const getStatColor = (value: number) => {
    if (value > 70) return '#66bb6a';
    if (value > 40) return '#ffd93d';
    return '#ef5350';
  };

  const getStatEmoji = (stat: string) => {
    switch (stat) {
      case 'hunger': return '\uD83C\uDF54';
      case 'energy': return '\u26A1';
      case 'cleanliness': return '\uD83D\uDCA7';
      case 'fun': return '\u2B50';
      case 'popularity': return '\uD83D\uDC96';
      default: return '\u2764\uFE0F';
    }
  };

  return (
    <View style={styles.container} pointerEvents="box-none">
      {/* Top bar - Room name, coins, level */}
      <View style={styles.topBar}>
        {onBack && (
          <TouchableOpacity style={styles.backButton} onPress={onBack}>
            <Text style={styles.backText}>{'\u2190'}</Text>
          </TouchableOpacity>
        )}
        <View style={styles.roomBadge}>
          <Text style={styles.roomName}>{roomName}</Text>
        </View>
        <View style={styles.topRight}>
          <View style={styles.levelBadge}>
            <Text style={styles.levelText}>Lv.{level}</Text>
          </View>
          <View style={styles.coinBadge}>
            <Text style={styles.coinEmoji}>{'\u20B9'}</Text>
            <Text style={styles.coinText}>{coins}</Text>
          </View>
        </View>
      </View>

      {/* Coin animation overlay */}
      {showCoinAnimation && coinDelta && (
        <Animated.View style={[styles.coinPopup, {
          opacity: coinAnim,
          transform: [{
            translateY: coinAnim.interpolate({
              inputRange: [0, 1],
              outputRange: [0, -30],
            }),
          }],
        }]}>
          <Text style={styles.coinPopupText}>+{'\u20B9'}{coinDelta}</Text>
        </Animated.View>
      )}

      {/* Stat bars */}
      <View style={styles.statsContainer}>
        {Object.entries(stats).map(([key, value]) => (
          <View key={key} style={styles.statRow}>
            <Text style={styles.statEmoji}>{getStatEmoji(key)}</Text>
            <View style={styles.statBarBg}>
              <View style={[styles.statBarFill, {
                width: `${Math.max(5, value)}%`,
                backgroundColor: getStatColor(value),
              }]} />
            </View>
          </View>
        ))}
      </View>

      {/* XP bar */}
      <View style={styles.xpContainer}>
        <View style={styles.xpBarBg}>
          <View style={[styles.xpBarFill, {
            width: `${Math.max(2, (xp / xpToNext) * 100)}%`,
          }]} />
        </View>
        <Text style={styles.xpText}>{xp}/{xpToNext} XP</Text>
      </View>

      {/* NPC Dialogue bubble */}
      {showDialogue && activeNPCDialogue && (
        <Animated.View style={[styles.dialogueBubble, {
          opacity: dialogueAnim,
          transform: [{
            scale: dialogueAnim.interpolate({
              inputRange: [0, 1],
              outputRange: [0.8, 1],
            }),
          }],
        }]}>
          <Text style={styles.dialogueName}>{activeNPCDialogue.npcName}</Text>
          <Text style={styles.dialogueText}>{activeNPCDialogue.text}</Text>
        </Animated.View>
      )}

      {/* Tap hint */}
      <View style={styles.hintContainer}>
        <Text style={styles.hintText}>Tap to move {'\u2022'} Tap objects to interact</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    ...StyleSheet.absoluteFillObject,
    zIndex: 10,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 12,
    paddingTop: 8,
  },
  backButton: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: 'rgba(255,255,255,0.9)',
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 3,
  },
  backText: {
    fontSize: 20,
    color: '#333',
  },
  roomBadge: {
    backgroundColor: 'rgba(255,255,255,0.9)',
    paddingHorizontal: 14,
    paddingVertical: 5,
    borderRadius: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.12,
    shadowRadius: 4,
    elevation: 3,
  },
  roomName: {
    fontSize: 14,
    fontWeight: '700',
    color: '#333',
  },
  topRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  levelBadge: {
    backgroundColor: '#b388ff',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  levelText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#fff',
  },
  coinBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'rgba(255,217,61,0.95)',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    gap: 2,
  },
  coinEmoji: {
    fontSize: 14,
    fontWeight: '700',
  },
  coinText: {
    fontSize: 14,
    fontWeight: '700',
    color: '#333',
  },
  coinPopup: {
    position: 'absolute',
    top: 50,
    alignSelf: 'center',
    backgroundColor: 'rgba(255,217,61,0.95)',
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 16,
  },
  coinPopupText: {
    fontSize: 18,
    fontWeight: '800',
    color: '#333',
  },
  statsContainer: {
    position: 'absolute',
    top: 50,
    left: 10,
    gap: 4,
  },
  statRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  statEmoji: {
    fontSize: 12,
    width: 18,
    textAlign: 'center',
  },
  statBarBg: {
    width: 55,
    height: 7,
    borderRadius: 4,
    backgroundColor: 'rgba(0,0,0,0.1)',
    overflow: 'hidden',
  },
  statBarFill: {
    height: '100%',
    borderRadius: 4,
  },
  xpContainer: {
    position: 'absolute',
    top: 50,
    right: 10,
    alignItems: 'flex-end',
    gap: 2,
  },
  xpBarBg: {
    width: 60,
    height: 6,
    borderRadius: 3,
    backgroundColor: 'rgba(0,0,0,0.1)',
    overflow: 'hidden',
  },
  xpBarFill: {
    height: '100%',
    borderRadius: 3,
    backgroundColor: '#b388ff',
  },
  xpText: {
    fontSize: 9,
    color: 'rgba(0,0,0,0.5)',
    fontWeight: '600',
  },
  dialogueBubble: {
    position: 'absolute',
    bottom: 60,
    alignSelf: 'center',
    backgroundColor: 'rgba(255,255,255,0.95)',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 16,
    maxWidth: '80%',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.15,
    shadowRadius: 6,
    elevation: 5,
  },
  dialogueName: {
    fontSize: 12,
    fontWeight: '700',
    color: '#ff6b9d',
    marginBottom: 2,
  },
  dialogueText: {
    fontSize: 14,
    color: '#333',
    lineHeight: 20,
  },
  hintContainer: {
    position: 'absolute',
    bottom: 8,
    alignSelf: 'center',
  },
  hintText: {
    fontSize: 10,
    color: 'rgba(0,0,0,0.35)',
    fontWeight: '500',
  },
});
