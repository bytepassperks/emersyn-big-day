using UnityEngine;
using System;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Centralized reward distribution system. Handles coins, XP, stars, level-ups,
    /// streak bonuses, and reward multipliers. All game rewards flow through here.
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        public static RewardSystem Instance { get; private set; }

        [Header("Multipliers")]
        public float CoinMultiplier = 1f;
        public float XPMultiplier = 1f;
        public float StreakBonusPerDay = 0.1f;
        public float MaxStreakBonus = 2f;

        [Header("Level Scaling")]
        public int BaseXPPerLevel = 100;
        public float XPScaleFactor = 1.2f;

        [Header("Interaction Rewards")]
        public int TapRewardCoins = 1;
        public int PetRewardCoins = 2;
        public int FeedRewardCoins = 3;
        public int MiniGameBaseReward = 10;

        public event Action<RewardData> OnRewardGranted;
        public event Action<int> OnLevelUp;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Grant a reward with all multipliers applied.
        /// </summary>
        public RewardData GrantReward(string source, int baseCoins, int baseXP, int baseStars = 0)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;

            float streakBonus = Mathf.Min(gm.ConsecutiveLoginDays * StreakBonusPerDay, MaxStreakBonus);
            float totalMultiplier = 1f + streakBonus;

            int coins = Mathf.CeilToInt(baseCoins * CoinMultiplier * totalMultiplier);
            int xp = Mathf.CeilToInt(baseXP * XPMultiplier * totalMultiplier);
            int stars = baseStars;

            gm.AddCoins(coins);
            gm.AddXP(xp);
            gm.Stars += stars;

            // Check for level up
            CheckLevelUp();

            var reward = new RewardData
            {
                Source = source,
                Coins = coins,
                XP = xp,
                Stars = stars,
                MultiplierApplied = totalMultiplier
            };

            OnRewardGranted?.Invoke(reward);
            return reward;
        }

        /// <summary>
        /// Grant reward for character interaction.
        /// </summary>
        public void GrantInteractionReward(string interactionType)
        {
            switch (interactionType.ToLower())
            {
                case "tap":
                case "poke":
                    GrantReward("interaction_tap", TapRewardCoins, 1);
                    break;
                case "pet":
                    GrantReward("interaction_pet", PetRewardCoins, 2);
                    break;
                case "feed":
                    GrantReward("interaction_feed", FeedRewardCoins, 3);
                    break;
                case "tickle":
                    GrantReward("interaction_tickle", TapRewardCoins + 1, 2);
                    break;
                case "dress":
                    GrantReward("interaction_dress", PetRewardCoins, 2);
                    break;
                default:
                    GrantReward($"interaction_{interactionType}", TapRewardCoins, 1);
                    break;
            }
        }

        /// <summary>
        /// Grant reward for mini-game completion.
        /// </summary>
        public void GrantMiniGameReward(string gameName, int score, int stars, bool won)
        {
            int coins = won ? MiniGameBaseReward + stars * 5 : MiniGameBaseReward / 2;
            int xp = 5 + stars * 3;
            GrantReward($"minigame_{gameName}", coins, xp, stars >= 3 ? 1 : 0);
        }

        /// <summary>
        /// Grant reward for need satisfaction.
        /// </summary>
        public void GrantNeedReward(string needName, float satisfactionAmount)
        {
            int coins = Mathf.CeilToInt(satisfactionAmount * 0.1f);
            int xp = Mathf.CeilToInt(satisfactionAmount * 0.2f);
            GrantReward($"need_{needName}", coins, xp);
        }

        private void CheckLevelUp()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            while (gm.XP >= gm.XPToNextLevel)
            {
                gm.XP -= gm.XPToNextLevel;
                gm.Level++;
                gm.XPToNextLevel = Mathf.CeilToInt(BaseXPPerLevel * Mathf.Pow(XPScaleFactor, gm.Level - 1));

                OnLevelUp?.Invoke(gm.Level);

                // Level up rewards
                gm.AddCoins(gm.Level * 10);
                gm.Stars += 1;

                // Show popup
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowLevelUpPopup(gm.Level);
                }

                // Effects
                if (Particles.ParticleManager.Instance != null)
                {
                    Particles.ParticleManager.Instance.SpawnLevelUp(Vector3.up * 2f);
                    Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                }

                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySFX("levelup");
                }

                // Achievement
                if (AchievementSystem.Instance != null)
                {
                    AchievementSystem.Instance.AddProgress("level_up");
                    if (gm.Level >= 10) AchievementSystem.Instance.CheckAchievement("reach_level_10");
                    if (gm.Level >= 25) AchievementSystem.Instance.CheckAchievement("reach_level_25");
                    if (gm.Level >= 50) AchievementSystem.Instance.CheckAchievement("reach_level_50");
                }

                if (CameraSystem.CameraController.Instance != null)
                {
                    CameraSystem.CameraController.Instance.ShakeMedium();
                }
            }
        }

        /// <summary>
        /// Set temporary multiplier (e.g., from daily event or power-up).
        /// </summary>
        public void SetTemporaryMultiplier(float coinMult, float xpMult, float duration)
        {
            CoinMultiplier = coinMult;
            XPMultiplier = xpMult;
            Invoke(nameof(ResetMultipliers), duration);
        }

        private void ResetMultipliers()
        {
            CoinMultiplier = 1f;
            XPMultiplier = 1f;
        }
    }

    [Serializable]
    public class RewardData
    {
        public string Source;
        public int Coins;
        public int XP;
        public int Stars;
        public float MultiplierApplied;
    }
}
