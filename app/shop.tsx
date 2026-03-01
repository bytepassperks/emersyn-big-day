import React, { useState, useMemo } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useGameStore } from '@/store/gameStore';
import { ScreenWrapper } from '@/components/ScreenWrapper';
import { CoinDisplay } from '@/components/CoinDisplay';
import { ShopItemCard } from '@/components/ShopItemCard';
import { Colors } from '@/lib/colors';
import { shopCatalog, getItemsByCategory, getCategoryEmoji, getCategoryName } from '@/content/shopCatalog';
import { ShopCategory } from '@/lib/types';

const CATEGORIES: ShopCategory[] = [
  'clothes_top', 'clothes_bottom', 'clothes_dress', 'shoes', 'bags',
  'hair', 'accessories', 'unicorn', 'makeup', 'toys', 'books',
  'room_decor', 'food_ingredients', 'bug_safety',
];

export default function Shop() {
  const { coins, stars, level, inventory, purchaseItem, equipItem, earnSticker, saveGame } = useGameStore();
  const [selectedCategory, setSelectedCategory] = useState<ShopCategory>('clothes_dress');

  const items = useMemo(() => getItemsByCategory(selectedCategory), [selectedCategory]);
  const ownedIds = useMemo(() => new Set(inventory.map((i) => i.id)), [inventory]);

  const handlePurchase = async (itemId: string, price: number, name: string) => {
    if (coins < price) {
      Alert.alert('Not enough coins! 😢', `You need ₹${price} but only have ₹${coins}.`);
      return;
    }

    Alert.alert(
      `Buy ${name}?`,
      `This will cost ₹${price}`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Buy! 🛍️',
          onPress: async () => {
            purchaseItem(itemId);
            earnSticker('sticker_first_purchase');
            if (inventory.length >= 9) earnSticker('sticker_10_items');
            if (inventory.length >= 49) earnSticker('sticker_50_items');
            await saveGame();
            Alert.alert('Purchased! 🎉', `${name} is now yours!`);
          },
        },
      ]
    );
  };

  return (
    <ScreenWrapper title="Shop" emoji="🛍️" bgColor={Colors.bgShop}>
      <View style={styles.headerArea}>
        <CoinDisplay coins={coins} stars={stars} size="medium" />
        <Text style={styles.levelText}>Level {level} · {inventory.length} items owned</Text>
      </View>

      {/* Category Tabs */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.categoryScroll}>
        {CATEGORIES.map((cat) => (
          <TouchableOpacity
            key={cat}
            style={[styles.categoryTab, selectedCategory === cat && styles.categoryTabActive]}
            onPress={() => setSelectedCategory(cat)}
          >
            <Text style={styles.categoryEmoji}>{getCategoryEmoji[cat]}</Text>
            <Text style={[styles.categoryName, selectedCategory === cat && styles.categoryNameActive]}>
              {getCategoryName[cat]}
            </Text>
          </TouchableOpacity>
        ))}
      </ScrollView>

      {/* Items Grid */}
      <View style={styles.itemGrid}>
        {items.map((item) => (
          <ShopItemCard
            key={item.id}
            item={item}
            owned={ownedIds.has(item.id)}
            canAfford={coins >= item.price}
            onPress={() => handlePurchase(item.id, item.price, item.name)}
          />
        ))}
      </View>

      {items.length === 0 && (
        <View style={styles.emptyState}>
          <Text style={styles.emptyEmoji}>🔒</Text>
          <Text style={styles.emptyText}>No items available in this category yet.</Text>
          <Text style={styles.emptySubtext}>Level up to unlock more!</Text>
        </View>
      )}
    </ScreenWrapper>
  );
}

const styles = StyleSheet.create({
  headerArea: {
    alignItems: 'center', paddingVertical: 12, gap: 6,
  },
  levelText: {
    fontSize: 13, color: Colors.gray500, fontWeight: '600',
  },
  categoryScroll: {
    paddingHorizontal: 8, marginBottom: 8, maxHeight: 50,
  },
  categoryTab: {
    flexDirection: 'row', alignItems: 'center', paddingHorizontal: 12, paddingVertical: 6,
    borderRadius: 18, backgroundColor: Colors.white, marginHorizontal: 3, gap: 4,
    borderWidth: 2, borderColor: Colors.gray200,
  },
  categoryTabActive: {
    backgroundColor: Colors.pink, borderColor: Colors.pink,
  },
  categoryEmoji: { fontSize: 16 },
  categoryName: { fontSize: 11, fontWeight: '700', color: Colors.gray500 },
  categoryNameActive: { color: Colors.white },
  itemGrid: {
    flexDirection: 'row', flexWrap: 'wrap', paddingHorizontal: 4,
  },
  emptyState: {
    alignItems: 'center', paddingVertical: 40,
  },
  emptyEmoji: { fontSize: 48 },
  emptyText: { fontSize: 16, fontWeight: '700', color: Colors.gray400, marginTop: 12 },
  emptySubtext: { fontSize: 13, color: Colors.gray400, marginTop: 4 },
});
