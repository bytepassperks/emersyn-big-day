using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #6: Tutorial & onboarding system for ages 4-8.
    /// Visual finger pointers, progressive unlock, zero-text instructions.
    /// Like Toca Life's intuitive first-time experience and Animal Crossing's gentle guidance.
    /// Emersyn is 6 — tutorial must be fun, visual, no reading required.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [Header("State")]
        public bool TutorialComplete;
        public int CurrentStep;
        public bool IsShowingTutorial;

        [Header("Settings")]
        public float PointerBobSpeed = 2f;
        public float PointerBobAmount = 20f;
        public float StepDelay = 1.5f;

        private List<TutorialStep> steps = new List<TutorialStep>();
        private GameObject pointerObject;
        private float pointerTimer;

        public event Action<int> OnStepCompleted;
        public event Action OnTutorialCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeSteps();
        }

        private void Start()
        {
            // Check if tutorial already completed
            if (PlayerPrefs.GetInt("TutorialComplete", 0) == 1)
            {
                TutorialComplete = true;
                return;
            }

            StartTutorial();
        }

        private void Update()
        {
            if (!IsShowingTutorial) return;
            AnimatePointer();
        }

        private void InitializeSteps()
        {
            // Zero-text, visual-only tutorial steps for a 6-year-old
            steps.Add(new TutorialStep(0, "welcome",
                TutorialAction.ShowCharacter, "Meet Emersyn!", Vector3.zero));

            steps.Add(new TutorialStep(1, "tap_emersyn",
                TutorialAction.PointAtTarget, "Tap Emersyn!", Vector3.zero));

            steps.Add(new TutorialStep(2, "feed_emersyn",
                TutorialAction.PointAtButton, "Feed button", new Vector3(0, -400, 0)));

            steps.Add(new TutorialStep(3, "watch_eat",
                TutorialAction.WaitForAnimation, "Watch her eat!", Vector3.zero));

            steps.Add(new TutorialStep(4, "check_needs",
                TutorialAction.PointAtUI, "Need bars", new Vector3(-450, 200, 0)));

            steps.Add(new TutorialStep(5, "play_game",
                TutorialAction.PointAtButton, "Play button", new Vector3(-150, -400, 0)));

            steps.Add(new TutorialStep(6, "change_room",
                TutorialAction.PointAtButton, "Next room arrow", new Vector3(450, 0, 0)));

            steps.Add(new TutorialStep(7, "pet_kitty",
                TutorialAction.PointAtTarget, "Pet the kitty!", Vector3.zero));

            steps.Add(new TutorialStep(8, "open_shop",
                TutorialAction.PointAtButton, "Shop button", new Vector3(300, -400, 0)));

            steps.Add(new TutorialStep(9, "tutorial_done",
                TutorialAction.ShowCelebration, "You did it!", Vector3.zero));
        }

        public void StartTutorial()
        {
            if (TutorialComplete) return;
            IsShowingTutorial = true;
            CurrentStep = 0;
            CreatePointer();
            ShowCurrentStep();
        }

        public void AdvanceStep()
        {
            if (!IsShowingTutorial) return;

            OnStepCompleted?.Invoke(CurrentStep);
            CurrentStep++;

            if (CurrentStep >= steps.Count)
            {
                CompleteTutorial();
                return;
            }

            StartCoroutine(ShowStepDelayed());
        }

        private System.Collections.IEnumerator ShowStepDelayed()
        {
            yield return new WaitForSeconds(StepDelay);
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            if (CurrentStep >= steps.Count) return;
            var step = steps[CurrentStep];

            switch (step.Action)
            {
                case TutorialAction.PointAtTarget:
                case TutorialAction.PointAtButton:
                case TutorialAction.PointAtUI:
                    ShowPointer(step.ScreenPosition);
                    break;

                case TutorialAction.ShowCharacter:
                    // Highlight Emersyn
                    if (Visual.ProceduralParticles.Instance != null)
                        Visual.ProceduralParticles.Instance.SpawnSparkles(Vector3.up * 2f);
                    // Auto-advance after delay
                    Invoke(nameof(AdvanceStep), 2f);
                    break;

                case TutorialAction.WaitForAnimation:
                    // Wait for activity to finish, then auto-advance
                    Invoke(nameof(AdvanceStep), 3f);
                    break;

                case TutorialAction.ShowCelebration:
                    if (Visual.ProceduralParticles.Instance != null)
                    {
                        Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
                        Visual.ProceduralParticles.Instance.SpawnStarBurst(Vector3.up * 2f);
                    }
                    if (Audio.AudioManager.Instance != null)
                        Audio.AudioManager.Instance.PlaySFX("achievement");
                    Invoke(nameof(AdvanceStep), 3f);
                    break;
            }
        }

        private void CompleteTutorial()
        {
            IsShowingTutorial = false;
            TutorialComplete = true;
            PlayerPrefs.SetInt("TutorialComplete", 1);
            PlayerPrefs.Save();

            DestroyPointer();

            // Grant tutorial completion reward
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.AddCoins(50);
                Core.GameManager.Instance.AddXP(25);
            }

            OnTutorialCompleted?.Invoke();
            Debug.Log("[TutorialSystem] Tutorial completed!");
        }

        private void CreatePointer()
        {
            if (pointerObject != null) return;

            // Create a simple pointer indicator (arrow or finger)
            pointerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerObject.name = "TutorialPointer";
            pointerObject.transform.localScale = Vector3.one * 0.3f;

            var renderer = pointerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Use primitive's default material shader (survives IL2CPP shader stripping)
                var mat = new Material(renderer.sharedMaterial);
                mat.color = new Color(1f, 0.8f, 0f, 1f); // Gold
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0f, 1f));
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0f, 0.5f));
                    mat.EnableKeyword("_EMISSION");
                }
                renderer.material = mat;
            }

            // Remove collider (it's just visual)
            var col = pointerObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            pointerObject.SetActive(false);
        }

        private void ShowPointer(Vector3 screenPos)
        {
            if (pointerObject == null) return;
            pointerObject.SetActive(true);

            // Convert screen position to world position
            if (Camera.main != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(Screen.width / 2f + screenPos.x, Screen.height / 2f + screenPos.y, 5f));
                pointerObject.transform.position = worldPos;
            }
        }

        private void AnimatePointer()
        {
            if (pointerObject == null || !pointerObject.activeSelf) return;
            pointerTimer += Time.deltaTime * PointerBobSpeed;
            Vector3 pos = pointerObject.transform.position;
            pos.y += Mathf.Sin(pointerTimer * Mathf.PI * 2f) * PointerBobAmount * Time.deltaTime;
            pointerObject.transform.position = pos;
        }

        private void DestroyPointer()
        {
            if (pointerObject != null) Destroy(pointerObject);
            pointerObject = null;
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        /// <summary>
        /// Check if a specific interaction matches the current tutorial step requirement.
        /// </summary>
        public void ReportInteraction(string interactionType)
        {
            if (!IsShowingTutorial || CurrentStep >= steps.Count) return;

            var step = steps[CurrentStep];
            bool matches = false;

            switch (step.StepId)
            {
                case "tap_emersyn":
                    matches = interactionType == "tap" || interactionType == "poke";
                    break;
                case "feed_emersyn":
                    matches = interactionType == "feed";
                    break;
                case "play_game":
                    matches = interactionType == "play" || interactionType == "play_minigame";
                    break;
                case "change_room":
                    matches = interactionType == "change_room" || interactionType == "visit_room";
                    break;
                case "pet_kitty":
                    matches = interactionType == "pet" || interactionType == "pet_care";
                    break;
                case "open_shop":
                    matches = interactionType == "shop" || interactionType == "buy_item";
                    break;
            }

            if (matches) AdvanceStep();
        }
    }

    [Serializable]
    public class TutorialStep
    {
        public int StepNumber;
        public string StepId;
        public TutorialAction Action;
        public string HintText; // For accessibility, not shown to child
        public Vector3 ScreenPosition;

        public TutorialStep(int num, string id, TutorialAction action, string hint, Vector3 pos)
        {
            StepNumber = num; StepId = id; Action = action; HintText = hint; ScreenPosition = pos;
        }
    }

    public enum TutorialAction
    {
        PointAtTarget, PointAtButton, PointAtUI,
        ShowCharacter, WaitForAnimation, ShowCelebration
    }
}
