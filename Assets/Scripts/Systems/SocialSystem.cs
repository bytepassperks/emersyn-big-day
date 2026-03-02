using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #23: Social features - friend interactions, gift giving, visiting.
    /// Characters have relationships, can chat, play together, share items.
    /// Like Sims' relationship system and Animal Crossing's friend visits.
    /// Emersyn's friends: Ava, Mia, Leo.
    /// </summary>
    public class SocialSystem : MonoBehaviour
    {
        public static SocialSystem Instance { get; private set; }

        private Dictionary<string, FriendshipData> friendships = new Dictionary<string, FriendshipData>();

        public event Action<string, float> OnFriendshipChanged; // characterName, newLevel
        public event Action<string> OnBestFriendReached;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeFriendships();
        }

        private void InitializeFriendships()
        {
            friendships["Ava"] = new FriendshipData("Ava", 30f, "Shy, loves art and painting");
            friendships["Mia"] = new FriendshipData("Mia", 25f, "Energetic, loves dancing and sports");
            friendships["Leo"] = new FriendshipData("Leo", 20f, "Curious, loves science and puzzles");

            // Load saved data
            foreach (var kvp in friendships)
            {
                float saved = PlayerPrefs.GetFloat($"friendship_{kvp.Key}", kvp.Value.Level);
                kvp.Value.Level = saved;
            }
        }

        public void AddFriendship(string characterName, float amount)
        {
            if (!friendships.ContainsKey(characterName)) return;
            var data = friendships[characterName];
            float oldLevel = data.Level;
            data.Level = Mathf.Clamp(data.Level + amount, 0f, 100f);

            // Save
            PlayerPrefs.SetFloat($"friendship_{characterName}", data.Level);

            // Effects
            if (amount > 0 && Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnHearts(Vector3.up * 2.5f);

            // Check milestones
            if (oldLevel < 50f && data.Level >= 50f)
            {
                // Good friends!
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("achievement");
            }
            if (oldLevel < 100f && data.Level >= 100f)
            {
                // Best friends!
                OnBestFriendReached?.Invoke(characterName);
                if (Visual.ProceduralParticles.Instance != null)
                    Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
                if (Core.AchievementSystem.Instance != null)
                    Core.AchievementSystem.Instance.CheckAchievement($"best_friend_{characterName.ToLower()}");
            }

            OnFriendshipChanged?.Invoke(characterName, data.Level);

            // Quest tracking
            if (Gameplay.QuestSystem.Instance != null)
                Gameplay.QuestSystem.Instance.ReportProgress("social_interact");
        }

        /// <summary>
        /// Interact with a friend (chat, play, share).
        /// </summary>
        public void Interact(string characterName, SocialAction action)
        {
            float friendshipGain;
            switch (action)
            {
                case SocialAction.Chat: friendshipGain = 3f; break;
                case SocialAction.PlayTogether: friendshipGain = 5f; break;
                case SocialAction.GiveGift: friendshipGain = 8f; break;
                case SocialAction.Hug: friendshipGain = 4f; break;
                case SocialAction.Dance: friendshipGain = 6f; break;
                case SocialAction.ShareFood: friendshipGain = 5f; break;
                case SocialAction.HighFive: friendshipGain = 2f; break;
                default: friendshipGain = 1f; break;
            }

            AddFriendship(characterName, friendshipGain);

            // Voice reaction
            if (Audio.CharacterVoiceSystem.Instance != null)
            {
                Audio.CharacterVoiceSystem.Instance.Speak(characterName, Audio.VoiceEmotion.Happy);
                Audio.CharacterVoiceSystem.Instance.Speak("Emersyn", Audio.VoiceEmotion.Happy);
            }

            // Need satisfaction
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.ModifyNeed("Social", friendshipGain * 3f);
                needSystem.ModifyNeed("Fun", friendshipGain * 2f);
            }

            // Analytics
            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackEvent("social_interact",
                    new Dictionary<string, object> { { "friend", characterName }, { "action", action.ToString() } });
        }

        public float GetFriendshipLevel(string characterName)
        {
            return friendships.ContainsKey(characterName) ? friendships[characterName].Level : 0f;
        }

        public string GetFriendshipTier(string characterName)
        {
            float level = GetFriendshipLevel(characterName);
            if (level >= 90f) return "Best Friends";
            if (level >= 70f) return "Great Friends";
            if (level >= 50f) return "Good Friends";
            if (level >= 30f) return "Friends";
            if (level >= 10f) return "Acquaintances";
            return "Just Met";
        }

        public List<string> GetAllFriendNames()
        {
            return new List<string>(friendships.Keys);
        }
    }

    [Serializable]
    public class FriendshipData
    {
        public string CharacterName;
        public float Level; // 0-100
        public string Personality;
        public int InteractionCount;

        public FriendshipData(string name, float level, string personality)
        {
            CharacterName = name; Level = level; Personality = personality;
        }
    }

    public enum SocialAction
    {
        Chat, PlayTogether, GiveGift, Hug, Dance, ShareFood, HighFive, Wave
    }
}
