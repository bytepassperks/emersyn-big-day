using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #7: Room unlock progression system.
    /// Start with 2 rooms (Bedroom, Kitchen), earn/buy the rest.
    /// Like Toca Life's $3.99-$7.99 world packs and Sims FreePlay's level-gated areas.
    /// </summary>
    public class RoomProgressionSystem : MonoBehaviour
    {
        public static RoomProgressionSystem Instance { get; private set; }

        private Dictionary<string, RoomUnlockData> roomData = new Dictionary<string, RoomUnlockData>();
        private int totalUnlocked;

        public event Action<string> OnRoomUnlocked;

        public int TotalUnlocked => totalUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeRoomData();
        }

        private void InitializeRoomData()
        {
            // Starting rooms (free)
            roomData["Bedroom"] = new RoomUnlockData("Bedroom", 0, 0, true);
            roomData["Kitchen"] = new RoomUnlockData("Kitchen", 0, 0, true);

            // Level-gated rooms
            roomData["Bathroom"] = new RoomUnlockData("Bathroom", 2, 50, false);
            roomData["Park"] = new RoomUnlockData("Park", 3, 100, false);
            roomData["School"] = new RoomUnlockData("School", 5, 200, false);
            roomData["Arcade"] = new RoomUnlockData("Arcade", 7, 300, false);
            roomData["Studio"] = new RoomUnlockData("Studio", 10, 400, false);
            roomData["Shop"] = new RoomUnlockData("Shop", 4, 150, false);
            roomData["Garden"] = new RoomUnlockData("Garden", 6, 250, false);

            // Load saved unlock state
            foreach (var kvp in roomData)
            {
                if (PlayerPrefs.GetInt($"room_unlocked_{kvp.Key}", kvp.Value.IsUnlocked ? 1 : 0) == 1)
                {
                    kvp.Value.IsUnlocked = true;
                }
            }

            CountUnlocked();
        }

        private void CountUnlocked()
        {
            totalUnlocked = 0;
            foreach (var data in roomData.Values)
                if (data.IsUnlocked) totalUnlocked++;
        }

        public bool IsRoomUnlocked(string roomName)
        {
            return roomData.ContainsKey(roomName) && roomData[roomName].IsUnlocked;
        }

        public bool CanUnlockRoom(string roomName)
        {
            if (!roomData.ContainsKey(roomName)) return false;
            var data = roomData[roomName];
            if (data.IsUnlocked) return false;

            var gm = Core.GameManager.Instance;
            if (gm == null) return false;

            return gm.Level >= data.RequiredLevel && gm.Coins >= data.CoinCost;
        }

        public bool UnlockRoom(string roomName)
        {
            if (!CanUnlockRoom(roomName)) return false;

            var data = roomData[roomName];
            var gm = Core.GameManager.Instance;

            gm.Coins -= data.CoinCost;
            data.IsUnlocked = true;
            totalUnlocked++;

            // Save
            PlayerPrefs.SetInt($"room_unlocked_{roomName}", 1);
            PlayerPrefs.Save();

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
            {
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
                Visual.ProceduralParticles.Instance.SpawnStarBurst(Vector3.up * 2f);
            }
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");

            // Quest progress
            if (Gameplay.QuestSystem.Instance != null)
                Gameplay.QuestSystem.Instance.ReportProgress("unlock_room");

            // Achievement
            if (Core.AchievementSystem.Instance != null)
            {
                Core.AchievementSystem.Instance.AddProgress("room_unlocked");
                if (totalUnlocked >= 9)
                    Core.AchievementSystem.Instance.CheckAchievement("all_rooms_unlocked");
            }

            OnRoomUnlocked?.Invoke(roomName);
            Debug.Log($"[RoomProgression] Unlocked: {roomName} (cost: {data.CoinCost} coins)");
            return true;
        }

        public int GetRoomCost(string roomName)
        {
            return roomData.ContainsKey(roomName) ? roomData[roomName].CoinCost : 0;
        }

        public int GetRequiredLevel(string roomName)
        {
            return roomData.ContainsKey(roomName) ? roomData[roomName].RequiredLevel : 0;
        }

        public List<string> GetUnlockedRooms()
        {
            var result = new List<string>();
            foreach (var kvp in roomData)
                if (kvp.Value.IsUnlocked) result.Add(kvp.Key);
            return result;
        }

        public List<string> GetLockedRooms()
        {
            var result = new List<string>();
            foreach (var kvp in roomData)
                if (!kvp.Value.IsUnlocked) result.Add(kvp.Key);
            return result;
        }
    }

    [Serializable]
    public class RoomUnlockData
    {
        public string RoomName;
        public int RequiredLevel;
        public int CoinCost;
        public bool IsUnlocked;

        public RoomUnlockData(string name, int level, int cost, bool unlocked)
        {
            RoomName = name; RequiredLevel = level; CoinCost = cost; IsUnlocked = unlocked;
        }
    }
}
