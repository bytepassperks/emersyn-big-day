using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Manages random daily events, daily rewards, seasonal events, and surprise moments.
    /// Keeps gameplay fresh and non-repetitive with weighted random selection.
    /// </summary>
    public class DailyEventSystem : MonoBehaviour
    {
        public static DailyEventSystem Instance { get; private set; }

        [Header("Daily Events")]
        public DailyEvent[] PossibleEvents;
        public int MaxEventsPerDay = 3;
        public float EventCheckInterval = 120f; // Check every 2 minutes

        [Header("Daily Rewards")]
        public DailyReward[] DailyRewards;
        public int MaxConsecutiveDays = 7;

        [Header("Seasonal")]
        public SeasonalEvent[] SeasonalEvents;

        private List<DailyEvent> todaysEvents = new List<DailyEvent>();
        private int eventsTriggeredToday = 0;
        private float eventTimer;
        private string lastEventDate;
        private bool dailyRewardClaimed = false;

        public event Action<DailyEvent> OnEventTriggered;
        public event Action<DailyReward> OnDailyRewardClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            CheckNewDay();
            GenerateDailyEvents();
        }

        private void Update()
        {
            eventTimer += Time.deltaTime;
            if (eventTimer >= EventCheckInterval)
            {
                eventTimer = 0f;
                TryTriggerEvent();
            }
        }

        // --- DAILY EVENTS ---
        private void GenerateDailyEvents()
        {
            todaysEvents.Clear();
            if (PossibleEvents == null || PossibleEvents.Length == 0) return;

            // Weighted random selection
            List<DailyEvent> pool = new List<DailyEvent>(PossibleEvents);
            int count = Mathf.Min(MaxEventsPerDay, pool.Count);

            for (int i = 0; i < count; i++)
            {
                float totalWeight = 0f;
                foreach (var e in pool) totalWeight += e.Weight;

                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float cumulative = 0f;

                for (int j = 0; j < pool.Count; j++)
                {
                    cumulative += pool[j].Weight;
                    if (roll <= cumulative)
                    {
                        todaysEvents.Add(pool[j]);
                        pool.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        private void TryTriggerEvent()
        {
            if (eventsTriggeredToday >= todaysEvents.Count) return;

            DailyEvent evt = todaysEvents[eventsTriggeredToday];

            // Check conditions
            if (evt.MinLevel > 0 && GameManager.Instance != null && GameManager.Instance.Level < evt.MinLevel) return;
            if (evt.RequiredRoom != "" && Rooms.RoomManager.Instance != null &&
                Rooms.RoomManager.Instance.CurrentRoom != null &&
                Rooms.RoomManager.Instance.CurrentRoom.RoomName != evt.RequiredRoom) return;

            // Trigger event
            eventsTriggeredToday++;
            OnEventTriggered?.Invoke(evt);

            // Apply event effects
            ApplyEventEffects(evt);

            // Show popup
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.ShowEventPopup(evt.EventName, evt.Description);
            }

            // Particles
            if (Particles.ParticleManager.Instance != null)
            {
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
            }

            // Audio
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("achievement");
            }
        }

        private void ApplyEventEffects(DailyEvent evt)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (evt.BonusCoins > 0) gm.AddCoins(evt.BonusCoins);
            if (evt.BonusXP > 0) gm.AddXP(evt.BonusXP);
            if (evt.BonusStars > 0) gm.Stars += evt.BonusStars;

            // Apply need effects
            if (evt.NeedEffects != null)
            {
                var needSystem = FindFirstObjectByType<NeedSystem>();
                if (needSystem != null)
                {
                    foreach (var effect in evt.NeedEffects)
                    {
                        needSystem.SatisfyNeed(effect.NeedName, effect.Amount);
                    }
                }
            }
        }

        public void ForceEvent(string eventName)
        {
            if (PossibleEvents == null) return;
            foreach (var evt in PossibleEvents)
            {
                if (evt.EventName == eventName)
                {
                    OnEventTriggered?.Invoke(evt);
                    ApplyEventEffects(evt);
                    return;
                }
            }
        }

        // --- DAILY REWARDS ---
        private void CheckNewDay()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (today != lastEventDate)
            {
                lastEventDate = today;
                eventsTriggeredToday = 0;
                dailyRewardClaimed = false;

                var gm = GameManager.Instance;
                if (gm != null) gm.CurrentDay++;
            }
        }

        public void ClaimDailyReward()
        {
            if (dailyRewardClaimed || DailyRewards == null || DailyRewards.Length == 0) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            int dayIndex = Mathf.Min(gm.ConsecutiveLoginDays, MaxConsecutiveDays) - 1;
            if (dayIndex < 0) dayIndex = 0;
            dayIndex = dayIndex % DailyRewards.Length;

            DailyReward reward = DailyRewards[dayIndex];
            dailyRewardClaimed = true;

            gm.AddCoins(reward.Coins);
            gm.AddXP(reward.XP);
            gm.Stars += reward.Stars;

            OnDailyRewardClaimed?.Invoke(reward);

            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.ShowRewardPopup(
                    $"Day {dayIndex + 1} Reward!",
                    reward.Coins, reward.Stars, reward.XP
                );
            }

            if (Particles.ParticleManager.Instance != null)
            {
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                Particles.ParticleManager.Instance.SpawnStarBurst(Vector3.up * 2f);
            }
        }

        // --- SEASONAL EVENTS ---
        public SeasonalEvent GetCurrentSeasonalEvent()
        {
            if (SeasonalEvents == null) return null;
            int month = DateTime.UtcNow.Month;
            int day = DateTime.UtcNow.Day;

            foreach (var se in SeasonalEvents)
            {
                if (month >= se.StartMonth && month <= se.EndMonth &&
                    day >= se.StartDay && day <= se.EndDay)
                {
                    return se;
                }
            }
            return null;
        }
    }

    [Serializable]
    public class DailyEvent
    {
        public string EventName;
        public string Description;
        public DailyEventType Type;
        public float Weight = 1f;
        public int MinLevel = 0;
        public string RequiredRoom = "";
        public int BonusCoins;
        public int BonusXP;
        public int BonusStars;
        public NeedEffect[] NeedEffects;
        public string SpecialAnimation;
    }

    [Serializable]
    public class NeedEffect
    {
        public string NeedName;
        public float Amount;
    }

    public enum DailyEventType
    {
        SurpriseParty,   // Random party with confetti and bonus coins
        RainyDay,        // Weather change, cozy indoor activities boosted
        FieldTrip,       // Bonus outdoor activities
        PetArrives,      // New pet companion for the day
        FriendVisits,    // NPC friend comes to play
        BakeSale,        // Cooking games give double rewards
        TalentShow,      // Performance mini-games boosted
        TreasureHunt,    // Hidden items to find around rooms
        PajamaParty,     // Nighttime themed activities
        ArtExhibit       // Art creations get showcased
    }

    [Serializable]
    public class DailyReward
    {
        public string DayLabel;
        public int Coins;
        public int Stars;
        public int XP;
        public string BonusItem;
        public Sprite RewardIcon;
    }

    [Serializable]
    public class SeasonalEvent
    {
        public string EventName;
        public string Description;
        public int StartMonth;
        public int StartDay;
        public int EndMonth;
        public int EndDay;
        public Material SeasonalSkybox;
        public GameObject[] SeasonalDecorations;
    }
}
