using UnityEngine;
using System;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #30: Daily reward / login streak system.
    /// Incentivizes daily play with escalating rewards.
    /// Like every top mobile game's daily login bonus system.
    /// </summary>
    public class DailyRewardSystem : MonoBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        [Header("State")]
        public int CurrentStreak;
        public int MaxStreak;
        public bool ClaimedToday;

        private DailyReward[] rewards;

        public event Action<DailyReward, int> OnRewardClaimed; // reward, streakDay

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeRewards();
            LoadState();
            CheckDailyReset();
        }

        private void InitializeRewards()
        {
            rewards = new DailyReward[]
            {
                new DailyReward(1, 10, 0, 5, ""),          // Day 1: 10 coins, 5 XP
                new DailyReward(2, 15, 0, 8, ""),          // Day 2: 15 coins, 8 XP
                new DailyReward(3, 20, 1, 10, ""),         // Day 3: 20 coins, 1 star, 10 XP
                new DailyReward(4, 25, 0, 12, ""),         // Day 4: 25 coins, 12 XP
                new DailyReward(5, 30, 1, 15, "random_sticker"), // Day 5: 30 coins, 1 star, sticker
                new DailyReward(6, 40, 0, 20, ""),         // Day 6: 40 coins, 20 XP
                new DailyReward(7, 50, 2, 25, "random_outfit"), // Day 7: 50 coins, 2 stars, outfit!
            };
        }

        private void CheckDailyReset()
        {
            string lastLogin = PlayerPrefs.GetString("last_login_date", "");
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            if (lastLogin == today)
            {
                ClaimedToday = PlayerPrefs.GetInt("claimed_today", 0) == 1;
                return;
            }

            if (lastLogin == yesterday)
            {
                // Continue streak
                ClaimedToday = false;
            }
            else if (string.IsNullOrEmpty(lastLogin))
            {
                // First time
                CurrentStreak = 0;
                ClaimedToday = false;
            }
            else
            {
                // Streak broken
                CurrentStreak = 0;
                ClaimedToday = false;
            }

            PlayerPrefs.SetString("last_login_date", today);
            PlayerPrefs.SetInt("claimed_today", 0);
            PlayerPrefs.Save();
        }

        public bool ClaimDailyReward()
        {
            if (ClaimedToday) return false;

            CurrentStreak++;
            if (CurrentStreak > MaxStreak) MaxStreak = CurrentStreak;
            ClaimedToday = true;

            int rewardIndex = ((CurrentStreak - 1) % rewards.Length);
            var reward = rewards[rewardIndex];

            // Grant rewards
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(reward.Coins);
                gm.AddXP(reward.XP);
                gm.Stars += reward.Stars;
            }

            // Special item reward
            if (!string.IsNullOrEmpty(reward.SpecialItemId))
            {
                if (Gameplay.CollectionSystem.Instance != null)
                    Gameplay.CollectionSystem.Instance.CollectItem("stickers_emoji", $"daily_{CurrentStreak}");
            }

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
            {
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
                Visual.ProceduralParticles.Instance.SpawnStarBurst(Vector3.up * 2f);
            }
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");

            // Save
            SaveState();

            OnRewardClaimed?.Invoke(reward, CurrentStreak);
            Debug.Log($"[DailyReward] Day {CurrentStreak}: +{reward.Coins} coins, +{reward.Stars} stars, +{reward.XP} XP");
            return true;
        }

        public DailyReward GetTodayReward()
        {
            int idx = (CurrentStreak % rewards.Length);
            return rewards[idx];
        }

        public int GetStreakDay() => CurrentStreak;

        private void SaveState()
        {
            PlayerPrefs.SetInt("daily_streak", CurrentStreak);
            PlayerPrefs.SetInt("daily_max_streak", MaxStreak);
            PlayerPrefs.SetInt("claimed_today", ClaimedToday ? 1 : 0);
            PlayerPrefs.SetString("last_login_date", DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            CurrentStreak = PlayerPrefs.GetInt("daily_streak", 0);
            MaxStreak = PlayerPrefs.GetInt("daily_max_streak", 0);
        }
    }

    [Serializable]
    public class DailyReward
    {
        public int Day;
        public int Coins;
        public int Stars;
        public int XP;
        public string SpecialItemId;

        public DailyReward(int day, int coins, int stars, int xp, string specialItem)
        {
            Day = day; Coins = coins; Stars = stars; XP = xp; SpecialItemId = specialItem;
        }
    }
}
