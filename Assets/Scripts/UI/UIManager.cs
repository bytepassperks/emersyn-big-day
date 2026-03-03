using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.UI
{
    /// <summary>
    /// Manages all UI elements: HUD, menus, popups, transitions, and touch feedback.
    /// Implements Talking Tom-style clean UI with stat bars, currency display, and room navigation.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Elements")]
        public GameObject HUDPanel;
        public Text CoinText;
        public Text StarText;
        public Text LevelText;
        public Text DayText;
        public Slider XPBar;
        public Image MoodIcon;

        [Header("Need Bars")]
        public NeedBarUI[] NeedBars;

        [Header("Room Navigation")]
        public Button NextRoomButton;
        public Button PrevRoomButton;
        public Text RoomNameText;
        public Image RoomIcon;

        [Header("Menus")]
        public GameObject MainMenuPanel;
        public GameObject PauseMenuPanel;
        public GameObject SettingsPanel;
        public GameObject ShopPanel;
        public GameObject AchievementPanel;
        public GameObject MiniGamePanel;

        [Header("Popups")]
        public GameObject RewardPopup;
        public GameObject AchievementPopup;
        public GameObject LevelUpPopup;
        public GameObject EventPopup;
        public GameObject DailyRewardPopup;

        [Header("Transitions")]
        public CanvasGroup FadeOverlay;
        public float PopupAnimDuration = 0.3f;

        [Header("Touch Feedback")]
        public GameObject TapEffectPrefab;
        public Canvas WorldCanvas;

        private Queue<PopupData> popupQueue = new Queue<PopupData>();
        private bool isShowingPopup = false;

        public event Action OnShopOpened;
        public event Action OnShopClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetupButtons();
            ShowMainMenu();
        }

        private void Update()
        {
            UpdateHUD();
            ProcessPopupQueue();
        }

        private void SetupButtons()
        {
            if (NextRoomButton != null) NextRoomButton.onClick.AddListener(OnNextRoom);
            if (PrevRoomButton != null) PrevRoomButton.onClick.AddListener(OnPrevRoom);
        }

        // --- HUD UPDATE ---
        private void UpdateHUD()
        {
            var gm = Core.GameManager.Instance;
            if (gm == null) return;

            if (CoinText != null) CoinText.text = FormatNumber(gm.Coins);
            if (StarText != null) StarText.text = FormatNumber(gm.Stars);
            if (LevelText != null) LevelText.text = $"Lv.{gm.Level}";
            if (DayText != null) DayText.text = $"Day {gm.CurrentDay}";
            if (XPBar != null) XPBar.value = gm.XPToNextLevel > 0 ? (float)gm.XP / gm.XPToNextLevel : 0f;

            UpdateNeedBars();
        }

        private void UpdateNeedBars()
        {
            if (NeedBars == null) return;
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem == null) return;

            foreach (var bar in NeedBars)
            {
                if (bar == null) continue;
                var need = needSystem.GetNeed(bar.NeedName);
                if (need != null)
                {
                    bar.UpdateBar(need.Normalized, need.IsCritical);
                }
            }
        }

        // --- ROOM NAVIGATION ---
        private void OnNextRoom()
        {
            if (Rooms.RoomManager.Instance != null) Rooms.RoomManager.Instance.GoToNextRoom();
        }

        private void OnPrevRoom()
        {
            if (Rooms.RoomManager.Instance != null) Rooms.RoomManager.Instance.GoToPreviousRoom();
        }

        public void UpdateRoomDisplay(string roomName, Sprite icon)
        {
            if (RoomNameText != null) RoomNameText.text = roomName;
            if (RoomIcon != null && icon != null) RoomIcon.sprite = icon;
        }

        // --- MENUS ---
        public void ShowMainMenu()
        {
            SetPanelActive(MainMenuPanel, true);
            SetPanelActive(HUDPanel, false);
        }

        public void HideMainMenu()
        {
            SetPanelActive(MainMenuPanel, false);
            SetPanelActive(HUDPanel, true);
        }

        public void TogglePause()
        {
            bool show = PauseMenuPanel != null && !PauseMenuPanel.activeSelf;
            SetPanelActive(PauseMenuPanel, show);
            Time.timeScale = show ? 0f : 1f;
        }

        public void OpenShop()
        {
            SetPanelActive(ShopPanel, true);
            OnShopOpened?.Invoke();
        }

        public void CloseShop()
        {
            SetPanelActive(ShopPanel, false);
            OnShopClosed?.Invoke();
        }

        public void OpenSettings() { SetPanelActive(SettingsPanel, true); }
        public void CloseSettings() { SetPanelActive(SettingsPanel, false); }
        public void OpenAchievements() { SetPanelActive(AchievementPanel, true); }
        public void CloseAchievements() { SetPanelActive(AchievementPanel, false); }

        // --- POPUPS ---
        public void ShowRewardPopup(string title, int coins, int stars, int xp)
        {
            popupQueue.Enqueue(new PopupData
            {
                Type = PopupType.Reward,
                Title = title,
                Coins = coins,
                Stars = stars,
                XP = xp
            });
        }

        public void ShowAchievementPopup(string achievementName, string description)
        {
            popupQueue.Enqueue(new PopupData
            {
                Type = PopupType.Achievement,
                Title = achievementName,
                Description = description
            });
        }

        public void ShowLevelUpPopup(int newLevel)
        {
            popupQueue.Enqueue(new PopupData
            {
                Type = PopupType.LevelUp,
                Title = $"Level {newLevel}!",
                Description = "You leveled up!"
            });
        }

        public void ShowEventPopup(string eventName, string description)
        {
            popupQueue.Enqueue(new PopupData
            {
                Type = PopupType.Event,
                Title = eventName,
                Description = description
            });
        }

        private void ProcessPopupQueue()
        {
            if (isShowingPopup || popupQueue.Count == 0) return;
            var data = popupQueue.Dequeue();
            StartCoroutine(ShowPopupCoroutine(data));
        }

        private System.Collections.IEnumerator ShowPopupCoroutine(PopupData data)
        {
            isShowingPopup = true;
            GameObject popup = GetPopupByType(data.Type);
            if (popup == null) { isShowingPopup = false; yield break; }

            // Set popup content
            var titleText = popup.GetComponentInChildren<Text>();
            if (titleText != null) titleText.text = data.Title;

            // Animate in (scale from 0 to 1)
            popup.SetActive(true);
            popup.transform.localScale = Vector3.zero;
            float timer = 0f;
            while (timer < PopupAnimDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / PopupAnimDuration;
                float scale = EaseOutBack(t);
                popup.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            popup.transform.localScale = Vector3.one;

            // Show for 2 seconds
            yield return new WaitForSecondsRealtime(2f);

            // Animate out (scale to 0)
            timer = 0f;
            while (timer < PopupAnimDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / PopupAnimDuration;
                popup.transform.localScale = Vector3.one * (1f - t);
                yield return null;
            }

            popup.SetActive(false);
            isShowingPopup = false;
        }

        private GameObject GetPopupByType(PopupType type)
        {
            switch (type)
            {
                case PopupType.Reward: return RewardPopup;
                case PopupType.Achievement: return AchievementPopup;
                case PopupType.LevelUp: return LevelUpPopup;
                case PopupType.Event: return EventPopup;
                default: return null;
            }
        }

        // --- FADE OVERLAY ---
        public void SetFadeOverlay(float alpha)
        {
            if (FadeOverlay != null)
            {
                FadeOverlay.alpha = alpha;
                FadeOverlay.blocksRaycasts = alpha > 0.01f;
            }
        }

        // --- TOUCH FEEDBACK ---
        public void SpawnTapEffect(Vector3 worldPosition)
        {
            if (TapEffectPrefab == null || WorldCanvas == null) return;
            GameObject effect = Instantiate(TapEffectPrefab, WorldCanvas.transform);
            effect.transform.position = worldPosition;
            Destroy(effect, 1f);
        }

        // --- HELPERS ---
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private string FormatNumber(int num)
        {
            if (num >= 1000000) return $"{num / 1000000f:F1}M";
            if (num >= 1000) return $"{num / 1000f:F1}K";
            return num.ToString();
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    [Serializable]
    public class NeedBarUI
    {
        public string NeedName;
        public Slider BarSlider;
        public Image FillImage;
        public Image IconImage;
        public Color NormalColor = Color.green;
        public Color LowColor = Color.yellow;
        public Color CriticalColor = Color.red;

        public void UpdateBar(float normalized, bool isCritical)
        {
            if (BarSlider != null) BarSlider.value = normalized;
            if (FillImage != null)
            {
                if (isCritical) FillImage.color = CriticalColor;
                else if (normalized < 0.4f) FillImage.color = LowColor;
                else FillImage.color = NormalColor;
            }
        }
    }

    public enum PopupType { Reward, Achievement, LevelUp, Event, DailyReward }

    [Serializable]
    public class PopupData
    {
        public PopupType Type;
        public string Title;
        public string Description;
        public int Coins;
        public int Stars;
        public int XP;
    }
}
