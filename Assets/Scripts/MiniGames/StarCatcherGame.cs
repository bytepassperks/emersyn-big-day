using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Star Catcher: catch falling stars by moving a basket left/right.
    /// Avoid bombs. Power-ups include magnet, slowdown, and double points.
    /// Satisfies Fun need.
    /// </summary>
    public class StarCatcherGame : MonoBehaviour
    {
        [Header("Settings")]
        public float GameDuration = 30f;
        public float SpawnInterval = 0.6f;
        public float FallSpeed = 4f;
        public float BasketSpeed = 8f;
        public float SpawnWidth = 4f;

        [Header("Prefabs")]
        public GameObject StarPrefab;
        public GameObject GoldStarPrefab;
        public GameObject BombPrefab;
        public GameObject MagnetPrefab;
        public GameObject SlowdownPrefab;
        public GameObject BasketObject;

        [Header("Scoring")]
        public int StarPoints = 10;
        public int GoldStarPoints = 50;
        public int BombPenalty = -30;

        private List<FallingObject> fallingObjects = new List<FallingObject>();
        private float gameTimer;
        private float spawnTimer;
        private int score = 0;
        private int starsCaught = 0;
        private bool hasMagnet = false;
        private float magnetTimer = 0f;
        private float speedMultiplier = 1f;
        private float slowdownTimer = 0f;
        private bool isActive = false;

        public void StartGame()
        {
            gameTimer = GameDuration;
            spawnTimer = 0f;
            score = 0;
            starsCaught = 0;
            hasMagnet = false;
            speedMultiplier = 1f;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f) { EndGame(); return; }

            // Handle power-up timers
            if (hasMagnet) { magnetTimer -= Time.deltaTime; if (magnetTimer <= 0f) hasMagnet = false; }
            if (speedMultiplier < 1f) { slowdownTimer -= Time.deltaTime; if (slowdownTimer <= 0f) speedMultiplier = 1f; }

            // Spawn objects
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SpawnInterval)
            {
                spawnTimer = 0f;
                SpawnObject();
            }

            // Update falling objects
            UpdateFallingObjects();

            // Move basket via touch/input
            UpdateBasketPosition();
        }

        private void SpawnObject()
        {
            float x = UnityEngine.Random.Range(-SpawnWidth, SpawnWidth);
            Vector3 spawnPos = new Vector3(x, 8f, 0f);

            float roll = UnityEngine.Random.value;
            GameObject prefab;
            FallingObjectType type;

            if (roll < 0.05f && MagnetPrefab != null) { prefab = MagnetPrefab; type = FallingObjectType.Magnet; }
            else if (roll < 0.1f && SlowdownPrefab != null) { prefab = SlowdownPrefab; type = FallingObjectType.Slowdown; }
            else if (roll < 0.2f && BombPrefab != null) { prefab = BombPrefab; type = FallingObjectType.Bomb; }
            else if (roll < 0.35f && GoldStarPrefab != null) { prefab = GoldStarPrefab; type = FallingObjectType.GoldStar; }
            else { prefab = StarPrefab; type = FallingObjectType.Star; }

            if (prefab == null) return;

            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            fallingObjects.Add(new FallingObject
            {
                Object = obj,
                Type = type,
                Speed = FallSpeed * UnityEngine.Random.Range(0.8f, 1.2f)
            });
        }

        private void UpdateFallingObjects()
        {
            if (BasketObject == null) return;
            Vector3 basketPos = BasketObject.transform.position;
            float catchRadius = hasMagnet ? 3f : 1f;

            for (int i = fallingObjects.Count - 1; i >= 0; i--)
            {
                var fo = fallingObjects[i];
                if (fo.Object == null) { fallingObjects.RemoveAt(i); continue; }

                // Fall
                fo.Object.transform.position += Vector3.down * fo.Speed * speedMultiplier * Time.deltaTime;

                // Rotate star
                fo.Object.transform.Rotate(0, 0, 90f * Time.deltaTime);

                // Magnet attraction
                if (hasMagnet && fo.Type != FallingObjectType.Bomb)
                {
                    Vector3 dir = (basketPos - fo.Object.transform.position).normalized;
                    fo.Object.transform.position += dir * 2f * Time.deltaTime;
                }

                // Check catch
                float dist = Vector3.Distance(fo.Object.transform.position, basketPos);
                if (dist < catchRadius)
                {
                    CatchObject(fo);
                    fallingObjects.RemoveAt(i);
                    continue;
                }

                // Remove if off screen
                if (fo.Object.transform.position.y < -2f)
                {
                    Destroy(fo.Object);
                    fallingObjects.RemoveAt(i);
                }
            }
        }

        private void CatchObject(FallingObject fo)
        {
            int pointsEarned = 0;

            switch (fo.Type)
            {
                case FallingObjectType.Star:
                    pointsEarned = StarPoints;
                    score += pointsEarned;
                    starsCaught++;
                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnSparkles(fo.Object.transform.position);
                    if (Audio.AudioManager.Instance != null)
                        Audio.AudioManager.Instance.PlaySFX("coin");
                    break;

                case FallingObjectType.GoldStar:
                    pointsEarned = GoldStarPoints;
                    score += pointsEarned;
                    starsCaught++;
                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnStarBurst(fo.Object.transform.position);
                    if (Audio.AudioManager.Instance != null)
                        Audio.AudioManager.Instance.PlaySFX("star");
                    break;

                case FallingObjectType.Bomb:
                    pointsEarned = BombPenalty;
                    score = Mathf.Max(0, score + pointsEarned);
                    if (CameraSystem.CameraController.Instance != null)
                        CameraSystem.CameraController.Instance.ShakeMedium();
                    if (Audio.AudioManager.Instance != null)
                        Audio.AudioManager.Instance.PlaySFX("lose");
                    break;

                case FallingObjectType.Magnet:
                    hasMagnet = true;
                    magnetTimer = 5f;
                    break;

                case FallingObjectType.Slowdown:
                    speedMultiplier = 0.5f;
                    slowdownTimer = 4f;
                    break;
            }

            Destroy(fo.Object);
            // Pass only incremental points to avoid double-counting the total score
            if (MiniGameManager.Instance != null && pointsEarned != 0)
                MiniGameManager.Instance.AddScore(pointsEarned);
        }

        private void UpdateBasketPosition()
        {
            if (BasketObject == null) return;

            float horizontal = 0f;
#if UNITY_EDITOR || UNITY_STANDALONE
            horizontal = UnityEngine.Input.GetAxis("Horizontal");
#else
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                float screenCenter = Screen.width / 2f;
                horizontal = (touch.position.x - screenCenter) / screenCenter;
            }
#endif
            Vector3 pos = BasketObject.transform.position;
            pos.x += horizontal * BasketSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, -SpawnWidth, SpawnWidth);
            BasketObject.transform.position = pos;
        }

        private void EndGame()
        {
            isActive = false;

            foreach (var fo in fallingObjects)
            {
                if (fo.Object != null) Destroy(fo.Object);
            }
            fallingObjects.Clear();

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.CompleteGame(starsCaught >= 10);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Fun", 25f);
        }

        public enum FallingObjectType { Star, GoldStar, Bomb, Magnet, Slowdown }

        public class FallingObject
        {
            public GameObject Object;
            public FallingObjectType Type;
            public float Speed;
        }
    }
}
