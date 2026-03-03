using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// In-game shop system for purchasing clothing, furniture, food, decorations, and pets.
    /// Coin-based economy with level-gated items and category browsing.
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        public static ShopSystem Instance { get; private set; }

        [Header("Shop Inventory")]
        public ShopCategory[] Categories;

        [Header("Settings")]
        public float SaleChance = 0.1f;
        public float SaleDiscount = 0.3f;

        private Dictionary<string, bool> ownedItems = new Dictionary<string, bool>();
        private Dictionary<string, float> saleItems = new Dictionary<string, float>();

        public event Action<ShopItemData> OnItemPurchased;
        public event Action<ShopItemData> OnItemEquipped;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            GenerateDailySales();
        }

        private void GenerateDailySales()
        {
            saleItems.Clear();
            if (Categories == null) return;

            foreach (var cat in Categories)
            {
                if (cat.Items == null) continue;
                foreach (var item in cat.Items)
                {
                    if (UnityEngine.Random.value < SaleChance && !IsOwned(item.ItemId))
                    {
                        saleItems[item.ItemId] = SaleDiscount;
                    }
                }
            }
        }

        public bool PurchaseItem(string itemId)
        {
            if (IsOwned(itemId)) return false;

            ShopItemData item = FindItem(itemId);
            if (item == null) return false;

            var gm = GameManager.Instance;
            if (gm == null) return false;

            // Check level requirement
            if (gm.Level < item.UnlockLevel)
            {
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("sad");
                return false;
            }

            // Calculate price (with sale if applicable)
            int price = GetPrice(itemId);

            // Check currency
            if (item.CurrencyType == CurrencyType.Coins && gm.Coins < price) return false;
            if (item.CurrencyType == CurrencyType.Stars && gm.Stars < price) return false;

            // Deduct currency
            if (item.CurrencyType == CurrencyType.Coins) gm.Coins -= price;
            else gm.Stars -= price;

            // Mark as owned
            ownedItems[itemId] = true;

            // Effects
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("coin");
            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnSparkles(Vector3.up * 2f);

            // Achievement tracking
            if (AchievementSystem.Instance != null)
            {
                AchievementSystem.Instance.AddProgress("shopaholic");
                AchievementSystem.Instance.AddProgress($"collect_{item.Category.ToString().ToLower()}");
            }

            OnItemPurchased?.Invoke(item);
            return true;
        }

        public bool EquipItem(string itemId)
        {
            if (!IsOwned(itemId)) return false;
            ShopItemData item = FindItem(itemId);
            if (item == null) return false;

            OnItemEquipped?.Invoke(item);

            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("tap");

            return true;
        }

        public int GetPrice(string itemId)
        {
            ShopItemData item = FindItem(itemId);
            if (item == null) return 0;

            int basePrice = item.Price;
            if (saleItems.ContainsKey(itemId))
            {
                basePrice = Mathf.CeilToInt(basePrice * (1f - saleItems[itemId]));
            }
            return basePrice;
        }

        public bool IsOwned(string itemId) => ownedItems.ContainsKey(itemId) && ownedItems[itemId];

        public bool IsOnSale(string itemId) => saleItems.ContainsKey(itemId);

        public bool CanAfford(string itemId)
        {
            ShopItemData item = FindItem(itemId);
            if (item == null) return false;
            var gm = GameManager.Instance;
            if (gm == null) return false;

            int price = GetPrice(itemId);
            if (item.CurrencyType == CurrencyType.Coins) return gm.Coins >= price;
            return gm.Stars >= price;
        }

        public bool IsUnlocked(string itemId)
        {
            ShopItemData item = FindItem(itemId);
            if (item == null) return false;
            var gm = GameManager.Instance;
            return gm != null && gm.Level >= item.UnlockLevel;
        }

        public ShopItemData FindItem(string itemId)
        {
            if (Categories == null) return null;
            foreach (var cat in Categories)
            {
                if (cat.Items == null) continue;
                foreach (var item in cat.Items)
                {
                    if (item.ItemId == itemId) return item;
                }
            }
            return null;
        }

        public List<ShopItemData> GetItemsByCategory(ShopCategoryType categoryType)
        {
            var result = new List<ShopItemData>();
            if (Categories == null) return result;
            foreach (var cat in Categories)
            {
                if (cat.Type == categoryType && cat.Items != null)
                {
                    result.AddRange(cat.Items);
                }
            }
            return result;
        }

        public void SetOwned(string itemId) { ownedItems[itemId] = true; }
        public List<string> GetOwnedItemIds()
        {
            var ids = new List<string>();
            foreach (var kvp in ownedItems) { if (kvp.Value) ids.Add(kvp.Key); }
            return ids;
        }
    }

    [Serializable]
    public class ShopCategory
    {
        public string CategoryName;
        public ShopCategoryType Type;
        public Sprite CategoryIcon;
        public ShopItemData[] Items;
    }

    [Serializable]
    public class ShopItemData
    {
        public string ItemId;
        public string DisplayName;
        public string Description;
        public ShopCategoryType Category;
        public Sprite Icon;
        public Sprite PreviewImage;
        public int Price;
        public CurrencyType CurrencyType;
        public int UnlockLevel;
        public ItemRarity Rarity;
        public string ModelPath; // Path to 3D model or texture
    }

    public enum ShopCategoryType { Clothing, Furniture, Food, Decoration, Pet, Accessory, Room }
    public enum CurrencyType { Coins, Stars }
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
}
