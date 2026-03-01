using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Fashion Show: dress up the character with clothing items to match a theme.
    /// Judges rate the outfit. Different themes each round.
    /// Satisfies Social and Creativity needs.
    /// </summary>
    public class FashionShowGame : MonoBehaviour
    {
        [Header("Settings")]
        public float DressUpTime = 30f;
        public int MaxOutfitSlots = 5;

        [Header("Theme")]
        public string[] Themes;
        public string CurrentTheme;

        [Header("Clothing")]
        public ClothingItem[] AvailableClothing;

        [Header("UI")]
        public Transform ClothingScrollContainer;
        public GameObject ClothingButtonPrefab;
        public TMPro.TextMeshProUGUI ThemeText;
        public TMPro.TextMeshProUGUI TimerText;
        public TMPro.TextMeshProUGUI ScoreText;

        private List<ClothingItem> currentOutfit = new List<ClothingItem>();
        private float gameTimer;
        private int score = 0;
        private bool isActive = false;

        public void StartGame()
        {
            // Pick random theme
            if (Themes != null && Themes.Length > 0)
                CurrentTheme = Themes[UnityEngine.Random.Range(0, Themes.Length)];
            else
                CurrentTheme = "Casual";

            if (ThemeText != null) ThemeText.text = $"Theme: {CurrentTheme}";

            currentOutfit.Clear();
            gameTimer = DressUpTime;
            score = 0;
            isActive = true;

            SpawnClothingOptions();
        }

        private void Update()
        {
            if (!isActive) return;

            gameTimer -= Time.deltaTime;
            if (TimerText != null) TimerText.text = $"{Mathf.CeilToInt(gameTimer)}s";

            if (gameTimer <= 0f)
            {
                JudgeOutfit();
            }
        }

        private void SpawnClothingOptions()
        {
            if (AvailableClothing == null || ClothingButtonPrefab == null || ClothingScrollContainer == null) return;

            // Shuffle and show subset
            List<ClothingItem> shuffled = new List<ClothingItem>(AvailableClothing);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int count = Mathf.Min(shuffled.Count, 12);
            for (int i = 0; i < count; i++)
            {
                var item = shuffled[i];
                var btn = Instantiate(ClothingButtonPrefab, ClothingScrollContainer);
                var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null) text.text = item.Name;
                var image = btn.GetComponentInChildren<UnityEngine.UI.Image>();
                if (image != null && item.Icon != null) image.sprite = item.Icon;

                int capturedIndex = i;
                btn.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() => OnClothingSelected(shuffled[capturedIndex]));
            }
        }

        public void OnClothingSelected(ClothingItem item)
        {
            if (!isActive || currentOutfit.Count >= MaxOutfitSlots) return;

            // Check if slot already occupied
            foreach (var equipped in currentOutfit)
            {
                if (equipped.Slot == item.Slot)
                {
                    currentOutfit.Remove(equipped);
                    break;
                }
            }

            currentOutfit.Add(item);
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("tap");
        }

        public void SubmitOutfit()
        {
            if (!isActive) return;
            JudgeOutfit();
        }

        private void JudgeOutfit()
        {
            isActive = false;

            // Score based on theme matching, variety, and style
            int themeBonus = 0;
            int varietyBonus = 0;
            int styleBonus = 0;

            HashSet<string> slotsUsed = new HashSet<string>();
            foreach (var item in currentOutfit)
            {
                // Theme matching
                if (item.Tags != null)
                {
                    foreach (var tag in item.Tags)
                    {
                        if (tag.ToLower() == CurrentTheme.ToLower())
                        {
                            themeBonus += 30;
                            break;
                        }
                    }
                }

                // Variety (different slots)
                if (!slotsUsed.Contains(item.Slot))
                {
                    slotsUsed.Add(item.Slot);
                    varietyBonus += 10;
                }

                // Style points
                styleBonus += item.StylePoints;
            }

            score = themeBonus + varietyBonus + styleBonus;

            // Crowd reaction based on score
            if (score >= 80)
            {
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("win");
            }

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(score >= 50);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Social", 20f);
                needSystem.SatisfyNeed("Creativity", 15f);
            }
        }
    }

    [System.Serializable]
    public class ClothingItem
    {
        public string Name;
        public string Slot; // Head, Top, Bottom, Shoes, Accessory
        public Sprite Icon;
        public string[] Tags; // Casual, Formal, Sporty, Princess, Winter, Summer
        public int StylePoints;
        public Color PrimaryColor;
    }
}
