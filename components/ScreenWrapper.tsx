import React from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, SafeAreaView } from 'react-native';
import { useRouter } from 'expo-router';
import { Colors } from '@/lib/colors';

interface ScreenWrapperProps {
  title: string;
  emoji?: string;
  bgColor?: string;
  showBack?: boolean;
  rightContent?: React.ReactNode;
  children: React.ReactNode;
  scrollable?: boolean;
}

export const ScreenWrapper: React.FC<ScreenWrapperProps> = ({
  title,
  emoji,
  bgColor = Colors.pinkLight,
  showBack = true,
  rightContent,
  children,
  scrollable = true,
}) => {
  const router = useRouter();

  return (
    <SafeAreaView style={[styles.safe, { backgroundColor: bgColor }]}>
      <View style={styles.header}>
        {showBack ? (
          <TouchableOpacity style={styles.backBtn} onPress={() => router.back()}>
            <Text style={styles.backText}>← Back</Text>
          </TouchableOpacity>
        ) : (
          <View style={styles.backPlaceholder} />
        )}
        <View style={styles.titleContainer}>
          {emoji && <Text style={styles.titleEmoji}>{emoji}</Text>}
          <Text style={styles.title}>{title}</Text>
        </View>
        {rightContent ?? <View style={styles.backPlaceholder} />}
      </View>
      {scrollable ? (
        <ScrollView
          style={styles.content}
          contentContainerStyle={styles.contentContainer}
          showsVerticalScrollIndicator={false}
        >
          {children}
        </ScrollView>
      ) : (
        <View style={[styles.content, styles.contentContainer, { flex: 1 }]}>
          {children}
        </View>
      )}
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  safe: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: Colors.overlayLight,
  },
  backBtn: {
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: 12,
    backgroundColor: Colors.white,
  },
  backText: {
    fontSize: 14,
    fontWeight: '700',
    color: Colors.pink,
  },
  backPlaceholder: {
    width: 70,
  },
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  titleEmoji: {
    fontSize: 22,
  },
  title: {
    fontSize: 18,
    fontWeight: '800',
    color: Colors.dark,
  },
  content: {
    flex: 1,
  },
  contentContainer: {
    paddingBottom: 32,
  },
});
