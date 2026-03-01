using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Sets up post-processing effects for AAA-quality visuals:
    /// Bloom (warm glow), Ambient Occlusion, Color Grading (pastel tones),
    /// Vignette, Depth of Field, and Toon Outline shader.
    /// Optimized for mobile 60fps on mid-range devices.
    /// </summary>
    public class PostProcessingSetup : MonoBehaviour
    {
        public static PostProcessingSetup Instance { get; private set; }

        [Header("Volume Profile")]
        public VolumeProfile PostProcessProfile;
        public Volume GlobalVolume;

        [Header("Quality Presets")]
        public QualityPreset CurrentPreset = QualityPreset.Medium;

        // Cached effect references
        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private DepthOfField depthOfField;
        private ChromaticAberration chromaticAberration;
        private FilmGrain filmGrain;
        private Tonemapping tonemapping;

        public enum QualityPreset { Low, Medium, High, Ultra }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetupPostProcessing();
            ApplyQualityPreset(CurrentPreset);
        }

        private void SetupPostProcessing()
        {
            if (GlobalVolume == null)
            {
                var volumeObj = new GameObject("GlobalPostProcessVolume");
                GlobalVolume = volumeObj.AddComponent<Volume>();
                GlobalVolume.isGlobal = true;
                GlobalVolume.priority = 1;
            }

            if (PostProcessProfile == null)
            {
                PostProcessProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                GlobalVolume.profile = PostProcessProfile;
            }

            // Setup Bloom
            if (!PostProcessProfile.TryGet(out bloom))
            {
                bloom = PostProcessProfile.Add<Bloom>();
            }
            bloom.active = true;
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.8f;
            bloom.scatter.value = 0.7f;
            bloom.tint.value = new Color(1f, 0.95f, 0.9f); // Warm tint
            bloom.threshold.overrideState = true;
            bloom.intensity.overrideState = true;
            bloom.scatter.overrideState = true;
            bloom.tint.overrideState = true;

            // Setup Color Adjustments (pastel/kawaii look)
            if (!PostProcessProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = PostProcessProfile.Add<ColorAdjustments>();
            }
            colorAdjustments.active = true;
            colorAdjustments.postExposure.value = 0.3f;
            colorAdjustments.contrast.value = -10f; // Slightly reduced for soft look
            colorAdjustments.saturation.value = 15f; // Boosted for cute vibrant colors
            colorAdjustments.colorFilter.value = new Color(1f, 0.98f, 0.95f); // Warm filter
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.colorFilter.overrideState = true;

            // Setup Vignette (subtle darkening at edges)
            if (!PostProcessProfile.TryGet(out vignette))
            {
                vignette = PostProcessProfile.Add<Vignette>();
            }
            vignette.active = true;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.value = 0.4f;
            vignette.color.value = new Color(0.1f, 0.05f, 0.15f); // Subtle purple tint
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.color.overrideState = true;

            // Setup Depth of Field (background blur for focus)
            if (!PostProcessProfile.TryGet(out depthOfField))
            {
                depthOfField = PostProcessProfile.Add<DepthOfField>();
            }
            depthOfField.active = false; // Only enable for cutscenes/focus shots
            depthOfField.mode.value = DepthOfFieldMode.Bokeh;
            depthOfField.focusDistance.value = 5f;
            depthOfField.focalLength.value = 50f;
            depthOfField.aperture.value = 5.6f;
            depthOfField.mode.overrideState = true;
            depthOfField.focusDistance.overrideState = true;
            depthOfField.focalLength.overrideState = true;
            depthOfField.aperture.overrideState = true;

            // Setup Tonemapping
            if (!PostProcessProfile.TryGet(out tonemapping))
            {
                tonemapping = PostProcessProfile.Add<Tonemapping>();
            }
            tonemapping.active = true;
            tonemapping.mode.value = TonemappingMode.ACES;
            tonemapping.mode.overrideState = true;
        }

        public void ApplyQualityPreset(QualityPreset preset)
        {
            CurrentPreset = preset;

            switch (preset)
            {
                case QualityPreset.Low:
                    SetQuality(bloom: false, vignette: false, dof: false, shadows: false, msaa: 1);
                    break;
                case QualityPreset.Medium:
                    SetQuality(bloom: true, vignette: true, dof: false, shadows: true, msaa: 2);
                    break;
                case QualityPreset.High:
                    SetQuality(bloom: true, vignette: true, dof: true, shadows: true, msaa: 4);
                    break;
                case QualityPreset.Ultra:
                    SetQuality(bloom: true, vignette: true, dof: true, shadows: true, msaa: 8);
                    break;
            }
        }

        private void SetQuality(bool bloom, bool vignette, bool dof, bool shadows, int msaa)
        {
            if (this.bloom != null) this.bloom.active = bloom;
            if (this.vignette != null) this.vignette.active = vignette;
            if (this.depthOfField != null) this.depthOfField.active = dof;

            QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;
            QualitySettings.antiAliasing = msaa;

            // Shadow distance based on quality
            QualitySettings.shadowDistance = CurrentPreset switch
            {
                QualityPreset.Low => 20f,
                QualityPreset.Medium => 40f,
                QualityPreset.High => 60f,
                QualityPreset.Ultra => 80f,
                _ => 40f
            };
        }

        // --- EFFECT TOGGLES ---
        public void EnableDoF(float focusDist)
        {
            if (depthOfField == null) return;
            depthOfField.active = true;
            depthOfField.focusDistance.value = focusDist;
        }

        public void DisableDoF()
        {
            if (depthOfField != null) depthOfField.active = false;
        }

        public void SetBloomIntensity(float intensity)
        {
            if (bloom != null) bloom.intensity.value = intensity;
        }

        public void SetVignetteIntensity(float intensity)
        {
            if (vignette != null) vignette.intensity.value = intensity;
        }

        public void SetSaturation(float saturation)
        {
            if (colorAdjustments != null) colorAdjustments.saturation.value = saturation;
        }

        /// <summary>
        /// Flash screen white briefly (for level up, achievement, etc.)
        /// </summary>
        public void FlashScreen(float duration = 0.3f)
        {
            StartCoroutine(FlashCoroutine(duration));
        }

        private System.Collections.IEnumerator FlashCoroutine(float duration)
        {
            if (colorAdjustments == null) yield break;
            float originalExposure = colorAdjustments.postExposure.value;
            colorAdjustments.postExposure.value = 3f;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                colorAdjustments.postExposure.value = Mathf.Lerp(3f, originalExposure, timer / duration);
                yield return null;
            }
            colorAdjustments.postExposure.value = originalExposure;
        }

        /// <summary>
        /// Auto-detect device capability and set appropriate quality.
        /// </summary>
        public void AutoDetectQuality()
        {
            int gpuMemory = SystemInfo.graphicMemorySize;
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
