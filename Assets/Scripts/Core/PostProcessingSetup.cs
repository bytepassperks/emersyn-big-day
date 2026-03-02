using UnityEngine;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Post-processing quality manager for Built-in Render Pipeline.
    /// URP Volume/Bloom/Vignette removed per Claude guidance (caused class layout incompatibility).
    /// Uses QualitySettings + RenderSettings for visual quality control.
    /// </summary>
    public class PostProcessingSetup : MonoBehaviour
    {
        public static PostProcessingSetup Instance { get; private set; }

        [Header("Quality Presets")]
        public QualityPreset CurrentPreset = QualityPreset.Medium;

        public enum QualityPreset { Low, Medium, High, Ultra }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ApplyQualityPreset(CurrentPreset);
        }

        public void ApplyQualityPreset(QualityPreset preset)
        {
            CurrentPreset = preset;

            switch (preset)
            {
                case QualityPreset.Low:
                    SetQuality(shadows: false, msaa: 1, shadowDist: 20f);
                    break;
                case QualityPreset.Medium:
                    SetQuality(shadows: true, msaa: 2, shadowDist: 40f);
                    break;
                case QualityPreset.High:
                    SetQuality(shadows: true, msaa: 4, shadowDist: 60f);
                    break;
                case QualityPreset.Ultra:
                    SetQuality(shadows: true, msaa: 8, shadowDist: 80f);
                    break;
            }
        }

        private void SetQuality(bool shadows, int msaa, float shadowDist)
        {
            QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;
            QualitySettings.antiAliasing = msaa;
            QualitySettings.shadowDistance = shadowDist;
        }

        // --- Stub methods for compatibility with other scripts ---
        public void EnableDoF(float focusDist) { /* No-op without URP Volume */ }
        public void DisableDoF() { /* No-op without URP Volume */ }
        public void SetBloomIntensity(float intensity) { /* No-op without URP Volume */ }
        public void SetVignetteIntensity(float intensity) { /* No-op without URP Volume */ }
        public void SetSaturation(float saturation) { /* No-op without URP Volume */ }

        /// <summary>
        /// Flash screen white briefly (for level up, achievement, etc.)
        /// Uses camera background color flash since URP Volume is removed.
        /// </summary>
        public void FlashScreen(float duration = 0.3f)
        {
            StartCoroutine(FlashCoroutine(duration));
        }

        private System.Collections.IEnumerator FlashCoroutine(float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            Color originalBg = cam.backgroundColor;
            cam.backgroundColor = Color.white;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cam.backgroundColor = Color.Lerp(Color.white, originalBg, timer / duration);
                yield return null;
            }
            cam.backgroundColor = originalBg;
        }

        /// <summary>
        /// Auto-detect device capability and set appropriate quality.
        /// </summary>
        public void AutoDetectQuality()
        {
            int gpuMemory = SystemInfo.graphicsMemorySize;
            int processorCount = SystemInfo.processorCount;

            if (gpuMemory >= 4096 && processorCount >= 6)
                ApplyQualityPreset(QualityPreset.Ultra);
            else if (gpuMemory >= 2048 && processorCount >= 4)
                ApplyQualityPreset(QualityPreset.High);
            else if (gpuMemory >= 1024)
                ApplyQualityPreset(QualityPreset.Medium);
            else
                ApplyQualityPreset(QualityPreset.Low);
        }
    }
}
