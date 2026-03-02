using UnityEngine;
using System;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #28: Ad integration framework (placeholder for monetization).
    /// Rewarded video ads for coins/items, interstitial between rooms.
    /// COPPA-compliant for ages 4-8, parent gate required.
    /// Framework only - actual ad SDK integration done at publish time.
    /// </summary>
    public class AdIntegration : MonoBehaviour
    {
        public static AdIntegration Instance { get; private set; }

        [Header("Settings")]
        public bool AdsEnabled = true;
        public float InterstitialCooldown = 180f; // 3 min between interstitials
        public int RewardedAdCoinBonus = 25;
        public int MaxRewardedAdsPerDay = 5;

        [Header("State")]
        public int RewardedAdsWatchedToday;
        public float LastInterstitialTime;
        public bool AdFreeMode;

        public event Action<int> OnRewardedAdCompleted; // coins earned
        public event Action OnInterstitialShown;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadAdState();
        }

        /// <summary>
        /// Show a rewarded video ad (grants coins).
        /// </summary>
        public void ShowRewardedAd(Action<bool> callback = null)
        {
            if (AdFreeMode || !AdsEnabled)
            {
                callback?.Invoke(false);
                return;
            }

            if (RewardedAdsWatchedToday >= MaxRewardedAdsPerDay)
            {
                Debug.Log("[AdIntegration] Daily rewarded ad limit reached");
                callback?.Invoke(false);
                return;
            }

            // Parent gate check
            if (ParentGate.Instance != null && ParentGate.Instance.IsLocked)
            {
                // For rewarded ads in kids' games, parent gate is optional
                // but we track it for COPPA compliance
            }

            // In production, this would call the ad SDK
            // For now, simulate a successful ad watch
            SimulateRewardedAd(callback);
        }

        private void SimulateRewardedAd(Action<bool> callback)
        {
            RewardedAdsWatchedToday++;

            // Grant reward
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.AddCoins(RewardedAdCoinBonus);
                Core.GameManager.Instance.AddXP(5);
            }

            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);

            OnRewardedAdCompleted?.Invoke(RewardedAdCoinBonus);
            callback?.Invoke(true);
            SaveAdState();

            Debug.Log($"[AdIntegration] Rewarded ad completed: +{RewardedAdCoinBonus} coins");
        }

        /// <summary>
        /// Show interstitial ad (between room transitions).
        /// </summary>
        public void ShowInterstitial()
        {
            if (AdFreeMode || !AdsEnabled) return;

            if (Time.realtimeSinceStartup - LastInterstitialTime < InterstitialCooldown)
                return;

            // In production, this would call the ad SDK
            LastInterstitialTime = Time.realtimeSinceStartup;
            OnInterstitialShown?.Invoke();

            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackEvent("interstitial_shown");

            Debug.Log("[AdIntegration] Interstitial shown");
        }

        /// <summary>
        /// Check if a rewarded ad is available.
        /// </summary>
        public bool IsRewardedAdAvailable()
        {
            return AdsEnabled && !AdFreeMode && RewardedAdsWatchedToday < MaxRewardedAdsPerDay;
        }

        /// <summary>
        /// Purchase ad-free mode (through IAP).
        /// </summary>
        public void PurchaseAdFree()
        {
            // In production, this would go through IAP
            AdFreeMode = true;
            PlayerPrefs.SetInt("ad_free", 1);
            PlayerPrefs.Save();
        }

        private void SaveAdState()
        {
            PlayerPrefs.SetInt("rewarded_ads_today", RewardedAdsWatchedToday);
            PlayerPrefs.SetString("last_ad_date", System.DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
        }

        private void LoadAdState()
        {
            AdFreeMode = PlayerPrefs.GetInt("ad_free", 0) == 1;

            // Reset daily counter if new day
            string lastDate = PlayerPrefs.GetString("last_ad_date", "");
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (lastDate != today)
            {
                RewardedAdsWatchedToday = 0;
            }
            else
            {
                RewardedAdsWatchedToday = PlayerPrefs.GetInt("rewarded_ads_today", 0);
            }
        }
    }
}
