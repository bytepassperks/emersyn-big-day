using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Hide and Seek: find hidden objects scattered around the room.
    /// Objects partially visible, tap to find them. Timer and hint system.
    /// Satisfies Fun and Social needs.
    /// </summary>
    public class HideAndSeekGame : MonoBehaviour
    {
        [Header("Settings")]
        public float GameDuration = 45f;
        public int ObjectsToFind = 8;
        public float HintCooldown = 10f;

        [Header("Visuals")]
        public GameObject[] HideableObjectPrefabs;
        public Transform[] HidingSpots;
        public GameObject FoundEffectPrefab;
        public GameObject HintArrowPrefab;

        [Header("UI")]
        public UnityEngine.UI.Text FoundCountText;
        public UnityEngine.UI.Text TimerText;
        public UnityEngine.UI.Button HintButton;

        private List<HiddenObject> hiddenObjects = new List<HiddenObject>();
        private int objectsFound = 0;
        private float gameTimer;
        private float hintTimer;
        private int score = 0;
        private bool isActive = false;

        public void StartGame()
        {
            objectsFound = 0;
            gameTimer = GameDuration;
            hintTimer = 0f;
            score = 0;
            isActive = true;

            PlaceHiddenObjects();
            UpdateUI();
        }

        private void Update()
        {
            if (!isActive) return;

            gameTimer -= Time.deltaTime;
            if (TimerText != null) TimerText.text = $"{Mathf.CeilToInt(gameTimer)}s";

            if (gameTimer <= 0f)
            {
                EndGame(objectsFound >= ObjectsToFind / 2);
                return;
            }

            hintTimer -= Time.deltaTime;
            if (HintButton != null) HintButton.interactable = hintTimer <= 0f;
        }

        private void PlaceHiddenObjects()
        {
            if (HideableObjectPrefabs == null || HidingSpots == null) return;

            // Shuffle hiding spots
            List<Transform> spots = new List<Transform>(HidingSpots);
            for (int i = spots.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = spots[i];
                spots[i] = spots[j];
                spots[j] = temp;
            }

            int count = Mathf.Min(ObjectsToFind, spots.Count);
            for (int i = 0; i < count; i++)
            {
                int prefabIdx = UnityEngine.Random.Range(0, HideableObjectPrefabs.Length);
                GameObject obj = Instantiate(HideableObjectPrefabs[prefabIdx], spots[i].position, Quaternion.identity);

                // Partially hide (scale down, partially behind cover)
                obj.transform.localScale *= UnityEngine.Random.Range(0.4f, 0.7f);
                obj.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);

                hiddenObjects.Add(new HiddenObject
                {
                    Object = obj,
                    IsFound = false,
                    Position = spots[i].position
                });
            }
        }

        public void OnObjectTapped(GameObject tappedObj)
        {
            if (!isActive) return;

            foreach (var hidden in hiddenObjects)
            {
                if (hidden.IsFound || hidden.Object != tappedObj) continue;

                hidden.IsFound = true;
                objectsFound++;
                score += Mathf.CeilToInt(50 * (gameTimer / GameDuration));

                // Visual feedback
                if (FoundEffectPrefab != null)
                {
                    var effect = Instantiate(FoundEffectPrefab, hidden.Position, Quaternion.identity);
                    Destroy(effect, 2f);
                }

                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(hidden.Position);
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("coin");

                // Scale up found object
                hidden.Object.transform.localScale *= 1.5f;

                UpdateUI();

                // Check win
                if (objectsFound >= ObjectsToFind)
                {
                    EndGame(true);
                }
                return;
            }
        }

        public void UseHint()
        {
            if (!isActive || hintTimer > 0f) return;
            hintTimer = HintCooldown;

            // Find nearest unfound object and show arrow
            foreach (var hidden in hiddenObjects)
            {
                if (!hidden.IsFound)
                {
                    if (HintArrowPrefab != null)
                    {
                        var arrow = Instantiate(HintArrowPrefab, hidden.Position + Vector3.up * 2f, Quaternion.identity);
                        Destroy(arrow, 3f);
                    }
                    break;
                }
            }
        }

        private void UpdateUI()
        {
            if (FoundCountText != null) FoundCountText.text = $"{objectsFound}/{ObjectsToFind}";
        }

        private void EndGame(bool won)
        {
            isActive = false;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(won);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Fun", 20f);
                needSystem.SatisfyNeed("Social", 10f);
            }
        }

        public class HiddenObject
        {
            public GameObject Object;
            public bool IsFound;
            public Vector3 Position;
        }
    }
}
