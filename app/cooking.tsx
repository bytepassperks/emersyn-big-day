/**
 * Kitchen/Cooking - 3D kitchen with cooking activities
 */
import React, { useState, useCallback } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity, ScrollView } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { getRandomEncouragement } from '@/lib/helpers';
import GameScene from '@/components/GameScene';
import GameHUD from '@/components/GameHUD';
import { InteractableInfo } from '@/engine/RoomBuilder';
import { NPCCharacter } from '@/engine/NPCCharacter';
import { getRecipesByCategory } from '@/content/recipes';
import { Recipe } from '@/lib/types';
import { GameEngine } from '@/engine/GameEngine';

const { width: SW } = Dimensions.get('window');

const INTERACTION_REWARDS: Record<string, { stats: Record<string, number>; coins: number; xp: number; sticker?: string }> = {
  stove: { stats: { hunger: 20, fun: 10 }, coins: 5, xp: 10, sticker: 'sticker_first_cook' },
  fridge: { stats: { hunger: 10 }, coins: 3, xp: 5 },
  sink: { stats: { cleanliness: 10 }, coins: 2, xp: 3 },
  table: { stats: { hunger: 15, fun: 5 }, coins: 4, xp: 8 },
  counter: { stats: { fun: 5 }, coins: 2, xp: 3 },
  hanging_pots: { stats: { fun: 3 }, coins: 1, xp: 2 },
};

type Phase = 'scene' | 'cooking' | 'done';

export default function Cooking() {
  const { coins, stats, xp, level, updateStats, addCoins, addXP, addStars, unlockRecipe, earnSticker, saveGame } = useGameStore();
  const [showCoinAnim, setShowCoinAnim] = useState(false);
  const [coinDelta, setCoinDelta] = useState(0);
  const [npcDialogue, setNpcDialogue] = useState<{ npcName: string; text: string } | null>(null);
  const [phase, setPhase] = useState<Phase>('scene');
  const [selectedRecipe, setSelectedRecipe] = useState<Recipe | null>(null);
  const [currentStep, setCurrentStep] = useState(0);
  const [engineRef, setEngineRef] = useState<GameEngine | null>(null);

  const xpToNext = level * 100;

  const handleInteract = useCallback((interactable: InteractableInfo) => {
    if (interactable.id === 'stove') {
      setPhase('cooking');
      return;
    }
    const reward = INTERACTION_REWARDS[interactable.id] || { stats: { fun: 3 }, coins: 1, xp: 2 };
    updateStats(reward.stats);
    if (reward.coins > 0) {
      addCoins(reward.coins);
      setCoinDelta(reward.coins);
      setShowCoinAnim(true);
      setTimeout(() => setShowCoinAnim(false), 1000);
    }
    addXP(reward.xp);
    if (reward.sticker) earnSticker(reward.sticker);
    saveGame();
  }, [updateStats, addCoins, addXP, earnSticker, saveGame]);

  const handleNPCTap = useCallback((npc: NPCCharacter) => {
    setNpcDialogue({ npcName: npc.name, text: npc.currentDialogue });
  }, []);

  const startCooking = (recipe: Recipe) => {
    setSelectedRecipe(recipe);
    setCurrentStep(0);
    if (engineRef) engineRef.character.setAnimation('cook');
  };

  const nextStep = async () => {
    if (!selectedRecipe) return;
    if (currentStep < selectedRecipe.steps.length - 1) {
      setCurrentStep(prev => prev + 1);
    } else {
      updateStats({ hunger: selectedRecipe.healthBonus, fun: selectedRecipe.happinessBonus });
      addCoins(selectedRecipe.coinReward);
      addXP(15);
      addStars(1);
      unlockRecipe(selectedRecipe.id);
      if (selectedRecipe.id.includes('pizza')) earnSticker('sticker_pizza_master');
      if (selectedRecipe.category === 'dessert') earnSticker('sticker_dessert_queen');
      if (engineRef) engineRef.character.setAnimation('happy');
      setPhase('done');
      await saveGame();
    }
  };

  if (phase === 'cooking' && !selectedRecipe) {
    return (
      <View style={styles.container}>
        <View style={styles.menuHeader}>
          <TouchableOpacity style={styles.backBtn} onPress={() => setPhase('scene')}>
            <Text style={styles.backBtnText}>{'\u2190'} Back</Text>
          </TouchableOpacity>
          <Text style={styles.menuTitle}>What shall we cook?</Text>
        </View>
        <ScrollView style={styles.recipeList} contentContainerStyle={{ paddingBottom: 30 }}>
          {['breakfast', 'lunch', 'snack', 'dinner', 'dessert', 'drink'].map(cat => {
            const catRecipes = getRecipesByCategory(cat as Recipe['category']).filter(r => r.unlockLevel <= level);
            if (catRecipes.length === 0) return null;
            return (
              <View key={cat}>
                <Text style={styles.catTitle}>{cat.charAt(0).toUpperCase() + cat.slice(1)}</Text>
                {catRecipes.map(recipe => (
                  <TouchableOpacity key={recipe.id} style={styles.recipeCard} onPress={() => startCooking(recipe)}>
                    <Text style={styles.recipeEmoji}>{recipe.emoji}</Text>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.recipeName}>{recipe.name}</Text>
                      <Text style={styles.recipeSteps}>{recipe.steps.length} steps</Text>
                    </View>
                    <Text style={styles.recipeCoin}>{'\u20B9'}{recipe.coinReward}</Text>
                  </TouchableOpacity>
                ))}
              </View>
            );
          })}
        </ScrollView>
      </View>
    );
  }

  if (phase === 'cooking' && selectedRecipe) {
    return (
      <View style={styles.container}>
        <View style={styles.cookingView}>
          <Text style={styles.cookingTitle}>{selectedRecipe.emoji} {selectedRecipe.name}</Text>
          <View style={styles.stepCard}>
            <Text style={styles.stepNum}>Step {currentStep + 1} of {selectedRecipe.steps.length}</Text>
            <Text style={styles.stepText}>{selectedRecipe.steps[currentStep]}</Text>
          </View>
          <View style={styles.dotsRow}>
            {selectedRecipe.steps.map((_, i) => (
              <View key={i} style={[styles.dot, i <= currentStep && styles.dotActive]} />
            ))}
          </View>
          <TouchableOpacity style={styles.nextBtn} onPress={nextStep}>
            <Text style={styles.nextBtnText}>
              {currentStep < selectedRecipe.steps.length - 1 ? 'Next Step' : 'Finish!'}
            </Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  if (phase === 'done' && selectedRecipe) {
    return (
      <View style={styles.container}>
        <View style={styles.doneView}>
          <Text style={styles.doneEmoji}>{selectedRecipe.emoji}</Text>
          <Text style={styles.doneName}>{selectedRecipe.name}</Text>
          <Text style={styles.doneMsg}>{getRandomEncouragement()}</Text>
          <View style={styles.rewardBox}>
            <Text style={styles.rewardLine}>+{'\u20B9'}{selectedRecipe.coinReward}</Text>
            <Text style={styles.rewardLine}>+1 Star</Text>
            <Text style={styles.rewardLine}>+{selectedRecipe.healthBonus} Health</Text>
          </View>
          <TouchableOpacity style={styles.nextBtn} onPress={() => { setPhase('scene'); setSelectedRecipe(null); }}>
            <Text style={styles.nextBtnText}>Back to Kitchen</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.sceneContainer}>
        <GameScene
          roomType="kitchen"
          onInteract={handleInteract}
          onNPCTap={handleNPCTap}
          onEngineReady={setEngineRef}
          height={SW * 0.85}
        />
        <GameHUD
          coins={coins}
          stats={stats}
          level={level}
          xp={xp % xpToNext}
          xpToNext={xpToNext}
          roomName="Kitchen"
          onBack={() => router.back()}
          showCoinAnimation={showCoinAnim}
          coinDelta={coinDelta}
          activeNPCDialogue={npcDialogue}
        />
      </View>
      <View style={styles.tip}>
        <Text style={styles.tipText}>Tap the stove to cook, fridge to snack, sink to clean!</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF0D4' },
  sceneContainer: { flex: 1, position: 'relative' },
  tip: { padding: 12, backgroundColor: '#fff', alignItems: 'center' },
  tipText: { fontSize: 12, color: '#999', fontWeight: '500' },
  menuHeader: { flexDirection: 'row', alignItems: 'center', padding: 16, gap: 12, paddingTop: 50 },
  backBtn: { padding: 8, backgroundColor: '#fff', borderRadius: 12 },
  backBtnText: { fontSize: 14, fontWeight: '700', color: '#333' },
  menuTitle: { fontSize: 20, fontWeight: '800', color: '#333' },
  recipeList: { flex: 1, paddingHorizontal: 16 },
  catTitle: { fontSize: 16, fontWeight: '800', color: '#ff9f43', marginTop: 16, marginBottom: 8 },
  recipeCard: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
    padding: 14, borderRadius: 16, marginBottom: 8, gap: 12,
  },
  recipeEmoji: { fontSize: 32 },
  recipeName: { fontSize: 16, fontWeight: '700', color: '#333' },
  recipeSteps: { fontSize: 12, color: '#999', marginTop: 2 },
  recipeCoin: { fontSize: 16, fontWeight: '800', color: '#ff9f43' },
  cookingView: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  cookingTitle: { fontSize: 26, fontWeight: '800', color: '#333', marginBottom: 20 },
  stepCard: { backgroundColor: '#fff', padding: 24, borderRadius: 20, width: '100%', alignItems: 'center' },
  stepNum: { fontSize: 13, fontWeight: '600', color: '#999', marginBottom: 8 },
  stepText: { fontSize: 20, fontWeight: '800', color: '#333', textAlign: 'center' },
  dotsRow: { flexDirection: 'row', gap: 8, marginTop: 20 },
  dot: { width: 12, height: 12, borderRadius: 6, backgroundColor: '#ddd' },
  dotActive: { backgroundColor: '#ff9f43' },
  nextBtn: { marginTop: 24, backgroundColor: '#ff9f43', paddingHorizontal: 30, paddingVertical: 14, borderRadius: 16 },
  nextBtnText: { fontSize: 18, fontWeight: '800', color: '#fff' },
  doneView: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 },
  doneEmoji: { fontSize: 80 },
  doneName: { fontSize: 28, fontWeight: '800', color: '#333', marginTop: 12 },
  doneMsg: { fontSize: 16, color: '#999', marginTop: 8 },
  rewardBox: { backgroundColor: '#fff', padding: 20, borderRadius: 18, marginTop: 20, width: '100%', gap: 8 },
  rewardLine: { fontSize: 18, fontWeight: '700', color: '#333' },
});
