using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Animation
{
    /// <summary>
    /// Enhancement #19: Activity-specific animation controller.
    /// Manages animation states for eating, sleeping, bathing, playing, etc.
    /// Like Sims' activity animations and Talking Tom's interaction sequences.
    /// </summary>
    public class ActivityAnimations : MonoBehaviour
    {
        public static ActivityAnimations Instance { get; private set; }

        [Header("Settings")]
        public float AnimationBlendTime = 0.25f;

        private Dictionary<string, ActivityAnimData> animations = new Dictionary<string, ActivityAnimData>();
        private string currentActivity = "idle";
        private float activityTimer;

        public event System.Action<string> OnActivityStarted;
        public event System.Action<string> OnActivityEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeAnimations();
        }

        private void InitializeAnimations()
        {
            // Eating animations
            animations["eat"] = new ActivityAnimData("eat", 3f,
                new[] { "ReachForFood", "PickUp", "BringToMouth", "Chew", "Swallow", "Happy" },
                "Hunger", 30f);

            // Sleeping animations
            animations["sleep"] = new ActivityAnimData("sleep", 5f,
                new[] { "Yawn", "SitDown", "LayDown", "CloseEyes", "SleepBreathing", "SleepZ" },
                "Energy", 40f);

            // Bathing animations
            animations["bathe"] = new ActivityAnimData("bathe", 4f,
                new[] { "WalkToBath", "Undress", "GetIn", "Scrub", "Splash", "GetOut", "DryOff" },
                "Hygiene", 50f);

            // Dancing animations
            animations["dance"] = new ActivityAnimData("dance", 3f,
                new[] { "BopHead", "ArmWave", "Spin", "Jump", "FreeStyle", "Pose" },
                "Fun", 25f);

            // Cooking animations
            animations["cook"] = new ActivityAnimData("cook", 4f,
                new[] { "OpenFridge", "GatherIngredients", "Chop", "Stir", "Season", "Plate" },
                "Hunger", 35f);

            // Drawing/art animations
            animations["draw"] = new ActivityAnimData("draw", 3f,
                new[] { "SitDown", "PickUpBrush", "Think", "Paint", "AddDetail", "ShowOff" },
                "Creativity", 30f);

            // Pet care animations
            animations["pet_care"] = new ActivityAnimData("pet_care", 3f,
                new[] { "CallPet", "KneelDown", "Pet", "Feed", "Play", "Hug" },
                "Social", 25f);

            // Karate animations (Emersyn's special!)
            animations["karate"] = new ActivityAnimData("karate", 3f,
                new[] { "Bow", "Stance", "Punch", "Kick", "Block", "KarateChop", "Bow" },
                "Fun", 30f);

            // Reading animations
            animations["read"] = new ActivityAnimData("read", 3f,
                new[] { "PickUpBook", "SitDown", "OpenBook", "Read", "TurnPage", "CloseBook" },
                "Creativity", 20f);

            // Gardening animations
            animations["garden"] = new ActivityAnimData("garden", 4f,
                new[] { "KneelDown", "Dig", "PlantSeed", "Water", "WipeForehead", "StandUp" },
                "Comfort", 25f);

            // Shopping animations
            animations["shop"] = new ActivityAnimData("shop", 2f,
                new[] { "Browse", "PickUp", "Examine", "AddToCart", "Pay", "Happy" },
                "Fun", 15f);

            // Social interaction animations
            animations["chat"] = new ActivityAnimData("chat", 3f,
                new[] { "Wave", "Approach", "Talk", "Listen", "Laugh", "Wave" },
                "Social", 30f);
        }

        /// <summary>
        /// Start an activity animation sequence.
        /// </summary>
        public void StartActivity(string activityName, Transform character = null)
        {
            if (!animations.ContainsKey(activityName)) return;

            currentActivity = activityName;
            activityTimer = 0f;

            var anim = animations[activityName];

            // Voice reaction at start
            if (Audio.CharacterVoiceSystem.Instance != null)
                Audio.CharacterVoiceSystem.Instance.Speak("Emersyn", Audio.VoiceEmotion.Excited);

            OnActivityStarted?.Invoke(activityName);

            // Start the activity coroutine
            StartCoroutine(PlayActivitySequence(anim, character));
        }

        private System.Collections.IEnumerator PlayActivitySequence(ActivityAnimData anim, Transform character)
        {
            float stepDuration = anim.Duration / anim.AnimationSteps.Length;

            for (int i = 0; i < anim.AnimationSteps.Length; i++)
            {
                string step = anim.AnimationSteps[i];

                // Apply character animation if Animator exists
                if (character != null)
                {
                    var animator = character.GetComponent<Animator>();
                    if (animator != null)
                        animator.CrossFadeInFixedTime(step, AnimationBlendTime);
                }

                // Midpoint particle effect
                if (i == anim.AnimationSteps.Length / 2 && Visual.ProceduralParticles.Instance != null)
                {
                    Vector3 pos = character != null ? character.position + Vector3.up * 1.5f : Vector3.up * 2f;
                    Visual.ProceduralParticles.Instance.SpawnSparkles(pos);
                }

                yield return new WaitForSeconds(stepDuration);
            }

            // Apply need effect on completion
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null && !string.IsNullOrEmpty(anim.NeedAffected))
            {
                needSystem.ModifyNeed(anim.NeedAffected, anim.NeedDelta);
            }

            // Completion effects
            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnHearts(
                    (character != null ? character.position : Vector3.zero) + Vector3.up * 2f);

            // Quest tracking
            if (Gameplay.QuestSystem.Instance != null)
            {
                Gameplay.QuestSystem.Instance.ReportProgress(currentActivity);

                // Map activities to quest objectives
                if (currentActivity == "eat" || currentActivity == "cook")
                    Gameplay.QuestSystem.Instance.ReportProgress("feed");
                if (currentActivity == "bathe")
                    Gameplay.QuestSystem.Instance.ReportProgress("clean");
                if (currentActivity == "dance" || currentActivity == "draw")
                    Gameplay.QuestSystem.Instance.ReportProgress("create");
                if (currentActivity == "garden")
                    Gameplay.QuestSystem.Instance.ReportProgress("garden");
            }

            OnActivityEnded?.Invoke(currentActivity);
            currentActivity = "idle";
        }

        public bool IsDoingActivity() => currentActivity != "idle";
        public string CurrentActivity => currentActivity;
        public List<string> GetAvailableActivities() => new List<string>(animations.Keys);
    }

    [System.Serializable]
    public class ActivityAnimData
    {
        public string ActivityName;
        public float Duration;
        public string[] AnimationSteps;
        public string NeedAffected;
        public float NeedDelta;

        public ActivityAnimData(string name, float duration, string[] steps, string need, float delta)
        {
            ActivityName = name; Duration = duration; AnimationSteps = steps;
            NeedAffected = need; NeedDelta = delta;
        }
    }
}
