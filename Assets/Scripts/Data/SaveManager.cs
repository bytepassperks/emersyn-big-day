using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace EmersynBigDay.Data
{
    /// <summary>
    /// Handles save/load game data using JSON serialization to persistent storage.
    /// Tracks progression, inventory, achievements, settings, and statistics.
    /// Auto-saves periodically and on app pause/quit.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Settings")]
        public float AutoSaveInterval = 60f;
        public string SaveFileName = "emersyn_save.json";

        private float autoSaveTimer;
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadGame();
        }

        private void Update()
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= AutoSaveInterval)
            {
                autoSaveTimer = 0f;
                SaveGame();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        // --- SAVE ---
        public void SaveGame()
        {
            try
            {
                GameSaveData data = CollectSaveData();
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
            }
        }

        // --- LOAD ---
        public void LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("No save file found. Starting fresh.");
                    return;
                }

                string json = File.ReadAllText(SavePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                ApplySaveData(data);
                OnLoadCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
            }
        }

        // --- DELETE ---
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("Save file deleted.");
            }
        }

        public bool HasSaveFile() => File.Exists(SavePath);

        // --- COLLECT DATA ---
        private GameSaveData CollectSaveData()
        {
            var gm = Core.GameManager.Instance;
            var data = new GameSaveData();

            if (gm != null)
            {
                data.Coins = gm.Coins;
                data.Stars = gm.Stars;
                data.XP = gm.XP;
                data.Level = gm.Level;
                data.XPToNextLevel = gm.XPToNextLevel;
                data.CurrentDay = gm.CurrentDay;
                data.ConsecutiveLogins = gm.ConsecutiveLoginDays;
                data.LastLoginDate = gm.LastLoginDate;
            }

            // Save need values
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                data.NeedValues = new float[8];
                string[] needNames = { "Hunger", "Energy", "Hygiene", "Fun", "Social", "Comfort", "Bladder", "Creativity" };
                for (int i = 0; i < needNames.Length; i++)
                {
                    var need = needSystem.GetNeed(needNames[i]);
                    data.NeedValues[i] = need != null ? need.Value : 50f;
                }
            }

            // Save room index
            if (Rooms.RoomManager.Instance != null)
            {
                data.CurrentRoomIndex = Rooms.RoomManager.Instance.CurrentRoomIndex;
            }

            // Save audio settings
            if (Audio.AudioManager.Instance != null)
            {
                var am = Audio.AudioManager.Instance;
                data.MasterVolume = am.MasterVolume;
                data.MusicVolume = am.MusicVolume;
                data.SFXVolume = am.SFXVolume;
                data.IsMuted = am.IsMuted;
            }

            data.SaveTimestamp = DateTime.UtcNow.ToString("o");
            data.SaveVersion = 1;

            return data;
        }

        // --- APPLY DATA ---
        private void ApplySaveData(GameSaveData data)
        {
            if (data == null) return;

            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.Coins = data.Coins;
                gm.Stars = data.Stars;
                gm.XP = data.XP;
                gm.Level = data.Level;
                gm.XPToNextLevel = data.XPToNextLevel;
                gm.CurrentDay = data.CurrentDay;
                gm.ConsecutiveLoginDays = data.ConsecutiveLogins;
                gm.LastLoginDate = data.LastLoginDate;
            }

            // Restore needs
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null && data.NeedValues != null)
            {
                string[] needNames = { "Hunger", "Energy", "Hygiene", "Fun", "Social", "Comfort", "Bladder", "Creativity" };
                for (int i = 0; i < Mathf.Min(needNames.Length, data.NeedValues.Length); i++)
                {
                    var need = needSystem.GetNeed(needNames[i]);
                    if (need != null) need.Value = data.NeedValues[i];
                }
            }

            // Restore audio settings
            if (Audio.AudioManager.Instance != null)
            {
                var am = Audio.AudioManager.Instance;
                am.SetMasterVolume(data.MasterVolume);
                am.SetMusicVolume(data.MusicVolume);
                am.SetSFXVolume(data.SFXVolume);
                am.IsMuted = data.IsMuted;
            }

            Debug.Log($"Game loaded. Day {data.CurrentDay}, Level {data.Level}, {data.Coins} coins.");
        }
    }

    [Serializable]
    public class GameSaveData
    {
        // Progression
        public int Coins;
        public int Stars;
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public int CurrentDay;

        // Login tracking
        public int ConsecutiveLogins;
        public string LastLoginDate;

        // Needs (8 values: Hunger, Energy, Hygiene, Fun, Social, Comfort, Bladder, Creativity)
        public float[] NeedValues;

        // Room
        public int CurrentRoomIndex;

        // Inventory
        public List<string> OwnedClothing = new List<string>();
        public List<string> OwnedFurniture = new List<string>();
        public List<string> OwnedFood = new List<string>();
        public List<string> OwnedPets = new List<string>();

        // Achievements
        public List<string> UnlockedAchievements = new List<string>();

        // Statistics
        public int TotalMiniGamesPlayed;
        public int TotalMiniGamesWon;
        public int TotalDaysPlayed;
        public float TotalPlayTimeSeconds;

        // Audio settings
        public float MasterVolume = 1f;
        public float MusicVolume = 0.5f;
        public float SFXVolume = 0.8f;
        public bool IsMuted;

        // Meta
        public string SaveTimestamp;
        public int SaveVersion;
    }
}
