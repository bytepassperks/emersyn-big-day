using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #14: Collection & Achievement sticker book system.
    /// Tracks collectible items, stickers, outfits, furniture, recipes, photos.
    /// Like Animal Crossing's museum/catalog and Toca Life's collectible items.
    /// </summary>
    public class CollectionSystem : MonoBehaviour
    {
        public static CollectionSystem Instance { get; private set; }

        private Dictionary<string, CollectionCategory> categories = new Dictionary<string, CollectionCategory>();
        private int totalCollected;
        private int totalAvailable;

        public event Action<string, string> OnItemCollected; // category, itemId
        public event Action<string> OnCategoryCompleted;

        public int TotalCollected => totalCollected;
        public int TotalAvailable => totalAvailable;
        public float CompletionPercent => totalAvailable > 0 ? (float)totalCollected / totalAvailable : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeCollections();
        }

        private void InitializeCollections()
        {
            // Sticker collections
            AddCategory("stickers_animals", "Animal Stickers", 20);
            AddCategory("stickers_food", "Food Stickers", 15);
            AddCategory("stickers_nature", "Nature Stickers", 12);
            AddCategory("stickers_emoji", "Emoji Stickers", 18);

            // Outfit collections
            AddCategory("outfits_casual", "Casual Outfits", 10);
            AddCategory("outfits_fancy", "Fancy Outfits", 8);
            AddCategory("outfits_costume", "Costume Outfits", 12);
            AddCategory("outfits_accessories", "Accessories", 15);

            // Furniture collections
            AddCategory("furniture_bedroom", "Bedroom Furniture", 10);
            AddCategory("furniture_kitchen", "Kitchen Items", 8);
            AddCategory("furniture_garden", "Garden Decor", 10);
            AddCategory("furniture_special", "Special Items", 6);

            // Recipe collections
            AddCategory("recipes_breakfast", "Breakfast Recipes", 8);
            AddCategory("recipes_lunch", "Lunch Recipes", 10);
            AddCategory("recipes_dessert", "Dessert Recipes", 12);
            AddCategory("recipes_drinks", "Drink Recipes", 6);

            // Photo album
            AddCategory("photos_selfie", "Selfie Photos", 10);
            AddCategory("photos_group", "Group Photos", 8);
            AddCategory("photos_scenic", "Scenic Photos", 12);

            // Pet collection
            AddCategory("pets_tricks", "Pet Tricks", 10);
            AddCategory("pets_outfits", "Pet Outfits", 8);

            CountTotals();
        }

        private void AddCategory(string id, string name, int totalItems)
        {
            categories[id] = new CollectionCategory(id, name, totalItems);
        }

        private void CountTotals()
        {
            totalAvailable = 0;
            totalCollected = 0;
            foreach (var cat in categories.Values)
            {
                totalAvailable += cat.TotalItems;
                totalCollected += cat.CollectedItems.Count;
            }
        }

        public bool CollectItem(string categoryId, string itemId)
        {
            if (!categories.ContainsKey(categoryId)) return false;
            var cat = categories[categoryId];
            if (cat.CollectedItems.Contains(itemId)) return false;

            cat.CollectedItems.Add(itemId);
            totalCollected++;

            // Rewards for collecting
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(5);
                gm.AddXP(10);
            }

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnStarBurst(Vector3.up * 2f);

            OnItemCollected?.Invoke(categoryId, itemId);

            // Check if category complete
            if (cat.CollectedItems.Count >= cat.TotalItems)
            {
                CompleteCategoryReward(cat);
                OnCategoryCompleted?.Invoke(categoryId);
            }

            // Quest integration
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.ReportProgress("collect_item");

            return true;
        }

        private void CompleteCategoryReward(CollectionCategory cat)
        {
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(50);
                gm.AddXP(100);
                gm.Stars += 2;
            }

            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);

            Debug.Log($"[CollectionSystem] Category completed: {cat.DisplayName}!");
        }

        public bool HasItem(string categoryId, string itemId)
        {
            return categories.ContainsKey(categoryId) && categories[categoryId].CollectedItems.Contains(itemId);
        }

        public float GetCategoryProgress(string categoryId)
        {
            if (!categories.ContainsKey(categoryId)) return 0f;
            var cat = categories[categoryId];
            return cat.TotalItems > 0 ? (float)cat.CollectedItems.Count / cat.TotalItems : 0f;
        }

        public int GetCategoryCollected(string categoryId)
        {
            return categories.ContainsKey(categoryId) ? categories[categoryId].CollectedItems.Count : 0;
        }

        public List<string> GetAllCategoryIds()
        {
            return new List<string>(categories.Keys);
        }
    }

    [Serializable]
    public class CollectionCategory
    {
        public string CategoryId;
        public string DisplayName;
        public int TotalItems;

        // HashSet<string> is not serializable in Unity player builds (causes class layout incompatibility).
        // Use List<string> for serialization + runtime HashSet for O(1) lookups per Claude guidance.
        [SerializeField] private List<string> collectedItemsList = new List<string>();
        [NonSerialized] private HashSet<string> _collectedItemsSet;

        public CollectionCategory(string id, string name, int totalItems)
        {
            CategoryId = id;
            DisplayName = name;
            TotalItems = totalItems;
            collectedItemsList = new List<string>();
            _collectedItemsSet = null;
        }

        public HashSet<string> CollectedItems
        {
            get
            {
                if (_collectedItemsSet == null)
                    _collectedItemsSet = new HashSet<string>(collectedItemsList);
                return _collectedItemsSet;
            }
        }

        public void SyncListFromSet()
        {
            if (_collectedItemsSet != null)
                collectedItemsList = new List<string>(_collectedItemsSet);
        }
    }
}
