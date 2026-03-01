using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Sims-style need/stat system. Each character has needs that decay over time
    /// and can be satisfied by interacting with objects in the world.
    /// </summary>
    [Serializable]
    public class Need
    {
        public string Name;
        public float Value;        // 0-100
        public float DecayRate;    // per second
        public float MinValue = 0f;
        public float MaxValue = 100f;
        public Sprite Icon;

        public float Normalized => (Value - MinValue) / (MaxValue - MinValue);
        public bool IsCritical => Value < 20f;
        public bool IsLow => Value < 40f;
        public bool IsFull => Value >= 95f;

        public event Action<float> OnValueChanged;

        public Need(string name, float initial, float decayRate)
        {
            Name = name;
            Value = initial;
            DecayRate = decayRate;
        }

        public void Update(float deltaTime)
        {
            float prev = Value;
            Value = Mathf.Clamp(Value - DecayRate * deltaTime, MinValue, MaxValue);
            if (Mathf.Abs(prev - Value) > 0.01f)
                OnValueChanged?.Invoke(Value);
        }

        public void Add(float amount)
        {
            float prev = Value;
            Value = Mathf.Clamp(Value + amount, MinValue, MaxValue);
            if (Mathf.Abs(prev - Value) > 0.01f)
                OnValueChanged?.Invoke(Value);
        }

        public void Set(float value)
        {
            float prev = Value;
            Value = Mathf.Clamp(value, MinValue, MaxValue);
            if (Mathf.Abs(prev - Value) > 0.01f)
                OnValueChanged?.Invoke(Value);
        }
    }

    /// <summary>
    /// Manages all needs for a character. Provides Sims-style stat decay,
    /// mood calculation, and object advertisement scoring.
    /// </summary>
    public class NeedSystem : MonoBehaviour
    {
        [Header("Needs")]
        public Need Hunger = new Need("Hunger", 80f, 0.8f);
        public Need Energy = new Need("Energy", 90f, 0.5f);
        public Need Hygiene = new Need("Hygiene", 85f, 0.3f);
        public Need Fun = new Need("Fun", 70f, 1.0f);
        public Need Social = new Need("Social", 60f, 0.6f);
        public Need Comfort = new Need("Comfort", 75f, 0.4f);
        public Need Bladder = new Need("Bladder", 90f, 1.2f);
        public Need Creativity = new Need("Creativity", 50f, 0.7f);

        [Header("Mood")]
        public float OverallMood = 75f; // 0-100, computed from all needs
        public MoodState CurrentMood = MoodState.Happy;

        [Header("Settings")]
        public float DecayMultiplier = 1f;
        public bool PauseDecay = false;

        // Events
        public event Action<Need> OnNeedCritical;
        public event Action<MoodState> OnMoodChanged;
        public event Action<string, float> OnNeedChanged;

        private List<Need> allNeeds;
        private MoodState previousMood;

        private void Awake()
        {
            allNeeds = new List<Need>
            {
                Hunger, Energy, Hygiene, Fun, Social, Comfort, Bladder, Creativity
            };
        }

        private void Update()
        {
            if (PauseDecay) return;

            float dt = Time.deltaTime * DecayMultiplier;

            foreach (var need in allNeeds)
            {
                float prev = need.Value;
                need.Update(dt);

                if (need.IsCritical && prev >= 20f)
                {
                    OnNeedCritical?.Invoke(need);
                }

                if (Mathf.Abs(prev - need.Value) > 0.5f)
                {
                    OnNeedChanged?.Invoke(need.Name, need.Value);
                }
            }

            // Recalculate mood
            UpdateMood();
        }

        private void UpdateMood()
        {
            // Weighted average of all needs
            float total = 0f;
            float weights = 0f;

            float[] needWeights = { 1.5f, 1.3f, 0.8f, 1.2f, 1.0f, 0.7f, 1.4f, 0.6f };

            for (int i = 0; i < allNeeds.Count; i++)
            {
                total += allNeeds[i].Value * needWeights[i];
                weights += needWeights[i];
            }

            OverallMood = total / weights;

            // Determine mood state
            MoodState newMood;
            if (OverallMood >= 80f) newMood = MoodState.Ecstatic;
            else if (OverallMood >= 65f) newMood = MoodState.Happy;
            else if (OverallMood >= 50f) newMood = MoodState.Content;
            else if (OverallMood >= 35f) newMood = MoodState.Uncomfortable;
            else if (OverallMood >= 20f) newMood = MoodState.Sad;
            else newMood = MoodState.Miserable;

            if (newMood != previousMood)
            {
                previousMood = newMood;
                CurrentMood = newMood;
                OnMoodChanged?.Invoke(newMood);
            }
        }

        /// <summary>
        /// Score how much an object would benefit this character (Sims advertisement system).
        /// Higher score = character more likely to use this object autonomously.
        /// </summary>
        public float ScoreAdvertisement(string needName, float satisfyAmount)
        {
            Need need = GetNeed(needName);
            if (need == null) return 0f;

            // Lower need value = higher urgency = higher score
            float urgency = 1f - need.Normalized;
            float benefit = satisfyAmount / 100f;

            // Exponential urgency for critical needs
            if (need.IsCritical)
                urgency *= 3f;
            else if (need.IsLow)
                urgency *= 1.5f;

            return urgency * benefit * 100f;
        }

        public Need GetNeed(string name)
        {
            return allNeeds.Find(n => n.Name == name);
        }

        public Need GetLowestNeed()
        {
            Need lowest = allNeeds[0];
            foreach (var need in allNeeds)
            {
                if (need.Value < lowest.Value)
                    lowest = need;
            }
            return lowest;
        }

        public Need GetMostUrgentNeed()
        {
            Need mostUrgent = null;
            float highestUrgency = 0f;

            float[] urgencyWeights = { 1.5f, 1.3f, 0.8f, 1.2f, 1.0f, 0.7f, 1.4f, 0.6f };

            for (int i = 0; i < allNeeds.Count; i++)
            {
                float urgency = (1f - allNeeds[i].Normalized) * urgencyWeights[i];
                if (urgency > highestUrgency)
                {
                    highestUrgency = urgency;
                    mostUrgent = allNeeds[i];
                }
            }

            return mostUrgent;
        }

        public List<Need> GetCriticalNeeds()
        {
            return allNeeds.FindAll(n => n.IsCritical);
        }

        public void SatisfyNeed(string name, float amount)
        {
            Need need = GetNeed(name);
            need?.Add(amount);
        }

        public void SatisfyAllNeeds(float amount)
        {
            foreach (var need in allNeeds)
            {
                need.Add(amount);
            }
        }
    }

    public enum MoodState
    {
        Ecstatic,    // 80-100
        Happy,       // 65-80
        Content,     // 50-65
        Uncomfortable, // 35-50
        Sad,         // 20-35
        Miserable    // 0-20
    }
}
