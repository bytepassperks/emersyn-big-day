/**
 * Shop - Shopping interface with categories and items
 */
import React, { useState, useMemo } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert, Dimensions } from 'react-native';
import { router } from 'expo-router';
import { useGameStore } from '@/store/gameStore';
import { shopCatalog } from '@/content/shopCatalog';

const { width: SW } = Dimensions.get('window');

const CATEGORIES = ['all', 'bags', 'unicorn', 'clothes_top', 'clothes_bottom', 'clothes_dress', 'shoes', 'hair', 'accessories', 'food_ingredients', 'food_snacks', 'food_drinks', 'toys', 'decor'];

export default function Shop() {
  const { coins, level, inventory, addCoins, addXP, updateStats, purchaseItem, saveGame } = useGameStore();
  const [selectedCat, setSelectedCat] = useState('all');

  const items = useMemo(() => {
    const available = shopCatalog.filter(item => item.unlockLevel <= level);
    if (selectedCat === 'all') return available;
    return available.filter(item => item.category === selectedCat);
  }, [selectedCat, level]);

  const handleBuy = async (item: typeof shopCatalog[0]) => {
    if (coins < item.price) {
      Alert.alert('Not enough coins!', `You need ${'\u20B9'}${item.price - coins} more coins.`);
      return;
    }
    const owned = inventory.some(i => i.id === item.id);
    if (owned) {
      Alert.alert('Already owned!', 'You already have this item.');
      return;
    }
    const success = purchaseItem(item.id, item.category, item.price);
    if (!success) {
      Alert.alert('Purchase failed', 'Something went wrong. Please try again.');
      return;
    }
    addXP(5);
    if (item.category === 'food_ingredients') {
      updateStats({ hunger: 15 });
    }
    await saveGame();
    Alert.alert('Purchased!', `You got ${item.name}!`);
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>{'\u2190'}</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Shop</Text>
        <View style={styles.coinBadge}>
          <Text style={styles.coinText}>{'\u20B9'}{coins}</Text>
        </View>
      </View>

      <ScrollView horizontal style={styles.catBar} showsHorizontalScrollIndicator={false}>
        {CATEGORIES.map(cat => (
          <TouchableOpacity
            key={cat}
            style={[styles.catBtn, selectedCat === cat && styles.catBtnActive]}
            onPress={() => setSelectedCat(cat)}
          >
            <Text style={[styles.catBtnText, selectedCat === cat && styles.catBtnTextActive]}>
              {cat.charAt(0).toUpperCase() + cat.slice(1)}
            </Text>
          </TouchableOpacity>
        ))}
      </ScrollView>

      <ScrollView style={styles.itemGrid} contentContainerStyle={styles.gridContent}>
        {items.map(item => {
          const owned = inventory.some(i => i.id === item.id);
          return (
            <TouchableOpacity
              key={item.id}
              style={[styles.itemCard, owned && styles.itemOwned]}
              onPress={() => handleBuy(item)}
              disabled={owned}
            >
              <Text style={styles.itemEmoji}>{item.emoji}</Text>
              <Text style={styles.itemName} numberOfLines={1}>{item.name}</Text>
              <Text style={styles.itemPrice}>
                {owned ? 'Owned' : `${'\u20B9'}${item.price}`}
              </Text>
            </TouchableOpacity>
          );
        })}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8E1' },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: 16, paddingTop: 50 },
  backBtn: { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fff', alignItems: 'center', justifyContent: 'center' },
  backBtnText: { fontSize: 20, fontWeight: '800' },
  title: { fontSize: 22, fontWeight: '800', color: '#333' },
  coinBadge: { backgroundColor: '#ff9f43', paddingHorizontal: 14, paddingVertical: 6, borderRadius: 14 },
  coinText: { fontSize: 16, fontWeight: '800', color: '#fff' },
  catBar: { paddingHorizontal: 12, maxHeight: 44, marginBottom: 8 },
  catBtn: { paddingHorizontal: 16, paddingVertical: 8, borderRadius: 14, backgroundColor: '#fff', marginRight: 8 },
  catBtnActive: { backgroundColor: '#ff9f43' },
  catBtnText: { fontSize: 13, fontWeight: '700', color: '#666' },
  catBtnTextActive: { color: '#fff' },
  itemGrid: { flex: 1, paddingHorizontal: 12 },
  gridContent: { flexDirection: 'row', flexWrap: 'wrap', gap: 10, paddingBottom: 30 },
  itemCard: {
    width: (SW - 44) / 3, backgroundColor: '#fff', borderRadius: 16, padding: 12, alignItems: 'center',
  },
  itemOwned: { opacity: 0.5 },
  itemEmoji: { fontSize: 36 },
  itemName: { fontSize: 12, fontWeight: '700', color: '#333', marginTop: 4, textAlign: 'center' },
  itemPrice: { fontSize: 13, fontWeight: '800', color: '#ff9f43', marginTop: 4 },
});
