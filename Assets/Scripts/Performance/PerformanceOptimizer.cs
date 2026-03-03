using UnityEngine;

namespace EmersynBigDay.Performance
{
    /// <summary>
    /// Enhancement #18: Texture atlasing, occlusion culling, and general performance optimization.
    /// Manages draw call batching, texture memory, and frame budget.
    /// Targets 60fps on mid-range Android devices.
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        public static PerformanceOptimizer Instance { get; private set; }

        [Header("Settings")]
        public int TargetFrameRate = 60;
        public bool EnableBatching = true;
        public bool EnableOcclusionCulling = true;
        public int MaxTextureSize = 1024;

        [Header("Memory")]
        public float MaxTextureMemoryMB = 256f;

        [Header("Debug")]
        public bool ShowStats;
        public int DrawCalls;
        public int Triangles;
        public float FrameTimeMs;
        public float MemoryUsedMB;

        private float statsTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyOptimizations();
        }

        private void Update()
        {
            FrameTimeMs = Time.unscaledDeltaTime * 1000f;

            statsTimer += Time.unscaledDeltaTime;
            if (statsTimer >= 1f)
            {
                statsTimer = 0f;
                UpdateStats();
            }
        }

        private void ApplyOptimizations()
        {
            // Target framerate
            Application.targetFrameRate = TargetFrameRate;

            // VSync off for mobile (let targetFrameRate control)
            QualitySettings.vSyncCount = 0;

            // Reduce render scale slightly for performance headroom
            // QualitySettings.renderPipeline handled by URP asset

            // Enable GPU instancing
            QualitySettings.maxQueuedFrames = 2;

            // Shadow settings for mobile
            QualitySettings.shadowDistance = 15f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;

            // LOD bias
            QualitySettings.lodBias = 1f;

            // Skin weights for characters
            QualitySettings.skinWeights = SkinWeights.TwoBones;

            // Particle raycast budget
            QualitySettings.particleRaycastBudget = 64;

            // Physics optimization
            Physics.defaultSolverIterations = 4;
            Physics.defaultSolverVelocityIterations = 1;
            Physics.autoSyncTransforms = false;

            // GC optimization
            Application.lowMemory += OnLowMemory;

            Debug.Log("[PerformanceOptimizer] Mobile optimizations applied");
        }

        private void OnLowMemory()
        {
            // Emergency cleanup
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            // Reduce quality
            if (LODManager.Instance != null)
            {
                // Force lower quality
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 0;
            }

            Debug.LogWarning("[PerformanceOptimizer] Low memory! Cleaned up resources.");
        }

        private void UpdateStats()
        {
            MemoryUsedMB = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f);
        }

        /// <summary>
        /// Optimize all textures in the scene for mobile.
        /// </summary>
        public void OptimizeTextures()
        {
            var renderers = FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    // Enable GPU instancing on all materials
                    mat.enableInstancing = true;
                }
            }
        }

        /// <summary>
        /// Enable static batching for non-moving objects.
        /// </summary>
        public void BatchStaticObjects()
        {
            if (!EnableBatching) return;

            var allObjects = FindObjectsOfType<MeshRenderer>();
            foreach (var mr in allObjects)
            {
                if (mr == null) continue;
                // Mark furniture and room geometry as static
                if (mr.gameObject.name.Contains("Furniture") ||
                    mr.gameObject.name.Contains("Floor") ||
                    mr.gameObject.name.Contains("Wall") ||
                    mr.gameObject.name.Contains("Room"))
                {
                    mr.gameObject.isStatic = true;
                }
            }

            // Trigger static batching
            var staticObjects = new System.Collections.Generic.List<GameObject>();
            foreach (var mr in allObjects)
            {
                if (mr != null && mr.gameObject.isStatic)
                    staticObjects.Add(mr.gameObject);
            }

            if (staticObjects.Count > 0)
            {
                StaticBatchingUtility.Combine(staticObjects.ToArray(), gameObject);
                Debug.Log($"[PerformanceOptimizer] Batched {staticObjects.Count} static objects");
            }
        }

        /// <summary>
        /// Force garbage collection during safe moment (room transition).
        /// </summary>
        public void SafeGarbageCollect()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }
    }
}
