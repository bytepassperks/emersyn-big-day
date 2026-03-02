using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #15: Analytics & KPI tracking system.
    /// Tracks session data, retention metrics, popular rooms/games, economy balance.
    /// Like how top mobile games track DAU, retention, ARPU, session length.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        [Header("Session Data")]
        public float SessionStartTime;
        public float TotalPlayTime;
        public int SessionCount;
        public int DaysPlayed;

        [Header("Engagement Metrics")]
        public int TotalTaps;
        public int RoomVisits;
        public int MiniGamesPlayed;
        public int ItemsPurchased;
        public int QuestsCompleted;
        public int PhotosTaken;

        [Header("Economy")]
        public int TotalCoinsEarned;
        public int TotalCoinsSpent;
        public int TotalStarsEarned;

        private Dictionary<string, int> roomVisitCounts = new Dictionary<string, int>();
        private Dictionary<string, int> gamePlayCounts = new Dictionary<string, int>();
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        private float lastSaveTime;

        public event Action<string, Dictionary<string, object>> OnAnalyticsEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadAnalytics();
            SessionStartTime = Time.realtimeSinceStartup;
            SessionCount++;
        }

        private void Update()
        {
            TotalPlayTime += Time.deltaTime;

            // Auto-save every 30 seconds
            if (Time.realtimeSinceStartup - lastSaveTime > 30f)
            {
                lastSaveTime = Time.realtimeSinceStartup;
                SaveAnalytics();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAnalytics();
        }

        private void OnApplicationQuit()
        {
            SaveAnalytics();
        }

        // --- EVENT TRACKING ---

        public void TrackEvent(string eventName, Dictionary<string, object> data = null)
        {
            if (!eventCounts.ContainsKey(eventName))
                eventCounts[eventName] = 0;
            eventCounts[eventName]++;

            OnAnalyticsEvent?.Invoke(eventName, data);
        }

        public void TrackTap() { TotalTaps++; }

        public void TrackRoomVisit(string roomName)
        {
            RoomVisits++;
            if (!roomVisitCounts.ContainsKey(roomName))
                roomVisitCounts[roomName] = 0;
            roomVisitCounts[roomName]++;

            TrackEvent("room_visit", new Dictionary<string, object> { { "room", roomName } });
        }

        public void TrackMiniGame(string gameName, int score, bool won)
        {
            MiniGamesPlayed++;
            if (!gamePlayCounts.ContainsKey(gameName))
                gamePlayCounts[gameName] = 0;
            gamePlayCounts[gameName]++;

            TrackEvent("minigame_complete", new Dictionary<string, object>
            {
                { "game", gameName }, { "score", score }, { "won", won }
            });
        }

        public void TrackPurchase(string itemId, int cost)
        {
            ItemsPurchased++;
            TotalCoinsSpent += cost;
            TrackEvent("purchase", new Dictionary<string, object>
            {
                { "item", itemId }, { "cost", cost }
            });
        }

        public void TrackCoinsEarned(int amount)
        {
            TotalCoinsEarned += amount;
        }

        public void TrackQuestComplete(string questId)
        {
            QuestsCompleted++;
            TrackEvent("quest_complete", new Dictionary<string, object> { { "quest", questId } });
        }

        // --- KPI CALCULATIONS ---

        public float GetSessionLength()
        {
            return Time.realtimeSinceStartup - SessionStartTime;
        }

        public float GetAverageSessionLength()
        {
            return SessionCount > 0 ? TotalPlayTime / SessionCount : 0f;
        }

        public string GetMostPopularRoom()
        {
            string best = "Bedroom";
            int maxVisits = 0;
            foreach (var kvp in roomVisitCounts)
            {
                if (kvp.Value > maxVisits) { maxVisits = kvp.Value; best = kvp.Key; }
            }
            return best;
        }

        public string GetMostPlayedGame()
        {
            string best = "";
            int maxPlays = 0;
            foreach (var kvp in gamePlayCounts)
            {
                if (kvp.Value > maxPlays) { maxPlays = kvp.Value; best = kvp.Key; }
            }
            return best;
        }

        public float GetEconomyBalance()
        {
            return TotalCoinsSpent > 0 ? (float)TotalCoinsEarned / TotalCoinsSpent : 1f;
        }

        // --- PERSISTENCE ---

        private void SaveAnalytics()
        {
            PlayerPrefs.SetFloat("analytics_playtime", TotalPlayTime);
            PlayerPrefs.SetInt("analytics_sessions", SessionCount);
            PlayerPrefs.SetInt("analytics_taps", TotalTaps);
            PlayerPrefs.SetInt("analytics_room_visits", RoomVisits);
            PlayerPrefs.SetInt("analytics_minigames", MiniGamesPlayed);
            PlayerPrefs.SetInt("analytics_purchases", ItemsPurchased);
            PlayerPrefs.SetInt("analytics_quests", QuestsCompleted);
            PlayerPrefs.SetInt("analytics_coins_earned", TotalCoinsEarned);
            PlayerPrefs.SetInt("analytics_coins_spent", TotalCoinsSpent);
            PlayerPrefs.Save();
        }

        private void LoadAnalytics()
        {
            TotalPlayTime = PlayerPrefs.GetFloat("analytics_playtime", 0f);
            SessionCount = PlayerPrefs.GetInt("analytics_sessions", 0);
            TotalTaps = PlayerPrefs.GetInt("analytics_taps", 0);
            RoomVisits = PlayerPrefs.GetInt("analytics_room_visits", 0);
            MiniGamesPlayed = PlayerPrefs.GetInt("analytics_minigames", 0);
            ItemsPurchased = PlayerPrefs.GetInt("analytics_purchases", 0);
            QuestsCompleted = PlayerPrefs.GetInt("analytics_quests", 0);
            TotalCoinsEarned = PlayerPrefs.GetInt("analytics_coins_earned", 0);
            TotalCoinsSpent = PlayerPrefs.GetInt("analytics_coins_spent", 0);
        }
    }
}
