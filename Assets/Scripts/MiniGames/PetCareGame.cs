using UnityEngine;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Pet Care: feed, wash, play with, and groom a pet animal.
    /// Each action has a mini-interaction (drag food to mouth, scrub with sponge, throw ball).
    /// Satisfies Social and Comfort needs.
    /// </summary>
    public class PetCareGame : MonoBehaviour
    {
        [Header("Settings")]
        public float GameDuration = 40f;
        public int TasksToComplete = 4;

        [Header("Pet")]
        public GameObject PetObject;
        public Animator PetAnimator;

        [Header("Items")]
        public GameObject FoodBowlPrefab;
        public GameObject SoapPrefab;
        public GameObject BallPrefab;
        public GameObject BrushPrefab;

        [Header("UI")]
        public TMPro.TextMeshProUGUI TaskText;
        public UnityEngine.UI.Slider PetHappinessBar;

        private PetTask currentTask;
        private int tasksCompleted = 0;
        private float petHappiness = 0f;
        private float gameTimer;
        private int score = 0;
        private bool isActive = false;

        public enum PetTask { Feed, Wash, Play, Groom }

        public void StartGame()
        {
            gameTimer = GameDuration;
            tasksCompleted = 0;
            petHappiness = 0f;
            score = 0;
            isActive = true;
            AssignNextTask();
        }

        private void Update()
        {
            if (!isActive) return;
            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f) { EndGame(); return; }
            if (PetHappinessBar != null) PetHappinessBar.value = petHappiness / 100f;
        }

        private void AssignNextTask()
        {
            if (tasksCompleted >= TasksToComplete) { EndGame(); return; }
            currentTask = (PetTask)(tasksCompleted % 4);
            string taskName = currentTask switch
            {
                PetTask.Feed => "Feed the pet!",
                PetTask.Wash => "Give the pet a bath!",
                PetTask.Play => "Play fetch with the pet!",
                PetTask.Groom => "Brush the pet's fur!",
                _ => "Take care of the pet!"
            };
            if (TaskText != null) TaskText.text = taskName;
        }

        public void OnTaskAction(Vector3 actionPosition)
        {
            if (!isActive) return;

            // Simple proximity check to pet
            if (PetObject == null) return;
            float dist = Vector3.Distance(actionPosition, PetObject.transform.position);
            if (dist > 3f) return;

            switch (currentTask)
            {
                case PetTask.Feed:
                    if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime("Eat", 0.2f);
                    petHappiness += 25f;
                    score += 25;
                    break;
                case PetTask.Wash:
                    if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime("Shake", 0.2f);
                    petHappiness += 25f;
                    score += 25;
                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnBubbles(PetObject.transform.position + Vector3.up);
                    break;
                case PetTask.Play:
                    if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime("Jump", 0.2f);
                    petHappiness += 25f;
                    score += 30;
                    break;
                case PetTask.Groom:
                    if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime("Happy", 0.2f);
                    petHappiness += 25f;
                    score += 20;
                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnSparkles(PetObject.transform.position);
                    break;
            }

            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
            tasksCompleted++;
            AssignNextTask();
        }

        private void EndGame()
        {
            isActive = false;
            petHappiness = Mathf.Clamp(petHappiness, 0f, 100f);

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(petHappiness >= 75f);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Social", 25f);
                needSystem.SatisfyNeed("Comfort", 15f);
            }
        }
    }
}
