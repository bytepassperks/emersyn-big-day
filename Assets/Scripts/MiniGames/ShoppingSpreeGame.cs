using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Shopping Spree: buy items within a budget to furnish a room or fill a wardrobe.
    /// Each item has a price and value. Maximize total value within budget.
    /// Satisfies Comfort need.
    /// </summary>
    public class ShoppingSpreeGame : MonoBehaviour
    {
        [Header("Settings")]
        public int Budget = 100;
        public float GameDuration = 40f;
        public int ItemsToDisplay = 8;

        [Header("Items")]
        public ShopItem[] AllItems;

        [Header("UI")]
        public Transform ItemContainer;
        public GameObject ItemCardPrefab;
        public TMPro.TextMeshProUGUI BudgetText;
        public TMPro.TextMeshProUGUI CartValueText;
        public TMPro.TextMeshProUGUI TimerText;

        private List<ShopItem> cart = new List<ShopItem>();
        private int spent = 0;
        private int cartValue = 0;
        private float gameTimer;
        private bool isActive = false;

        public void StartGame()
        {
            cart.Clear();
            spent = 0;
            cartValue = 0;
            gameTimer = GameDuration;
            isActive = true;

            DisplayItems();
            UpdateUI();
        }

        private void Update()
        {
            if (!isActive) return;
            gameTimer -= Time.deltaTime;
            if (TimerText != null) TimerText.text = $"{Mathf.CeilToInt(gameTimer)}s";
            if (gameTimer <= 0f) Checkout();
        }

        private void DisplayItems()
        {
            if (AllItems == null || ItemCardPrefab == null || ItemContainer == null) return;

            List<ShopItem> shuffled = new List<ShopItem>(AllItems);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int count = Mathf.Min(ItemsToDisplay, shuffled.Count);
            for (int i = 0; i < count; i++)
            {
                var item = shuffled[i];
                var card = Instantiate(ItemCardPrefab, ItemContainer);
                var text = card.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null) text.text = $"{item.Name}\n${item.Price}";

                var capturedItem = item;
                card.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() => BuyItem(capturedItem));
            }
        }

        public void BuyItem(ShopItem item)
        {
            if (!isActive) return;
            if (spent + item.Price > Budget)
            {
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("sad");
                if (CameraSystem.CameraController.Instance != null)
                    CameraSystem.CameraController.Instance.ShakeSmall();
                return;
            }

            cart.Add(item);
            spent += item.Price;
            cartValue += item.Value;

            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnSparkles(Vector3.up * 2f);

            UpdateUI();
        }

        public void Checkout()
        {
            isActive = false;

            int score = cartValue;
            // Bonus for staying under budget
            int remaining = Budget - spent;
            score += remaining / 2;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(cartValue >= Budget * 0.8f);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Comfort", 20f);
        }

        private void UpdateUI()
        {
            if (BudgetText != null) BudgetText.text = $"Budget: ${Budget - spent}";
            if (CartValueText != null) CartValueText.text = $"Cart Value: {cartValue}";
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public string Name;
        public int Price;
        public int Value;
        public Sprite Icon;
        public string Category;
    }
}
