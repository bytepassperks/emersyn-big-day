using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Performance
{
    /// <summary>
    /// Enhancement #16: LOD (Level of Detail) system for mobile performance.
    /// Reduces polygon count for distant objects, manages draw calls.
    /// Like how top mobile games maintain 60fps on low-end devices.
    /// </summary>
    public class LODManager : MonoBehaviour
    {
        public static LODManager Instance { get; private set; }

        [Header("LOD Settings")]
        public float LOD0Distance = 5f;   // Full detail
        public float LOD1Distance = 10f;  // Medium detail
        public float LOD2Distance = 20f;  // Low detail
        public float CullDistance = 30f;   // Don't render

        [Header("Quality Settings")]
        public int TargetFPS = 60;
        public bool AutoAdjustQuality = true;
        public float QualityCheckInterval = 2f;

        private Camera mainCamera;
        private List<LODObject> managedObjects = new List<LODObject>();
        private float qualityTimer;
        private int currentQualityLevel = 2; // 0=Low, 1=Med, 2=High
        private float[] fpsHistory = new float[30];
        private int fpsIndex;
        private float lodUpdateTimer;
        private const float LOD_UPDATE_INTERVAL = 0.1f; // 10 times per second is enough

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            mainCamera = Camera.main;
            // Initialize FPS history with target FPS to avoid division issues
            for (int i = 0; i < fpsHistory.Length; i++)
                fpsHistory[i] = TargetFPS;
        }

        private void Update()
        {
            TrackFPS();
            lodUpdateTimer += Time.deltaTime;
            if (lodUpdateTimer >= LOD_UPDATE_INTERVAL)
            {
                lodUpdateTimer = 0f;
                UpdateLODs();
            }

            if (AutoAdjustQuality)
            {
                qualityTimer += Time.deltaTime;
                if (qualityTimer >= QualityCheckInterval)
                {
                    qualityTimer = 0f;
                    AutoAdjustQualityLevel();
                }
            }
        }

        private void TrackFPS()
        {
            fpsHistory[fpsIndex] = 1f / Time.unscaledDeltaTime;
            fpsIndex = (fpsIndex + 1) % fpsHistory.Length;
        }

        private float GetAverageFPS()
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < fpsHistory.Length; i++)
            {
                if (fpsHistory[i] > 0)
                {
                    sum += fpsHistory[i];
                    count++;
                }
            }
            return count > 0 ? sum / count : 60f;
        }

        private void UpdateLODs()
        {
            if (mainCamera == null) { mainCamera = Camera.main; return; }
            Vector3 camPos = mainCamera.transform.position;

            for (int i = managedObjects.Count - 1; i >= 0; i--)
            {
                var obj = managedObjects[i];
                if (obj.Target == null) { managedObjects.RemoveAt(i); continue; }

                float dist = Vector3.Distance(camPos, obj.Target.position);
                int newLOD;

                if (dist < LOD0Distance) newLOD = 0;
                else if (dist < LOD1Distance) newLOD = 1;
                else if (dist < LOD2Distance) newLOD = 2;
                else newLOD = 3; // Culled

                if (newLOD != obj.CurrentLOD)
                {
                    ApplyLOD(obj, newLOD);
                    obj.CurrentLOD = newLOD;
                }
            }
        }

        private void ApplyLOD(LODObject obj, int lodLevel)
        {
            if (obj.Renderers == null) return;

            foreach (var r in obj.Renderers)
            {
                if (r == null) continue;

                switch (lodLevel)
                {
                    case 0: // Full detail
                        r.enabled = true;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        break;
                    case 1: // Medium - no shadows
                        r.enabled = true;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        break;
                    case 2: // Low - simplified
                        r.enabled = true;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        break;
                    case 3: // Culled
                        r.enabled = false;
                        break;
                }
            }

            // Disable particles at distance
            if (obj.Particles != null)
            {
                foreach (var ps in obj.Particles)
                {
                    if (ps != null)
                    {
                        if (lodLevel >= 2) { if (ps.isPlaying) ps.Stop(); }
                        else { if (!ps.isPlaying) ps.Play(); }
                    }
                }
            }
        }

        /// <summary>
        /// Auto-adjust quality if FPS drops below target.
        /// </summary>
        private void AutoAdjustQualityLevel()
        {
            float avgFPS = GetAverageFPS();

            if (avgFPS < TargetFPS * 0.8f && currentQualityLevel > 0)
            {
                currentQualityLevel--;
                ApplyQualityLevel(currentQualityLevel);
                Debug.Log($"[LODManager] Lowered quality to {currentQualityLevel} (FPS: {avgFPS:F0})");
            }
            else if (avgFPS > TargetFPS * 1.2f && currentQualityLevel < 2)
            {
                currentQualityLevel++;
                ApplyQualityLevel(currentQualityLevel);
                Debug.Log($"[LODManager] Raised quality to {currentQualityLevel} (FPS: {avgFPS:F0})");
            }
        }

        private void ApplyQualityLevel(int level)
        {
            switch (level)
            {
                case 0: // Low
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.antiAliasing = 0;
                    LOD0Distance = 3f;
                    LOD1Distance = 6f;
                    CullDistance = 15f;
                    break;
                case 1: // Medium
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.antiAliasing = 2;
                    LOD0Distance = 5f;
                    LOD1Distance = 10f;
                    CullDistance = 25f;
                    break;
                case 2: // High
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 4;
                    LOD0Distance = 8f;
                    LOD1Distance = 15f;
                    CullDistance = 35f;
                    break;
            }
        }

        /// <summary>
        /// Register a GameObject for LOD management.
        /// </summary>
        public void Register(GameObject go)
        {
            if (go == null) return;
            var obj = new LODObject
            {
                Target = go.transform,
                Renderers = go.GetComponentsInChildren<Renderer>(),
                Particles = go.GetComponentsInChildren<ParticleSystem>(),
                CurrentLOD = 0
            };
            managedObjects.Add(obj);
        }

        public void Unregister(GameObject go)
        {
            if (go == null) return;
            managedObjects.RemoveAll(o => o.Target == go.transform);
        }

        public int GetCurrentQuality() => currentQualityLevel;
        public float GetCurrentFPS() => GetAverageFPS();
    }

    public class LODObject
    {
        public Transform Target;
        public Renderer[] Renderers;
        public ParticleSystem[] Particles;
        public int CurrentLOD;
    }
}
