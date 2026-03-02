using UnityEngine;
using System;

namespace EmersynBigDay.Animation
{
    /// <summary>
    /// Enhancement #11: Emotional animation state machine.
    /// Drives character animations based on mood, needs, and context.
    /// Smooth transitions between emotional states with blend trees.
    /// Like Animal Crossing's reactive character behavior and Sims' mood system.
    /// </summary>
    public class EmotionalAnimator : MonoBehaviour
    {
        public static EmotionalAnimator Instance { get; private set; }

        [Header("Current State")]
        public EmotionalState CurrentState = EmotionalState.Happy;
        public float StateIntensity = 0.5f;

        [Header("Transition")]
        public float TransitionSpeed = 2f;
        public float IdleCheckInterval = 3f;

        private EmotionalState targetState;
        private float targetIntensity;
        private float idleTimer;
        private Core.NeedSystem cachedNeedSystem;

        public event Action<EmotionalState, float> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            targetState = CurrentState;
            targetIntensity = StateIntensity;
        }

        private void Update()
        {
            UpdateStateFromNeeds();
            SmoothTransition();
            UpdateIdleBehavior();
        }

        /// <summary>
        /// Read needs and mood to determine emotional state.
        /// </summary>
        private void UpdateStateFromNeeds()
        {
            if (cachedNeedSystem == null) cachedNeedSystem = FindObjectOfType<Core.NeedSystem>();
            if (cachedNeedSystem == null) return;
            var needSystem = cachedNeedSystem;

            string moodState = needSystem.GetMoodState();
            float overallMood = needSystem.GetOverallMood();

            EmotionalState newState;
            float newIntensity;

            // Map mood to emotional state
            switch (moodState)
            {
                case "Ecstatic":
                    newState = EmotionalState.Ecstatic;
                    newIntensity = 1f;
                    break;
                case "Happy":
                    newState = EmotionalState.Happy;
                    newIntensity = 0.7f;
                    break;
                case "Content":
                    newState = EmotionalState.Content;
                    newIntensity = 0.5f;
                    break;
                case "Uncomfortable":
                    newState = EmotionalState.Uncomfortable;
                    newIntensity = 0.6f;
                    break;
                case "Sad":
                    newState = EmotionalState.Sad;
                    newIntensity = 0.7f;
                    break;
                case "Miserable":
                    newState = EmotionalState.Miserable;
                    newIntensity = 1f;
                    break;
                default:
                    newState = EmotionalState.Content;
                    newIntensity = 0.5f;
                    break;
            }

            // Check specific needs for special states
            var hungerNeed = needSystem.GetNeed("Hunger");  
            if (hungerNeed != null && hungerNeed.IsCritical)
            {
                newState = EmotionalState.Hungry;
                newIntensity = 0.8f;
            }

            var energyNeed = needSystem.GetNeed("Energy");
            if (energyNeed != null && energyNeed.Value < 20f)
            {
                newState = EmotionalState.Sleepy;
                newIntensity = 0.9f;
            }

            var funNeed = needSystem.GetNeed("Fun");
            if (funNeed != null && funNeed.IsCritical)
            {
                newState = EmotionalState.Bored;
                newIntensity = 0.7f;
            }

            SetTargetState(newState, newIntensity);
        }

        private void SmoothTransition()
        {
            if (CurrentState != targetState)
            {
                float speed = TransitionSpeed * Time.deltaTime;
                StateIntensity = Mathf.MoveTowards(StateIntensity, 0f, speed);

                if (StateIntensity <= 0.05f)
                {
                    CurrentState = targetState;
                    OnStateChanged?.Invoke(CurrentState, targetIntensity);
                }
            }

            StateIntensity = Mathf.MoveTowards(StateIntensity, targetIntensity, TransitionSpeed * Time.deltaTime);
        }

        private void UpdateIdleBehavior()
        {
            idleTimer += Time.deltaTime;
            if (idleTimer < IdleCheckInterval) return;
            idleTimer = 0f;

            // Trigger contextual idle animations
            switch (CurrentState)
            {
                case EmotionalState.Happy:
                case EmotionalState.Ecstatic:
                    if (Random.value < 0.3f) TriggerIdleAnimation("happy_bounce");
                    break;
                case EmotionalState.Sad:
                case EmotionalState.Miserable:
                    if (Random.value < 0.2f) TriggerIdleAnimation("sad_sigh");
                    break;
                case EmotionalState.Sleepy:
                    if (Random.value < 0.4f) TriggerIdleAnimation("yawn");
                    break;
                case EmotionalState.Hungry:
                    if (Random.value < 0.3f) TriggerIdleAnimation("stomach_growl");
                    break;
                case EmotionalState.Bored:
                    if (Random.value < 0.3f) TriggerIdleAnimation("look_around");
                    break;
            }
        }

        private void TriggerIdleAnimation(string animName)
        {
            // Play voice for emotional state
            if (Audio.CharacterVoiceSystem.Instance != null)
            {
                VoiceEmotion voiceEmotion = CurrentState switch
                {
                    EmotionalState.Happy => VoiceEmotion.Happy,
                    EmotionalState.Ecstatic => VoiceEmotion.Excited,
                    EmotionalState.Sad => VoiceEmotion.Sad,
                    EmotionalState.Sleepy => VoiceEmotion.Sleepy,
                    EmotionalState.Miserable => VoiceEmotion.Sad,
                    _ => VoiceEmotion.Neutral
                };
                Audio.CharacterVoiceSystem.Instance.Speak("Emersyn", voiceEmotion);
            }

            // Trigger particle effects for emotions
            if (Visual.ProceduralParticles.Instance != null)
            {
                switch (CurrentState)
                {
                    case EmotionalState.Happy:
                    case EmotionalState.Ecstatic:
                        Visual.ProceduralParticles.Instance.SpawnSparkles(transform.position + Vector3.up * 2f);
                        break;
                    case EmotionalState.Sleepy:
                        Visual.ProceduralParticles.Instance.Play("sleepz", transform.position + Vector3.up * 2.5f);
                        break;
                }
            }
        }

        public void SetTargetState(EmotionalState state, float intensity)
        {
            targetState = state;
            targetIntensity = Mathf.Clamp01(intensity);
        }

        public void ForceState(EmotionalState state, float intensity, float duration)
        {
            CurrentState = state;
            StateIntensity = intensity;
            targetState = state;
            targetIntensity = intensity;
            OnStateChanged?.Invoke(state, intensity);

            if (duration > 0f)
                Invoke(nameof(ClearForceState), duration);
        }

        private void ClearForceState()
        {
            // Let UpdateStateFromNeeds take over again
        }

        private void OnDestroy()
        {
            OnStateChanged = null;
        }

        public string GetAnimationForState()
        {
            return CurrentState switch
            {
                EmotionalState.Ecstatic => "Dance",
                EmotionalState.Happy => "Idle",
                EmotionalState.Content => "Idle",
                EmotionalState.Uncomfortable => "Fidget",
                EmotionalState.Sad => "Sad",
                EmotionalState.Miserable => "Cry",
                EmotionalState.Hungry => "HoldStomach",
                EmotionalState.Sleepy => "Yawn",
                EmotionalState.Bored => "LookAround",
                EmotionalState.Excited => "Jump",
                _ => "Idle"
            };
        }
    }

    public enum EmotionalState
    {
        Ecstatic, Happy, Content, Uncomfortable, Sad, Miserable,
        Hungry, Sleepy, Bored, Excited, Angry, Surprised
    }
}
