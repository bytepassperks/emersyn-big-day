using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Bubble Pop: tap bubbles to pop them before they float away.
    /// Colored bubbles worth different points. Combo system for consecutive pops.
    /// Satisfies Hygiene need on completion.
    /// </summary>
    public class BubblePopGame : MonoBehaviour
    {
        [Header("Settings")]
        public float SpawnInterval = 0.5f;
        public float BubbleSpeed = 1.5f;
        public float BubbleLifetime = 4f;
        public int MaxBubbles = 15;
        public float GameDuration = 30f;

        [Header("Visuals")]
        public GameObject BubblePrefab;
        public Transform SpawnArea;
        public Vector2 SpawnSize = new Vector2(6f, 1f);

        [Header("Scoring")]
        public int NormalPoints = 10;
        public int GoldenPoints = 50;
        public int RainbowPoints = 100;
        public float GoldenChance = 0.1f;
        public float RainbowChance = 0.03f;

        private List<BubbleData> activeBubbles = new List<BubbleData>();
        private float spawnTimer;
        private float gameTimer;
        private int score = 0;
        private int combo = 0;
        private int totalPopped = 0;
        private bool isActive = false;

        public void StartGame()
        {
            score = 0;
            combo = 0;
            totalPopped = 0;
            gameTimer = GameDuration;
            spawnTimer = 0f;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f) { EndGame(); return; }

            // Spawn bubbles
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SpawnInterval && activeBubbles.Count < MaxBubbles)
            {
                spawnTimer = 0f;
                SpawnBubble();
            }

            // Update bubbles
            for (int i = activeBubbles.Count - 1; i >= 0; i--)
            {
                var bubble = activeBubbles[i];
                if (bubble.BubbleObject == null) { activeBubbles.RemoveAt(i); continue; }

                bubble.Age += Time.deltaTime;
                bubble.BubbleObject.transform.position += Vector3.up * BubbleSpeed * Time.deltaTime;

                // Wobble
                float wobble = Mathf.Sin(Time.time * 3f + bubble.Age * 2f) * 0.3f;
                bubble.BubbleObject.transform.position += Vector3.right * wobble * Time.deltaTime;

                // Scale pulse
                float pulse = 1f + Mathf.Sin(Time.time * 2f + bubble.Age) * 0.05f;
                bubble.BubbleObject.transform.localScale = Vector3.one * bubble.Size * pulse;

                // Remove if too old
                if (bubble.Age >= BubbleLifetime)
                {
                    combo = 0; // Reset combo on miss
                    Destroy(bubble.BubbleObject);
                    activeBubbles.RemoveAt(i);
                }
            }
        }

        private void SpawnBubble()
        {
            if (BubblePrefab == null) return;

            Vector3 pos = SpawnArea != null ? SpawnArea.position : Vector3.zero;
            pos += new Vector3(
                UnityEngine.Random.Range(-SpawnSize.x / 2f, SpawnSize.x / 2f),
                UnityEngine.Random.Range(-SpawnSize.y / 2f, SpawnSize.y / 2f),
                0f
            );

            GameObject bubbleObj = Instantiate(BubblePrefab, pos, Quaternion.identity);
            float size = UnityEngine.Random.Range(0.5f, 1.2f);
            bubbleObj.transform.localScale = Vector3.one * size;

            // Determine type
            BubbleType type = BubbleType.Normal;
            float roll = UnityEngine.Random.value;
            if (roll < RainbowChance) type = BubbleType.Rainbow;
            else if (roll < GoldenChance) type = BubbleType.Golden;

            // Set color based on type
            var renderer = bubbleObj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                switch (type)
                {
                    case BubbleType.Golden:
                        renderer.material.color = new Color(1f, 0.85f, 0.2f, 0.7f);
                        break;
                    case BubbleType.Rainbow:
                        renderer.material.color = new Color(1f, 0.4f, 0.8f, 0.7f);
                        break;
                    default:
                        Color[] colors = { Color.cyan, Color.blue, Color.green, Color.magenta, new Color(0.5f, 0.8f, 1f) };
                        renderer.material.color = colors[UnityEngine.Random.Range(0, colors.Length)] * new Color(1, 1, 1, 0.7f);
                        break;
                }
            }

            activeBubbles.Add(new BubbleData
            {
                BubbleObject = bubbleObj,
                Type = type,
                Size = size,
                Age = 0f
            });
        }

        public void OnBubbleTapped(GameObject bubbleObj)
        {
            if (!isActive) return;

            BubbleData bubble = null;
            int bubbleIndex = -1;
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                if (activeBubbles[i].BubbleObject == bubbleObj)
                {
                    bubble = activeBubbles[i];
                    bubbleIndex = i;
                    break;
                }
            }

            if (bubble == null) return;

            combo++;
            totalPopped++;

            int points = bubble.Type switch
            {
                BubbleType.Golden => GoldenPoints,
                BubbleType.Rainbow => RainbowPoints,
                _ => NormalPoints
            };

            // Combo multiplier
            points += (combo - 1) * 5;
            score += points;

            // Effects
            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnBubbles(bubble.BubbleObject.transform.position);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("tap");

            Destroy(bubble.BubbleObject);
            activeBubbles.RemoveAt(bubbleIndex);

            if (MiniGameManager.Instance != null)
                MiniGameManager.Instance.AddScore(points);
        }

        private void EndGame()
        {
            isActive = false;

            // Clean up
            foreach (var bubble in activeBubbles)
            {
                if (bubble.BubbleObject != null) Destroy(bubble.BubbleObject);
            }
            activeBubbles.Clear();

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.CompleteGame(totalPopped >= 10);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Hygiene", 20f);
        }

        public enum BubbleType { Normal, Golden, Rainbow }

        public class BubbleData
        {
            public GameObject BubbleObject;
            public BubbleType Type;
            public float Size;
            public float Age;
        }
    }
}
