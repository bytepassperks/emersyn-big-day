import React, { useState } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Alert, ScrollView } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { GameButton } from '@/components/GameButton';
import { Colors } from '@/lib/colors';
import { recipes, getRecipesByCategory } from '@/content/recipes';
import { getRandomEncouragement } from '@/lib/helpers';
import { Recipe } from '@/lib/types';

type CookingPhase = 'menu' | 'cooking' | 'done';

export default function Cooking() {
  const { coins, recipeBook, level, updateStats, addCoins, addXP, addStars, unlockRecipe, earnSticker, saveGame } = useGameStore();
  const [selectedCategory, setSelectedCategory] = useState<string>('lunch');
  const [selectedRecipe, setSelectedRecipe] = useState<Recipe | null>(null);
  const [phase, setPhase] = useState<CookingPhase>('menu');
  const [currentStep, setCurrentStep] = useState(0);

  const categories = [
    { id: 'breakfast', name: 'Breakfast', emoji: '🥞' },
    { id: 'lunch', name: 'Lunch', emoji: '🍕' },
    { id: 'snack', name: 'Snacks', emoji: '🍿' },
    { id: 'dinner', name: 'Dinner', emoji: '🍽️' },
    { id: 'dessert', name: 'Desserts', emoji: '🧁' },
    { id: 'drink', name: 'Drinks', emoji: '🥤' },
  ];

  const availableRecipes = getRecipesByCategory(selectedCategory as Recipe['category'])
    .filter((r) => r.unlockLevel <= level);

  const startCooking = (recipe: Recipe) => {
    setSelectedRecipe(recipe);
    setCurrentStep(0);
    setPhase('cooking');
  };

  const nextStep = async () => {
    if (!selectedRecipe) return;

    if (currentStep < selectedRecipe.steps.length - 1) {
      setCurrentStep((prev) => prev + 1);
    } else {
      // Cooking complete!
      updateStats({ hunger: selectedRecipe.healthBonus, fun: selectedRecipe.happinessBonus });
      addCoins(selectedRecipe.coinReward);
      addXP(15);
      addStars(1);
      unlockRecipe(selectedRecipe.id);

      if (!recipeBook.includes(selectedRecipe.id)) {
        earnSticker('sticker_first_cook');
      }
      if (selectedRecipe.id.includes('pizza')) earnSticker('sticker_pizza_master');
      if (selectedRecipe.id.includes('burger')) earnSticker('sticker_burger_stack');
      if (selectedRecipe.category === 'dessert') earnSticker('sticker_dessert_queen');

      setPhase('done');
      await saveGame();
    }
  };

  if (phase === 'cooking' && selectedRecipe) {
    return (
      <ScreenWrapper title="Cooking!" emoji="👩‍🍳" bgColor={Colors.bgKitchen} showBack={false}>
        <View style={styles.cookingArea}>
          <Text style={styles.recipeName}>{selectedRecipe.emoji} {selectedRecipe.name}</Text>

          <View style={styles.stepCard}>
            <Text style={styles.stepNumber}>Step {currentStep + 1} of {selectedRecipe.steps.length}</Text>
            <Text style={styles.stepText}>{selectedRecipe.steps[currentStep]}</Text>
          </View>

          {/* Progress dots */}
          <View style={styles.progressRow}>
            {selectedRecipe.steps.map((_, i) => (
              <View
                key={i}
                style={[styles.progressDot, i <= currentStep && styles.progressDotActive]}
              />
            ))}
          </View>

          {/* Ingredients display */}
          <View style={styles.ingredientsRow}>
            {selectedRecipe.ingredients.map((ing, i) => (
              <View key={i} style={styles.ingredientChip}>
                <Text style={styles.ingredientText}>{ing}</Text>
              </View>
            ))}
          </View>

          <GameButton
            title={currentStep < selectedRecipe.steps.length - 1 ? 'Next Step →' : 'Finish! 🎉'}
            onPress={nextStep}
            variant={currentStep < selectedRecipe.steps.length - 1 ? 'primary' : 'accent'}
            size="large"
            style={{ marginTop: 20 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  if (phase === 'done' && selectedRecipe) {
    return (
      <ScreenWrapper title="Yummy!" emoji="🎉" bgColor={Colors.bgKitchen} showBack={false}>
        <View style={styles.doneArea}>
          <Text style={styles.doneEmoji}>{selectedRecipe.emoji}</Text>
          <Text style={styles.doneName}>{selectedRecipe.name}</Text>
          <Text style={styles.doneSubtitle}>{getRandomEncouragement()}</Text>

          <View style={styles.rewardCard}>
            <Text style={styles.rewardText}>💰 +₹{selectedRecipe.coinReward}</Text>
            <Text style={styles.rewardText}>⭐ +1 Star</Text>
            <Text style={styles.rewardText}>❤️ +{selectedRecipe.healthBonus} Health</Text>
            <Text style={styles.rewardText}>🎉 +{selectedRecipe.happinessBonus} Fun</Text>
          </View>

          <GameButton
            title="Cook Again"
            emoji="🍳"
            onPress={() => setPhase('menu')}
            variant="primary"
            size="large"
            style={{ marginTop: 20 }}
          />
          <GameButton
            title="Go Home"
            emoji="🏠"
            onPress={() => {
              setPhase('menu');
              // Router back handled by ScreenWrapper
            }}
            variant="outline"
            size="medium"
            style={{ marginTop: 10 }}
          />
        </View>
      </ScreenWrapper>
    );
  }

  return (
    <ScreenWrapper title="Kitchen" emoji="🍳" bgColor={Colors.bgKitchen}>
      <View style={styles.characterArea}>
        <Text style={styles.characterEmoji}>👩‍🍳</Text>
        <Text style={styles.roomDesc}>What shall we cook today?</Text>
      </View>

      {/* Category Tabs */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.categoryScroll}>
        {categories.map((cat) => (
          <TouchableOpacity
            key={cat.id}
            style={[styles.categoryTab, selectedCategory === cat.id && styles.categoryTabActive]}
            onPress={() => setSelectedCategory(cat.id)}
          >
            <Text style={styles.categoryEmoji}>{cat.emoji}</Text>
            <Text style={[styles.categoryName, selectedCategory === cat.id && styles.categoryNameActive]}>
              {cat.name}
            </Text>
          </TouchableOpacity>
        ))}
      </ScrollView>

      {/* Recipes */}
      {availableRecipes.map((recipe) => (
        <TouchableOpacity
          key={recipe.id}
          style={styles.recipeCard}
          onPress={() => startCooking(recipe)}
          activeOpacity={0.7}
        >
          <Text style={styles.recipeEmoji}>{recipe.emoji}</Text>
          <View style={styles.recipeInfo}>
            <Text style={styles.recipeCardName}>{recipe.name}</Text>
            <Text style={styles.recipeDifficulty}>
              {'⭐'.repeat(recipe.difficulty)} · {recipe.steps.length} steps
            </Text>
          </View>
          <View style={styles.recipeReward}>
            <Text style={styles.recipeCoins}>₹{recipe.coinReward}</Text>
            {recipeBook.includes(recipe.id) && (
              <Text style={styles.recipeUnlocked}>✓</Text>
            )}
          </View>
        </TouchableOpacity>
      ))}
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  characterArea: { alignItems: 'center', paddingVertical: 16 },
  characterEmoji: { fontSize: 64 },
  roomDesc: { fontSize: 14, color: Colors.gray500, marginTop: 8 },
  categoryScroll: { paddingHorizontal: 12, marginBottom: 8 },
  categoryTab: {
    flexDirection: 'row', alignItems: 'center', paddingHorizontal: 14, paddingVertical: 8,
    borderRadius: 20, backgroundColor: Colors.white, marginHorizontal: 4, gap: 6,
    borderWidth: 2, borderColor: Colors.gray200,
  },
  categoryTabActive: { backgroundColor: Colors.pink, borderColor: Colors.pink },
  categoryEmoji: { fontSize: 18 },
  categoryName: { fontSize: 13, fontWeight: '700', color: Colors.gray500 },
  categoryNameActive: { color: Colors.white },
  recipeCard: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.white,
    marginHorizontal: 16, marginVertical: 4, padding: 14, borderRadius: 16,
    borderWidth: 2, borderColor: Colors.orangeLight,
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.06, shadowRadius: 3, elevation: 2,
  },
  recipeEmoji: { fontSize: 36, marginRight: 12 },
  recipeInfo: { flex: 1 },
  recipeCardName: { fontSize: 16, fontWeight: '700', color: Colors.dark },
  recipeDifficulty: { fontSize: 12, color: Colors.gray400, marginTop: 2 },
  recipeReward: { alignItems: 'flex-end' },
  recipeCoins: { fontSize: 16, fontWeight: '800', color: Colors.orange },
  recipeUnlocked: { fontSize: 14, fontWeight: '800', color: Colors.success, marginTop: 2 },
  // Cooking phase
  cookingArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 30 },
  recipeName: { fontSize: 26, fontWeight: '800', color: Colors.dark, marginBottom: 20 },
  stepCard: {
    backgroundColor: Colors.white, padding: 24, borderRadius: 20, width: '100%',
    alignItems: 'center', borderWidth: 2, borderColor: Colors.orangeLight,
    shadowColor: Colors.dark, shadowOffset: { width: 0, height: 3 }, shadowOpacity: 0.1, shadowRadius: 6, elevation: 4,
  },
  stepNumber: { fontSize: 13, fontWeight: '600', color: Colors.gray400, marginBottom: 8 },
  stepText: { fontSize: 22, fontWeight: '800', color: Colors.dark, textAlign: 'center' },
  progressRow: { flexDirection: 'row', gap: 8, marginTop: 20 },
  progressDot: { width: 12, height: 12, borderRadius: 6, backgroundColor: Colors.gray200 },
  progressDotActive: { backgroundColor: Colors.orange },
  ingredientsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 6, marginTop: 16, justifyContent: 'center' },
  ingredientChip: {
    backgroundColor: Colors.yellowLight, paddingHorizontal: 10, paddingVertical: 4, borderRadius: 10,
  },
  ingredientText: { fontSize: 12, fontWeight: '600', color: Colors.orange },
  // Done phase
  doneArea: { alignItems: 'center', paddingHorizontal: 20, paddingTop: 40 },
  doneEmoji: { fontSize: 80 },
  doneName: { fontSize: 28, fontWeight: '800', color: Colors.dark, marginTop: 12 },
  doneSubtitle: { fontSize: 16, color: Colors.gray500, marginTop: 8 },
  rewardCard: {
    backgroundColor: Colors.white, padding: 20, borderRadius: 18, marginTop: 20,
    width: '100%', borderWidth: 2, borderColor: Colors.yellowLight, gap: 8,
  },
  rewardText: { fontSize: 18, fontWeight: '700', color: Colors.dark },
});
