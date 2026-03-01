import React from 'react';
import { TouchableOpacity, Text, StyleSheet, View } from 'react-native';
import { Colors, getRarityColor } from '@/lib/colors';
import { ShopItem } from '@/lib/types';

interface ShopItemCardProps {
  item: ShopItem;
  owned: boolean;
  canAfford: boolean;
  onPress: () => void;
}

export const ShopItemCard: React.FC<ShopItemCardProps> = ({
  item,
  owned,
  canAfford,
  onPress,
}) => {
  const rarityColor = getRarityColor(item.rarity);

  return (
    <TouchableOpacity
      style={[
        styles.card,
        { borderColor: rarityColor },
        owned && styles.cardOwned,
      ]}
      onPress={onPress}
      disabled={owned}
      activeOpacity={0.7}
    >
      <View style={[styles.rarityBadge, { backgroundColor: rarityColor }]}>
        <Text style={styles.rarityText}>{item.rarity}</Text>
      </View>
      <Text style={styles.emoji}>{item.emoji}</Text>
      <Text style={styles.name} numberOfLines={1}>{item.name}</Text>
      {owned ? (
        <Text style={styles.ownedText}>Owned ✓</Text>
      ) : (
        <Text style={[styles.price, !canAfford && styles.priceUnaffordable]}>
          ₹{item.price}
        </Text>
      )}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    width: '30%',
    backgroundColor: Colors.white,
    borderRadius: 18,
    padding: 12,
    margin: '1.5%',
    alignItems: 'center',
    borderWidth: 2,
    shadowColor: Colors.dark,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 4,
    elevation: 3,
    position: 'relative',
  },
  cardOwned: {
    backgroundColor: Colors.mintLight,
    opacity: 0.8,
  },
  rarityBadge: {
    position: 'absolute',
    top: 4,
    right: 4,
    borderRadius: 6,
    paddingHorizontal: 5,
    paddingVertical: 1,
  },
  rarityText: {
    fontSize: 8,
    fontWeight: '700',
    color: Colors.white,
    textTransform: 'uppercase',
  },
  emoji: {
    fontSize: 36,
    marginTop: 8,
    marginBottom: 6,
  },
  name: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.dark,
    textAlign: 'center',
  },
  price: {
    fontSize: 14,
    fontWeight: '800',
    color: Colors.orange,
    marginTop: 4,
  },
  priceUnaffordable: {
    color: Colors.gray400,
  },
  ownedText: {
    fontSize: 12,
    fontWeight: '700',
    color: Colors.success,
    marginTop: 4,
  },
});
