using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #29: Cosmetic pack system for premium content bundles.
    /// Themed outfit/furniture/sticker packs purchasable with in-game or real currency.
    /// Like Toca Life's world packs and My Talking Angela 2's fashion packs.
    /// </summary>
    public class CosmeticPackSystem : MonoBehaviour
    {
        public static CosmeticPackSystem Instance { get; private set; }

        private List<CosmeticPack> allPacks = new List<CosmeticPack>();

        public event Action<CosmeticPack> OnPackPurchased;
        public event Action<CosmeticPack> OnPackEquipped;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializePacks();
        }

        private void InitializePacks()
        {
            allPacks.Add(new CosmeticPack("pack_karate", "Karate Champion", "Emersyn's karate gear!",
                CosmeticPackType.Outfit, 100, new[] { "karate_gi_top", "karate_pants", "karate_shoes", "karate_belt", "karate_headband" }));

            allPacks.Add(new CosmeticPack("pack_princess", "Princess Bundle", "Royal outfits and accessories!",
                CosmeticPackType.Outfit, 150, new[] { "princess_dress", "tiara", "sparkle_shoes", "flower_crown" }));

            allPacks.Add(new CosmeticPack("pack_superhero", "Superhero Pack", "Save the day in style!",
                CosmeticPackType.Outfit, 120, new[] { "superhero_cape", "boots" }));

            allPacks.Add(new CosmeticPack("pack_bedroom_cozy", "Cozy Bedroom", "Warm and snuggly bedroom decor!",
                CosmeticPackType.Furniture, 200, new[] { "cozy_bed", "fairy_lights", "plush_rug", "star_lamp" }));

            allPacks.Add(new CosmeticPack("pack_garden_magic", "Magic Garden", "Enchanted garden decorations!",
                CosmeticPackType.Furniture, 180, new[] { "fairy_house", "magic_flowers", "butterfly_bush", "wishing_well" }));

            allPacks.Add(new CosmeticPack("pack_stickers_cute", "Cute Stickers Vol.1", "50 adorable stickers!",
                CosmeticPackType.Sticker, 80, new[] { "sticker_set_cute_1" }));

            allPacks.Add(new CosmeticPack("pack_pet_outfits", "Pet Fashion", "Dress up your pets!",
                CosmeticPackType.PetOutfit, 100, new[] { "pet_bow_tie", "pet_tutu", "pet_cape", "pet_hat" }));

            allPacks.Add(new CosmeticPack("pack_seasonal_spring", "Spring Festival", "Seasonal spring items!",
                CosmeticPackType.Seasonal, 150, new[] { "flower_dress", "bunny_ears", "spring_garden_set" }));

            // Load owned packs
            foreach (var pack in allPacks)
            {
                pack.IsOwned = PlayerPrefs.GetInt($"pack_{pack.PackId}", 0) == 1;
            }
        }

        public bool PurchasePack(string packId)
        {
            var pack = allPacks.Find(p => p.PackId == packId);
            if (pack == null || pack.IsOwned) return false;

            var gm = Core.GameManager.Instance;
            if (gm == null || gm.Coins < pack.CoinPrice) return false;

            gm.Coins -= pack.CoinPrice;
            pack.IsOwned = true;

            PlayerPrefs.SetInt($"pack_{packId}", 1);
            PlayerPrefs.Save();

            // Unlock all items in pack
            if (Gameplay.CharacterCustomization.Instance != null)
            {
                foreach (var itemId in pack.ItemIds)
                    Gameplay.CharacterCustomization.Instance.PurchaseItem(itemId);
            }

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
            {
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
                Visual.ProceduralParticles.Instance.SpawnStarBurst(Vector3.up * 2f);
            }

            // Analytics
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackPurchase(packId, pack.CoinPrice);

            OnPackPurchased?.Invoke(pack);
            return true;
        }

        public List<CosmeticPack> GetPacksByType(CosmeticPackType type)
        {
            return allPacks.FindAll(p => p.Type == type);
        }

        public List<CosmeticPack> GetAvailablePacks()
        {
            return allPacks.FindAll(p => !p.IsOwned);
        }

        public CosmeticPack GetPack(string packId)
        {
            return allPacks.Find(p => p.PackId == packId);
        }
    }

    [Serializable]
    public class CosmeticPack
    {
        public string PackId;
        public string DisplayName;
        public string Description;
        public CosmeticPackType Type;
        public int CoinPrice;
        public string[] ItemIds;
        public bool IsOwned;

        public CosmeticPack(string id, string name, string desc, CosmeticPackType type, int price, string[] items)
        {
            PackId = id; DisplayName = name; Description = desc;
            Type = type; CoinPrice = price; ItemIds = items;
        }
    }

    public enum CosmeticPackType { Outfit, Furniture, Sticker, PetOutfit, Seasonal }
}
