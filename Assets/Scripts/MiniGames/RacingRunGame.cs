using UnityEngine;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Racing Run: endless runner style. Swipe left/right to dodge obstacles,
    /// swipe up to jump, collect coins and power-ups.
    /// Satisfies Fun and Energy needs.
    /// </summary>
    public class RacingRunGame : MonoBehaviour
    {
        [Header("Settings")]
        public float RunSpeed = 6f;
        public float SpeedIncreaseRate = 0.1f;
        public float LaneWidth = 2f;
        public int LaneCount = 3;
        public float JumpForce = 10f;
        public float Gravity = 25f;

        [Header("Prefabs")]
        public GameObject ObstaclePrefab;
        public GameObject CoinPrefab;
        public GameObject SpeedBoostPrefab;
        public GameObject ShieldPrefab;

        [Header("Player")]
        public Transform PlayerTransform;
        public Animator PlayerAnimator;

        private int currentLane = 1; // 0=left, 1=center, 2=right
        private float verticalVelocity = 0f;
        private float groundY = 0f;
        private bool isJumping = false;
        private bool hasShield = false;
        private float shieldTimer = 0f;
        private float currentSpeed;
        private float distanceRun = 0f;
        private int coinsCollected = 0;
        private int score = 0;
        private float spawnTimer = 0f;
        private float spawnInterval = 1.5f;
        private bool isActive = false;

        public void StartGame()
        {
            currentLane = 1;
            currentSpeed = RunSpeed;
            distanceRun = 0f;
            coinsCollected = 0;
            score = 0;
            verticalVelocity = 0f;
            isJumping = false;
            hasShield = false;
            isActive = true;

            if (PlayerTransform != null) groundY = PlayerTransform.position.y;
            if (PlayerAnimator != null) PlayerAnimator.CrossFadeInFixedTime("Run", 0.2f);
        }

        private void Update()
        {
            if (!isActive) return;

            // Increase speed over time
            currentSpeed += SpeedIncreaseRate * Time.deltaTime;
            distanceRun += currentSpeed * Time.deltaTime;
            score = Mathf.CeilToInt(distanceRun) + coinsCollected * 10;

            // Handle jump
            if (isJumping && PlayerTransform != null)
            {
                verticalVelocity -= Gravity * Time.deltaTime;
                Vector3 pos = PlayerTransform.position;
                pos.y += verticalVelocity * Time.deltaTime;
                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    isJumping = false;
                    verticalVelocity = 0f;
                    if (PlayerAnimator != null) PlayerAnimator.CrossFadeInFixedTime("Run", 0.2f);
                }
                PlayerTransform.position = pos;
            }

            // Shield timer
            if (hasShield)
            {
                shieldTimer -= Time.deltaTime;
                if (shieldTimer <= 0f) hasShield = false;
            }

            // Spawn obstacles/coins
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnLaneObjects();
                spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.01f);
            }

            // Score is calculated from distanceRun + coinsCollected and passed at game end
            // No per-frame AddScore call to avoid massive double-counting
        }

        public void SwipeLeft()
        {
            if (!isActive) return;
            if (currentLane > 0)
            {
                currentLane--;
                UpdatePlayerLane();
            }
        }

        public void SwipeRight()
        {
            if (!isActive) return;
            if (currentLane < LaneCount - 1)
            {
                currentLane++;
                UpdatePlayerLane();
            }
        }

        public void Jump()
        {
            if (!isActive || isJumping) return;
            isJumping = true;
            verticalVelocity = JumpForce;
            if (PlayerAnimator != null) PlayerAnimator.CrossFadeInFixedTime("Jump", 0.1f);
        }

        private void UpdatePlayerLane()
        {
            if (PlayerTransform == null) return;
            float targetX = (currentLane - 1) * LaneWidth;
            Vector3 pos = PlayerTransform.position;
            pos.x = targetX; // Instant lane change (could lerp for smoothness)
            PlayerTransform.position = pos;
        }

        private void SpawnLaneObjects()
        {
            float spawnZ = PlayerTransform != null ? PlayerTransform.position.z + 30f : 30f;

            // Random obstacle
            int obstacleLane = UnityEngine.Random.Range(0, LaneCount);
            if (ObstaclePrefab != null)
            {
                float x = (obstacleLane - 1) * LaneWidth;
                Instantiate(ObstaclePrefab, new Vector3(x, groundY, spawnZ), Quaternion.identity);
            }

            // Random coin
            int coinLane = UnityEngine.Random.Range(0, LaneCount);
            if (coinLane != obstacleLane && CoinPrefab != null)
            {
                float x = (coinLane - 1) * LaneWidth;
                Instantiate(CoinPrefab, new Vector3(x, groundY + 1f, spawnZ + UnityEngine.Random.Range(-2f, 2f)), Quaternion.identity);
            }

            // Rare power-up
            if (UnityEngine.Random.value < 0.1f)
            {
                int puLane = UnityEngine.Random.Range(0, LaneCount);
                GameObject prefab = UnityEngine.Random.value < 0.5f ? SpeedBoostPrefab : ShieldPrefab;
                if (prefab != null)
                {
                    float x = (puLane - 1) * LaneWidth;
                    Instantiate(prefab, new Vector3(x, groundY + 1f, spawnZ + 5f), Quaternion.identity);
                }
            }
        }

        public void OnHitObstacle()
        {
            if (hasShield)
            {
                hasShield = false;
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(PlayerTransform != null ? PlayerTransform.position : Vector3.zero);
                return;
            }

            // Game over
            isActive = false;
            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.ShakeLarge();
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("lose");
            if (PlayerAnimator != null) PlayerAnimator.CrossFadeInFixedTime("Fall", 0.1f);

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(distanceRun >= 100f);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Fun", 15f);
        }

        public void OnCollectCoin()
        {
            coinsCollected++;
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnCoinCollect(PlayerTransform != null ? PlayerTransform.position : Vector3.zero);
        }

        public void OnCollectShield()
        {
            hasShield = true;
            shieldTimer = 5f;
        }

        public void OnCollectSpeedBoost()
        {
            currentSpeed *= 1.5f;
            StartCoroutine(ResetSpeed());
        }

        private System.Collections.IEnumerator ResetSpeed()
        {
            yield return new WaitForSeconds(3f);
            currentSpeed = RunSpeed + distanceRun * SpeedIncreaseRate;
        }
    }
}
