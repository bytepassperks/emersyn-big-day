using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #3: Customizable character system with modular hair, clothes, accessories.
    /// Like Toca Life's 1000+ items and My Talking Angela 2's dress-up system.
    /// Emersyn is 6 years old, does karate, has brown skin, brown eyes, loves pink.
    /// </summary>
    public class CharacterCustomization : MonoBehaviour
    {
        public static CharacterCustomization Instance { get; private set; }

        [Header("Current Equipment")]
        public string CurrentHairStyle = "ponytail";
        public string CurrentOutfitTop = "pink_tshirt";
        public string CurrentOutfitBottom = "denim_skirt";
        public string CurrentShoes = "pink_sneakers";
        public string CurrentAccessory = "hair_bow";

        [Header("Colors")]
        public Color HairColor = new Color(0.15f, 0.1f, 0.08f); // Dark brown
        public Color SkinColor = new Color(0.55f, 0.38f, 0.28f); // Brown skin
        public Color OutfitPrimaryColor = new Color(1f, 0.6f, 0.8f); // Pink
        public Color OutfitSecondaryColor = Color.white;
        public Color EyeColor = new Color(0.3f, 0.2f, 0.1f); // Brown

        private Dictionary<string, List<CustomizationItem>> itemCategories = new Dictionary<string, List<CustomizationItem>>();
        private List<string> ownedItems = new List<string>();

        public event Action<string, string> OnItemEquipped; // category, itemId
        public event Action<Color> OnColorChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeItems();
        }

        private void InitializeItems()
        {
            // Hair styles
            var hair = new List<CustomizationItem>
            {
                new CustomizationItem("ponytail", "Ponytail", "hair", 0, true),
                new CustomizationItem("pigtails", "Pigtails", "hair", 10),
                new CustomizationItem("braids", "Braids", "hair", 15),
                new CustomizationItem("bob", "Bob Cut", "hair", 20),
                new CustomizationItem("curly", "Curly", "hair", 15),
                new CustomizationItem("afro_puffs", "Afro Puffs", "hair", 25),
                new CustomizationItem("space_buns", "Space Buns", "hair", 20),
                new CustomizationItem("long_straight", "Long & Straight", "hair", 15),
                new CustomizationItem("karate_headband", "Karate Headband", "hair", 30),
            };
            itemCategories["hair"] = hair;

            // Tops
            var tops = new List<CustomizationItem>
            {
                new CustomizationItem("pink_tshirt", "Pink T-Shirt", "top", 0, true),
                new CustomizationItem("karate_gi_top", "Karate Gi", "top", 30),
                new CustomizationItem("princess_dress", "Princess Dress", "top", 40),
                new CustomizationItem("rainbow_hoodie", "Rainbow Hoodie", "top", 25),
                new CustomizationItem("flower_blouse", "Flower Blouse", "top", 20),
                new CustomizationItem("denim_jacket", "Denim Jacket", "top", 30),
                new CustomizationItem("tutu_top", "Ballet Tutu", "top", 35),
                new CustomizationItem("superhero_cape", "Superhero Cape", "top", 45),
                new CustomizationItem("school_uniform", "School Uniform", "top", 20),
                new CustomizationItem("artist_smock", "Artist Smock", "top", 15),
            };
            itemCategories["top"] = tops;

            // Bottoms
            var bottoms = new List<CustomizationItem>
            {
                new CustomizationItem("denim_skirt", "Denim Skirt", "bottom", 0, true),
                new CustomizationItem("karate_pants", "Karate Pants", "bottom", 30),
                new CustomizationItem("tutu_skirt", "Tutu Skirt", "bottom", 25),
                new CustomizationItem("jeans", "Jeans", "bottom", 15),
                new CustomizationItem("leggings", "Leggings", "bottom", 10),
                new CustomizationItem("overalls", "Overalls", "bottom", 20),
                new CustomizationItem("shorts", "Shorts", "bottom", 10),
            };
            itemCategories["bottom"] = bottoms;

            // Shoes
            var shoes = new List<CustomizationItem>
            {
                new CustomizationItem("pink_sneakers", "Pink Sneakers", "shoes", 0, true),
                new CustomizationItem("ballet_shoes", "Ballet Shoes", "shoes", 20),
                new CustomizationItem("boots", "Rain Boots", "shoes", 15),
                new CustomizationItem("sandals", "Sandals", "shoes", 10),
                new CustomizationItem("sparkle_shoes", "Sparkle Shoes", "shoes", 30),
                new CustomizationItem("karate_shoes", "Karate Shoes", "shoes", 25),
            };
            itemCategories["shoes"] = shoes;

            // Accessories
            var accessories = new List<CustomizationItem>
            {
                new CustomizationItem("hair_bow", "Hair Bow", "accessory", 0, true),
                new CustomizationItem("tiara", "Tiara", "accessory", 30),
                new CustomizationItem("sunglasses", "Sunglasses", "accessory", 15),
                new CustomizationItem("necklace", "Necklace", "accessory", 20),
                new CustomizationItem("backpack", "Backpack", "accessory", 15),
                new CustomizationItem("wings", "Fairy Wings", "accessory", 40),
                new CustomizationItem("karate_belt", "Karate Belt", "accessory", 25),
                new CustomizationItem("flower_crown", "Flower Crown", "accessory", 20),
                new CustomizationItem("cat_ears", "Cat Ears", "accessory", 15),
                new CustomizationItem("bunny_ears", "Bunny Ears", "accessory", 15),
            };
            itemCategories["accessory"] = accessories;

            // Add default owned items
            foreach (var cat in itemCategories.Values)
                foreach (var item in cat)
                    if (item.IsDefault) ownedItems.Add(item.ItemId);
        }

        public bool EquipItem(string category, string itemId)
        {
            if (!IsOwned(itemId)) return false;

            switch (category)
            {
                case "hair": CurrentHairStyle = itemId; break;
                case "top": CurrentOutfitTop = itemId; break;
                case "bottom": CurrentOutfitBottom = itemId; break;
                case "shoes": CurrentShoes = itemId; break;
                case "accessory": CurrentAccessory = itemId; break;
                default: return false;
            }

            OnItemEquipped?.Invoke(category, itemId);

            // Quest tracking
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.ReportProgress("change_outfit");

            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("tap");

            return true;
        }

        public bool PurchaseItem(string itemId)
        {
            if (IsOwned(itemId)) return false;

            CustomizationItem item = FindItem(itemId);
            if (item == null) return false;

            var gm = Core.GameManager.Instance;
            if (gm == null || gm.Coins < item.Price) return false;

            gm.Coins -= item.Price;
            ownedItems.Add(itemId);

            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnSparkles(Vector3.up * 2f);

            if (CollectionSystem.Instance != null)
                CollectionSystem.Instance.CollectItem("outfits_casual", itemId);

            return true;
        }

        public void SetHairColor(Color color)
        {
            HairColor = color;
            OnColorChanged?.Invoke(color);
        }

        public void SetOutfitColor(Color primary, Color secondary)
        {
            OutfitPrimaryColor = primary;
            OutfitSecondaryColor = secondary;
            OnColorChanged?.Invoke(primary);
        }

        public bool IsOwned(string itemId) => ownedItems.Contains(itemId);

        public List<CustomizationItem> GetItemsByCategory(string category)
        {
            return itemCategories.ContainsKey(category) ? itemCategories[category] : new List<CustomizationItem>();
        }

        public CustomizationItem FindItem(string itemId)
        {
            foreach (var cat in itemCategories.Values)
                foreach (var item in cat)
                    if (item.ItemId == itemId) return item;
            return null;
        }

        public int GetOwnedCount()
        {
            return ownedItems.Count;
        }

        /// <summary>
        /// Apply current customization colors to a character's renderers.
        /// </summary>
        public void ApplyToCharacter(GameObject character)
        {
            if (character == null) return;
            var renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                string name = r.gameObject.name.ToLower();
                Color targetColor;
                if (name.Contains("hair")) targetColor = HairColor;
                else if (name.Contains("skin") || name.Contains("body") || name.Contains("face"))
                    targetColor = SkinColor;
                else if (name.Contains("eye")) targetColor = EyeColor;
                else if (name.Contains("top") || name.Contains("shirt") || name.Contains("dress"))
                    targetColor = OutfitPrimaryColor;
                else if (name.Contains("bottom") || name.Contains("skirt") || name.Contains("pant"))
                    targetColor = OutfitSecondaryColor;
                else if (name.Contains("shoe") || name.Contains("foot"))
                    targetColor = OutfitPrimaryColor * 0.8f;
                else continue;

                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);
                    else mat.color = targetColor;
                }
            }
        }
    }

    [Serializable]
    public class CustomizationItem
    {
        public string ItemId;
        public string DisplayName;
        public string Category;
        public int Price;
        public bool IsDefault;
        public Sprite Icon;

        public CustomizationItem(string id, string name, string cat, int price, bool isDefault = false)
        {
            ItemId = id; DisplayName = name; Category = cat; Price = price; IsDefault = isDefault;
        }
    }
}
