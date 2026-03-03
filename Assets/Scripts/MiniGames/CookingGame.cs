using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Cooking mini-game: follow recipe steps by tapping ingredients in order.
    /// Recipes get harder with more steps and time pressure.
    /// Satisfies Hunger need on completion.
    /// </summary>
    public class CookingGame : MonoBehaviour
    {
        [Header("Game Settings")]
        public float TimePerStep = 5f;
        public int MaxMistakes = 3;

        [Header("Recipes")]
        public RecipeData[] Recipes;

        [Header("UI References")]
        public Transform IngredientContainer;
        public GameObject IngredientButtonPrefab;
        public UnityEngine.UI.Image CurrentStepImage;
        public UnityEngine.UI.Text StepText;
        public UnityEngine.UI.Text ScoreText;
        public UnityEngine.UI.Slider TimerBar;

        private RecipeData currentRecipe;
        private int currentStep = 0;
        private int score = 0;
        private int mistakes = 0;
        private float stepTimer;
        private bool isActive = false;
        private List<GameObject> spawnedButtons = new List<GameObject>();

        public void StartGame()
        {
            if (Recipes == null || Recipes.Length == 0) return;

            // Pick random recipe
            currentRecipe = Recipes[UnityEngine.Random.Range(0, Recipes.Length)];
            currentStep = 0;
            score = 0;
            mistakes = 0;
            isActive = true;

            SetupStep();
        }

        private void Update()
        {
            if (!isActive) return;

            stepTimer -= Time.deltaTime;
            if (TimerBar != null) TimerBar.value = stepTimer / TimePerStep;

            if (stepTimer <= 0f)
            {
                OnMistake();
            }
        }

        private void SetupStep()
        {
            if (currentStep >= currentRecipe.Steps.Length)
            {
                CompleteRecipe();
                return;
            }

            var step = currentRecipe.Steps[currentStep];
            if (StepText != null) StepText.text = step.Instruction;
            if (CurrentStepImage != null && step.StepIcon != null) CurrentStepImage.sprite = step.StepIcon;
            stepTimer = TimePerStep;

            // Spawn ingredient buttons (correct + distractors)
            ClearButtons();
            List<string> options = new List<string> { step.CorrectIngredient };

            // Add random distractors
            for (int i = 0; i < 3; i++)
            {
                string distractor = currentRecipe.AllIngredients[UnityEngine.Random.Range(0, currentRecipe.AllIngredients.Length)];
                if (!options.Contains(distractor)) options.Add(distractor);
            }

            // Shuffle
            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string temp = options[i];
                options[i] = options[j];
                options[j] = temp;
            }

            foreach (var opt in options)
            {
                if (IngredientButtonPrefab != null && IngredientContainer != null)
                {
                    var btn = Instantiate(IngredientButtonPrefab, IngredientContainer);
                    var text = btn.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (text != null) text.text = opt;
                    string captured = opt;
                    btn.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() => OnIngredientSelected(captured));
                    spawnedButtons.Add(btn);
                }
            }
        }

        public void OnIngredientSelected(string ingredient)
        {
            if (!isActive) return;

            if (ingredient == currentRecipe.Steps[currentStep].CorrectIngredient)
            {
                // Correct!
                score += Mathf.CeilToInt(10 * (stepTimer / TimePerStep));
                currentStep++;

                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(transform.position);

                SetupStep();
            }
            else
            {
                OnMistake();
            }

            UpdateScoreDisplay();
        }

        private void OnMistake()
        {
            mistakes++;
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("sad");

            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.ShakeSmall();

            if (mistakes >= MaxMistakes)
            {
                FailRecipe();
            }
            else
            {
                stepTimer = TimePerStep; // Reset timer
            }
        }

        private void CompleteRecipe()
        {
            isActive = false;
            ClearButtons();

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(true);
            }

            // Bonus: satisfy hunger
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Hunger", 25f);
        }

        private void FailRecipe()
        {
            isActive = false;
            ClearButtons();

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(false);
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in spawnedButtons)
            {
                if (btn != null) Destroy(btn);
            }
            spawnedButtons.Clear();
        }

        private void UpdateScoreDisplay()
        {
            if (ScoreText != null) ScoreText.text = $"Score: {score}";
        }
    }

    [System.Serializable]
    public class RecipeData
    {
        public string RecipeName;
        public Sprite RecipeIcon;
        public RecipeStep[] Steps;
        public string[] AllIngredients;
    }

    [System.Serializable]
    public class RecipeStep
    {
        public string Instruction;
        public string CorrectIngredient;
        public Sprite StepIcon;
    }
}
