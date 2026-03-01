import React, { useState } from 'react';
import { View, Text, StyleSheet, Alert, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { CoinDisplay } from '@/components/CoinDisplay';
import { Colors } from '@/lib/colors';
import { getRandomNPCComment, generateFakeLikes, getRandomEncouragement } from '@/lib/helpers';

type StudioPhase = 'menu' | 'dressup' | 'makeup' | 'recording' | 'posting' | 'feed';

interface NPCComment {
  name: string;
  comment: string;
  emoji: string;
  likes: number;
}

export default function Studio() {
  const router = useRouter();
  const {
    coins, stars, inventory, equippedOutfit, stats,
    updateStats, addCoins, addXP, addStars, earnSticker, createReel, saveGame,
  } = useGameStore();
  const [phase, setPhase] = useState<StudioPhase>('menu');
  const [npcComments, setNpcComments] = useState<NPCComment[]>([]);
  const [reelPosted, setReelPosted] = useState(false);

  const handleDressUp = () => {
    updateStats({ fun: 15 });
    addCoins(5);
    addXP(8);
    earnSticker('sticker_makeover');
    Alert.alert('Looking fabulous! 💖', getRandomEncouragement());
    setPhase('menu');
    saveGame();
  };

  const handleMakeup = () => {
    updateStats({ fun: 15 });
    addCoins(5);
    addXP(8);
    Alert.alert('So pretty! 💄', getRandomEncouragement());
    setPhase('menu');
    saveGame();
  };

  const handleRecord = () => {
    setPhase('recording');
    setTimeout(() => {
      createReel({ outfitHash: 'reel_' + Date.now(), likes: 0, coins: 0, postedAt: Date.now() });
      earnSticker('sticker_first_reel');
      setPhase('posting');
    }, 2000);
  };

  const handlePost = async () => {
    const likes = generateFakeLikes();
    const comments: NPCComment[] = Array.from({ length: 5 }, () => ({
      ...getRandomNPCComment(),
      likes: Math.floor(Math.random() * 20) + 1,
    }));

    setNpcComments(comments);
    updateStats({ popularity: 15, fun: 10 });
    addCoins(likes);
    addXP(20);
    addStars(2);
    earnSticker('sticker_popular');

    setReelPosted(true);
    setPhase('feed');
    await saveGame();
  };

  if (phase === 'dressup') {
    return (
      <ScreenWrapper title="Dress Up" emoji="👗" bgColor={Colors.bgStudio} showBack={false}>
        <View style={styles.dressUpArea}>
          <Text style={styles.bigEmoji}>👧</Text>
          <Text style={styles.phaseTitle}>Choose Your Look!</Text>

          <View style={styles.outfitGrid}>
            {['👗', '👚', '👖', '🩱', '👟', '👠', '🎀', '👜'].map((emoji, i) => (
              <View key={i} style={styles.outfitSlot}>
                <Text style={styles.outfitEmoji}>{emoji}</Text>
              </View>
            ))}
          </View>

          <GameButton
            title="Save Look!"
            emoji="💖"
            onPress={handleDressUp}
            variant="primary"
            size="large"
            style={{ marginTop: 20 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'makeup') {
    return (
      <ScreenWrapper title="Makeup" emoji="💄" bgColor={Colors.bgStudio} showBack={false}>
        <View style={styles.dressUpArea}>
          <Text style={styles.bigEmoji}>💄</Text>
          <Text style={styles.phaseTitle}>Cartoon-Safe Makeup!</Text>

          <View style={styles.makeupGrid}>
            {['💄', '💅', '👁️', '🌸', '✨', '🌈'].map((emoji, i) => (
              <View key={i} style={styles.makeupSlot}>
                <Text style={styles.makeupEmoji}>{emoji}</Text>
                <Text style={styles.makeupLabel}>
                  {['Lip Gloss', 'Nails', 'Sparkle', 'Blush', 'Glitter', 'Colors'][i]}
                </Text>
              </View>
            ))}
          </View>

          <GameButton
            title="Done! 🎉"
            onPress={handleMakeup}
            variant="accent"
            size="large"
            style={{ marginTop: 20 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'recording') {
    return (
      <ScreenWrapper title="Recording..." emoji="🎬" bgColor={Colors.bgStudio} showBack={false} scrollable={false}>
        <View style={styles.recordingArea}>
          <Text style={styles.bigEmoji}>📸</Text>
          <Text style={styles.phaseTitle}>Recording your reel...</Text>
          <Text style={styles.recordingDots}>🔴 REC</Text>
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'posting') {
    return (
      <ScreenWrapper title="Post Reel" emoji="📱" bgColor={Colors.bgStudio} showBack={false}>
        <View style={styles.postArea}>
          <Text style={styles.bigEmoji}>📱</Text>
          <Text style={styles.phaseTitle}>Ready to post?</Text>
          <Text style={styles.postDesc}>Share your reel with in-game friends!</Text>

          <GameButton
            title="Post to Feed! 🌟"
            onPress={handlePost}
            variant="primary"
            size="large"
            style={{ marginTop: 20 }}
          />
          <GameButton
            title="Discard"
            onPress={() => setPhase('menu')}
            variant="outline"
            size="small"
            style={{ marginTop: 10 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'feed') {
    return (
      <ScreenWrapper title="Your Feed" emoji="📱" bgColor={Colors.bgStudio}>
        <View style={styles.feedArea}>
          <Text style={styles.feedTitle}>Your reel is live! 🎉</Text>

          {npcComments.map((comment, i) => (
            <View key={i} style={styles.commentCard}>
              <Text style={styles.commentEmoji}>{comment.emoji}</Text>
              <View style={styles.commentContent}>
                <Text style={styles.commentName}>{comment.name}</Text>
                <Text style={styles.commentText}>{comment.comment}</Text>
              </View>
              <Text style={styles.commentLikes}>❤️ {comment.likes}</Text>
            </View>
          ))}

          <GameButton
            title="Back to Studio"
            emoji="🎬"
            onPress={() => { setPhase('menu'); setReelPosted(false); }}
            variant="primary"
            size="medium"
            style={{ marginTop: 16 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  // Main menu
  return (
    <ScreenWrapper title="Weekend Studio" emoji="🎬" bgColor={Colors.bgStudio}>
      <View style={styles.headerArea}>
        <CoinDisplay coins={coins} stars={stars} />
        <Text style={styles.popText}>💖 Popularity: {Math.round(stats.popularity)}</Text>
      </View>

      <View style={styles.characterArea}>
        <Text style={styles.bigEmoji}>👧</Text>
        <Text style={styles.studioDesc}>Create, dress up, and share!</Text>
      </View>

      <Text style={styles.sectionTitle}>✨ Create</Text>
      <View style={styles.actionGrid}>
        <GameButton
          title="Dress Up"
          emoji="👗"
          onPress={() => setPhase('dressup')}
          variant="primary"
          size="large"
          style={styles.actionBtn}
        />
        <GameButton
          title="Makeup"
          emoji="💄"
          onPress={() => setPhase('makeup')}
          variant="secondary"
          size="large"
          style={styles.actionBtn}
        />
        <GameButton
          title="Record Reel"
          emoji="🎬"
          onPress={handleRecord}
          variant="accent"
          size="large"
          style={styles.actionBtn}
        />
      </View>
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  headerArea: { alignItems: 'center', paddingVertical: 12, gap: 6 },
  popText: { fontSize: 14, fontWeight: '700', color: Colors.pink },
  characterArea: { alignItems: 'center', paddingVertical: 16 },
  bigEmoji: { fontSize: 80 },
  studioDesc: { fontSize: 14, color: Colors.gray500, marginTop: 8 },
  sectionTitle: {
    fontSize: 20, fontWeight: '800', color: Colors.dark,
    paddingHorizontal: 16, paddingTop: 12, paddingBottom: 8,
  },
  actionGrid: { paddingHorizontal: 16, gap: 10 },
  actionBtn: { width: '100%' },
  // Dress up
  dressUpArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  phaseTitle: { fontSize: 24, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  outfitGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginTop: 20, justifyContent: 'center' },
  outfitSlot: {
    width: 70, height: 70, borderRadius: 18, backgroundColor: Colors.white,
    justifyContent: 'center', alignItems: 'center', borderWidth: 2, borderColor: Colors.pinkLight,
  },
  outfitEmoji: { fontSize: 32 },
  // Makeup
  makeupGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginTop: 20, justifyContent: 'center' },
  makeupSlot: {
    width: 90, height: 80, borderRadius: 18, backgroundColor: Colors.white,
    justifyContent: 'center', alignItems: 'center', borderWidth: 2, borderColor: Colors.pinkLight,
    padding: 8,
  },
  makeupEmoji: { fontSize: 28 },
  makeupLabel: { fontSize: 10, fontWeight: '700', color: Colors.gray500, marginTop: 4 },
  // Recording
  recordingArea: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  recordingDots: { fontSize: 20, fontWeight: '800', color: Colors.coral, marginTop: 16 },
  // Posting
  postArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 40 },
  postDesc: { fontSize: 14, color: Colors.gray500, marginTop: 8 },
  // Feed
  feedArea: { paddingHorizontal: 16, paddingTop: 16 },
  feedTitle: { fontSize: 22, fontWeight: '800', color: Colors.dark, textAlign: 'center', marginBottom: 16 },
  commentCard: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.white,
    padding: 12, borderRadius: 14, marginVertical: 4, gap: 10,
    borderWidth: 1, borderColor: Colors.gray200,
  },
  commentEmoji: { fontSize: 28 },
  commentContent: { flex: 1 },
  commentName: { fontSize: 14, fontWeight: '700', color: Colors.dark },
  commentText: { fontSize: 13, color: Colors.gray500, marginTop: 2 },
  commentLikes: { fontSize: 13, fontWeight: '700', color: Colors.coral },
});
