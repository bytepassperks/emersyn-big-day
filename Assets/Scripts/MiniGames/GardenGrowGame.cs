using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Garden Grow: plant seeds, water them, and watch flowers grow.
    /// Tap to plant, swipe to water, watch for weeds to pull.
    /// Satisfies Comfort and Creativity needs.
    /// </summary>
    public class GardenGrowGame : MonoBehaviour
    {
        [Header("Settings")]
        public int GardenSlots = 6;
        public float GrowTime = 3f;
        public float WaterInterval = 2f;
        public float WeedChance = 0.2f;
        public float GameDuration = 45f;

        [Header("Visuals")]
        public GameObject SeedPrefab;
        public GameObject SproutPrefab;
        public GameObject FlowerPrefab;
        public GameObject WeedPrefab;
        public GameObject WaterDropPrefab;
        public Transform[] PlotPositions;

        private GardenPlot[] plots;
        private float gameTimer;
        private int score = 0;
        private int flowersGrown = 0;
        private bool isActive = false;

        public void StartGame()
        {
            gameTimer = GameDuration;
            score = 0;
            flowersGrown = 0;
            isActive = true;

            plots = new GardenPlot[GardenSlots];
            for (int i = 0; i < GardenSlots; i++)
            {
                plots[i] = new GardenPlot
                {
                    State = PlotState.Empty,
                    GrowProgress = 0f,
                    WaterLevel = 0f,
                    HasWeed = false,
                    Position = PlotPositions != null && i < PlotPositions.Length
                        ? PlotPositions[i].position : Vector3.right * i * 1.5f
                };
            }
        }

        private void Update()
        {
            if (!isActive) return;

            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f) { EndGame(); return; }

            for (int i = 0; i < plots.Length; i++)
            {
                var plot = plots[i];
                if (plot.State == PlotState.Empty || plot.State == PlotState.Bloomed) continue;

                // Grow if watered
                if (plot.WaterLevel > 0f && !plot.HasWeed)
                {
                    plot.GrowProgress += Time.deltaTime / GrowTime;
                    plot.WaterLevel -= Time.deltaTime * 0.3f;

                    if (plot.GrowProgress >= 0.5f && plot.State == PlotState.Seed)
                    {
                        plot.State = PlotState.Sprout;
                        UpdatePlotVisual(i);
                    }
                    else if (plot.GrowProgress >= 1f && plot.State == PlotState.Sprout)
                    {
                        plot.State = PlotState.Bloomed;
                        flowersGrown++;
                        score += 50;
                        UpdatePlotVisual(i);

                        if (Particles.ParticleManager.Instance != null)
                            Particles.ParticleManager.Instance.SpawnSparkles(plot.Position + Vector3.up);
                        if (Audio.AudioManager.Instance != null)
                            Audio.AudioManager.Instance.PlaySFX("coin");
                    }
                }

                // Random weed spawn
                if (!plot.HasWeed && plot.State != PlotState.Bloomed && UnityEngine.Random.value < WeedChance * Time.deltaTime)
                {
                    plot.HasWeed = true;
                    SpawnWeed(i);
                }
            }
        }

        public void OnPlotTapped(int plotIndex)
        {
            if (!isActive || plotIndex < 0 || plotIndex >= plots.Length) return;
            var plot = plots[plotIndex];

            if (plot.HasWeed)
            {
                // Pull weed
                plot.HasWeed = false;
                score += 10;
                if (plot.WeedObject != null) Destroy(plot.WeedObject);
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("tap");
            }
            else if (plot.State == PlotState.Empty)
            {
                // Plant seed
                plot.State = PlotState.Seed;
                plot.GrowProgress = 0f;
                UpdatePlotVisual(plotIndex);
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("tap");
            }
            else if (plot.State == PlotState.Bloomed)
            {
                // Harvest flower — score tracked locally, passed to MiniGameManager at EndGame
                plot.State = PlotState.Empty;
                plot.GrowProgress = 0f;
                score += 25;
                UpdatePlotVisual(plotIndex);
            }
        }

        public void OnPlotWatered(int plotIndex)
        {
            if (!isActive || plotIndex < 0 || plotIndex >= plots.Length) return;
            var plot = plots[plotIndex];

            if (plot.State != PlotState.Empty && plot.State != PlotState.Bloomed)
            {
                plot.WaterLevel = Mathf.Min(plot.WaterLevel + 0.5f, 1f);
                score += 5;

                // Spawn water drop effect
                if (WaterDropPrefab != null)
                {
                    var drop = Instantiate(WaterDropPrefab, plot.Position + Vector3.up * 2f, Quaternion.identity);
                    Destroy(drop, 1.5f);
                }
            }
        }

        private void UpdatePlotVisual(int plotIndex)
        {
            var plot = plots[plotIndex];

            // Destroy existing visual
            if (plot.VisualObject != null) Destroy(plot.VisualObject);

            GameObject prefab = null;
            switch (plot.State)
            {
                case PlotState.Seed: prefab = SeedPrefab; break;
                case PlotState.Sprout: prefab = SproutPrefab; break;
                case PlotState.Bloomed: prefab = FlowerPrefab; break;
            }

            if (prefab != null)
            {
                plot.VisualObject = Instantiate(prefab, plot.Position, Quaternion.identity);
            }
        }

        private void SpawnWeed(int plotIndex)
        {
            var plot = plots[plotIndex];
            if (WeedPrefab != null)
            {
                plot.WeedObject = Instantiate(WeedPrefab, plot.Position + Vector3.right * 0.3f, Quaternion.identity);
            }
        }

        private void EndGame()
        {
            isActive = false;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(flowersGrown >= 3);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Comfort", 15f);
                needSystem.SatisfyNeed("Creativity", 15f);
            }
        }

        public enum PlotState { Empty, Seed, Sprout, Bloomed }

        public class GardenPlot
        {
            public PlotState State;
            public float GrowProgress;
            public float WaterLevel;
            public bool HasWeed;
            public Vector3 Position;
            public GameObject VisualObject;
            public GameObject WeedObject;
        }
    }
}
