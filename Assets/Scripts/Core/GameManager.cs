using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Central game manager - singleton that orchestrates all game systems.
    /// Handles game state, room transitions, time management, and system coordination.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState CurrentState = GameState.MainMenu;
        public float GameTimeScale = 1f;
        public int CurrentDay = 1;
        public float DayTimer = 0f;
        public float DayDuration = 300f; // 5 min real time = 1 game day

        [Header("Currency & Progression")]
        public int Coins = 100;
        public int Stars = 0;
        public int XP = 0;
        public int Level = 1;
        public int XPToNextLevel = 100;

        [Header("Daily Events")]
        public bool DailyRewardClaimed = false;
        public int ConsecutiveLoginDays = 0;
        public string LastLoginDate = "";

        // Events
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnStarsChanged;
        public event Action<int, int> OnXPChanged; // current, max
        public event Action<int> OnLevelUp;
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnRoomChanged;
        public event Action OnDayChanged;
        public event Action<string> OnAchievementUnlocked;

        // Random event system
        private List<string> dailyEvents = new List<string>
        {
            "surprise_party", "rainy_day", "field_trip", "pet_arrives",
            "friend_visits", "cooking_contest", "dance_off", "treasure_hunt",
            "garden_bloom", "stargazing", "art_show", "music_festival",
            "pajama_party", "sports_day", "science_fair"
        };

        private float randomEventTimer = 0f;
        private float randomEventInterval = 60f; // Check every 60 seconds

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadGame();
            ChangeState(GameState.Playing);
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // Update day timer
            DayTimer += Time.deltaTime * GameTimeScale;
            if (DayTimer >= DayDuration)
            {
                AdvanceDay();
            }

            // Random events
            randomEventTimer += Time.deltaTime;
            if (randomEventTimer >= randomEventInterval)
            {
                randomEventTimer = 0f;
                TryTriggerRandomEvent();
            }
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        public void AddCoins(int amount)
        {
            Coins = Mathf.Max(0, Coins + amount);
            OnCoinsChanged?.Invoke(Coins);
            if (amount > 0)
            {
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnCoinCollect(Vector3.zero);
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("coin");
            }
        }

        public void AddStars(int amount)
        {
            Stars = Mathf.Max(0, Stars + amount);
            OnStarsChanged?.Invoke(Stars);
            if (amount > 0)
            {
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("star");
            }
        }

        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel)
            {
                XP -= XPToNextLevel;
                Level++;
                XPToNextLevel = CalculateXPForLevel(Level);
                OnLevelUp?.Invoke(Level);
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnLevelUp(Vector3.zero);
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("levelup");
            }
            OnXPChanged?.Invoke(XP, XPToNextLevel);
        }

        private int CalculateXPForLevel(int level)
        {
            return 100 + (level - 1) * 50; // 100, 150, 200, 250...
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("button");
            return true;
        }

        public void ChangeRoom(string roomName)
        {
            if (Rooms.RoomManager.Instance != null)
                Rooms.RoomManager.Instance.LoadRoom(roomName);
            OnRoomChanged?.Invoke(roomName);
        }

        private void AdvanceDay()
        {
            DayTimer = 0f;
            CurrentDay++;
            OnDayChanged?.Invoke();

            // Daily reward
            DailyRewardClaimed = false;
        }

        public void ClaimDailyReward()
        {
            if (DailyRewardClaimed) return;
            DailyRewardClaimed = true;
            ConsecutiveLoginDays++;

            int rewardCoins = 10 + ConsecutiveLoginDays * 5;
            int rewardStars = ConsecutiveLoginDays >= 7 ? 1 : 0;

            AddCoins(rewardCoins);
            AddStars(rewardStars);
            AddXP(25);

            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");
        }

        private void TryTriggerRandomEvent()
        {
            if (UnityEngine.Random.value > 0.3f) return; // 30% chance

            int idx = UnityEngine.Random.Range(0, dailyEvents.Count);
            string eventName = dailyEvents[idx];
            TriggerEvent(eventName);
        }

        private void TriggerEvent(string eventName)
        {
            Debug.Log($"[GameManager] Random event triggered: {eventName}");
            if (UI.UIManager.Instance != null)
                UI.UIManager.Instance.ShowEventPopup(eventName, $"A {eventName.Replace('_', ' ')} is happening!");
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");

            switch (eventName)
            {
                case "surprise_party":
                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                    AddCoins(50);
                    break;
                case "treasure_hunt":
                    AddStars(2);
                    break;
                default:
                    AddXP(15);
                    break;
            }
        }

        public void UnlockAchievement(string achievementId)
        {
            OnAchievementUnlocked?.Invoke(achievementId);
            AddXP(50);
            AddStars(1);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");
            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 2f);
        }

        private void LoadGame()
        {
            if (Data.SaveManager.Instance != null)
                Data.SaveManager.Instance.LoadGame();
        }

        public void SaveGame()
        {
            if (Data.SaveManager.Instance != null)
                Data.SaveManager.Instance.SaveGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        MiniGame,
        Shopping,
        Settings,
        Loading,
        Cutscene
    }
}
