using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Tracks and awards achievements based on gameplay milestones.
    /// Persistent progress tracking with notification popups.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [Header("Achievements")]
        public AchievementData[] AllAchievements;

        private Dictionary<string, AchievementProgress> progress = new Dictionary<string, AchievementProgress>();

        public event Action<AchievementData> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeProgress();
        }

        private void InitializeProgress()
        {
            if (AllAchievements == null) return;
            foreach (var ach in AllAchievements)
            {
                if (!progress.ContainsKey(ach.AchievementId))
                {
                    progress[ach.AchievementId] = new AchievementProgress
                    {
                        CurrentValue = 0,
                        IsUnlocked = false
                    };
                }
            }
        }

        /// <summary>
        /// Increment progress toward an achievement.
        /// </summary>
        public void AddProgress(string achievementId, int amount = 1)
        {
            if (!progress.ContainsKey(achievementId)) return;
            var p = progress[achievementId];
            if (p.IsUnlocked) return;

            p.CurrentValue += amount;

            // Find achievement data
            AchievementData data = null;
            foreach (var ach in AllAchievements)
            {
                if (ach.AchievementId == achievementId) { data = ach; break; }
            }

            if (data != null && p.CurrentValue >= data.TargetValue)
            {
                UnlockAchievement(data, p);
            }
        }

        /// <summary>
        /// Check and set a one-time achievement.
        /// </summary>
        public void CheckAchievement(string achievementId)
        {
            if (!progress.ContainsKey(achievementId)) return;
            var p = progress[achievementId];
            if (p.IsUnlocked) return;

            AchievementData data = null;
            foreach (var ach in AllAchievements)
            {
                if (ach.AchievementId == achievementId) { data = ach; break; }
            }

            if (data != null)
            {
                p.CurrentValue = data.TargetValue;
                UnlockAchievement(data, p);
            }
        }

        private void UnlockAchievement(AchievementData data, AchievementProgress p)
        {
            p.IsUnlocked = true;
            p.UnlockDate = DateTime.UtcNow.ToString("o");

            // Grant rewards
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(data.RewardCoins);
                gm.AddXP(data.RewardXP);
                gm.Stars += data.RewardStars;
            }

            // Show popup
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.ShowAchievementPopup(data.DisplayName, data.Description);
            }

            // Effects
            if (Particles.ParticleManager.Instance != null)
            {
                Particles.ParticleManager.Instance.SpawnStarBurst(Vector3.up * 2f);
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
            }

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("achievement");
            }

            OnAchievementUnlocked?.Invoke(data);
        }

        public float GetProgress(string achievementId)
        {
            if (!progress.ContainsKey(achievementId)) return 0f;
            AchievementData data = null;
            foreach (var ach in AllAchievements)
            {
                if (ach.AchievementId == achievementId) { data = ach; break; }
            }
            if (data == null || data.TargetValue <= 0) return 0f;
            return (float)progress[achievementId].CurrentValue / data.TargetValue;
        }

        public bool IsUnlocked(string achievementId)
        {
            return progress.ContainsKey(achievementId) && progress[achievementId].IsUnlocked;
        }

        public int GetUnlockedCount()
        {
            int count = 0;
            foreach (var p in progress.Values) { if (p.IsUnlocked) count++; }
            return count;
        }

        public List<string> GetUnlockedAchievementIds()
        {
            var ids = new List<string>();
            foreach (var kvp in progress)
            {
                if (kvp.Value.IsUnlocked) ids.Add(kvp.Key);
            }
            return ids;
        }
    }

    [Serializable]
    public class AchievementData
    {
        public string AchievementId;
        public string DisplayName;
        public string Description;
        public AchievementCategory Category;
        public Sprite Icon;
        public int TargetValue = 1;
        public int RewardCoins;
        public int RewardXP;
        public int RewardStars;
        public bool IsHidden;
    }

    public class AchievementProgress
    {
        public int CurrentValue;
        public bool IsUnlocked;
        public string UnlockDate;
    }

    public enum AchievementCategory
    {
        General,        // First steps, tutorial completion
        Social,         // Friend interactions, social activities
        Creative,       // Art, music, fashion achievements
        Explorer,       // Room discovery, item finding
        MiniGame,       // Mini-game mastery
        Collection,     // Collecting items, stickers
        Care,           // Pet care, need management
        Milestone       // Level milestones, day streaks
    }
}
