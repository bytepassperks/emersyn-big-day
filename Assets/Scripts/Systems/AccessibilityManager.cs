using UnityEngine;
using System;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #27: Accessibility features for inclusive play.
    /// Colorblind modes, large touch targets, haptic feedback, text scaling.
    /// WCAG-inspired for ages 4-8, COPPA compliant.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [Header("Visual")]
        public ColorblindMode CurrentColorblindMode = ColorblindMode.Normal;
        public float UIScale = 1f;
        public bool HighContrast;
        public bool ReducedMotion;

        [Header("Audio")]
        public bool ClosedCaptions;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.5f;
        public float SFXVolume = 0.8f;

        [Header("Input")]
        public float TouchTargetMinSize = 48f; // 48dp minimum
        public bool HapticFeedback = true;
        public float DoubleTapDelay = 0.5f;

        public event Action OnSettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadSettings();
        }

        public void SetColorblindMode(ColorblindMode mode)
        {
            CurrentColorblindMode = mode;
            ApplyColorblindFilter();
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetUIScale(float scale)
        {
            UIScale = Mathf.Clamp(scale, 0.8f, 1.5f);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetHighContrast(bool enabled)
        {
            HighContrast = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetReducedMotion(bool enabled)
        {
            ReducedMotion = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetHapticFeedback(bool enabled)
        {
            HapticFeedback = enabled;
            SaveSettings();
        }

        public void SetClosedCaptions(bool enabled)
        {
            ClosedCaptions = enabled;
            SaveSettings();
        }

        public void SetVolumes(float master, float music, float sfx)
        {
            MasterVolume = Mathf.Clamp01(master);
            MusicVolume = Mathf.Clamp01(music);
            SFXVolume = Mathf.Clamp01(sfx);
            AudioListener.volume = MasterVolume;
            SaveSettings();
        }

        /// <summary>
        /// Apply colorblind simulation filter to camera.
        /// </summary>
        private void ApplyColorblindFilter()
        {
            // In a full implementation, this would apply a post-process shader
            // For now, we adjust ambient light to help differentiate
            switch (CurrentColorblindMode)
            {
                case ColorblindMode.Deuteranopia: // Red-green
                    RenderSettings.ambientLight = new Color(0.85f, 0.85f, 0.9f);
                    break;
                case ColorblindMode.Protanopia: // Red-weak
                    RenderSettings.ambientLight = new Color(0.9f, 0.85f, 0.85f);
                    break;
                case ColorblindMode.Tritanopia: // Blue-yellow
                    RenderSettings.ambientLight = new Color(0.85f, 0.9f, 0.85f);
                    break;
                default:
                    // Normal - reset
                    break;
            }
        }

        /// <summary>
        /// Trigger haptic feedback on supported devices.
        /// </summary>
        public void TriggerHaptic(HapticType type = HapticType.Light)
        {
            if (!HapticFeedback) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    long duration = type switch
                    {
                        HapticType.Light => 10L,
                        HapticType.Medium => 25L,
                        HapticType.Heavy => 50L,
                        _ => 10L
                    };
                    vibrator.Call("vibrate", duration);
                }
            }
            catch (System.Exception) { /* Vibrator not available */ }
#endif
        }

        /// <summary>
        /// Check if reduced motion should skip an animation.
        /// </summary>
        public bool ShouldReduceMotion() => ReducedMotion;

        /// <summary>
        /// Get scaled touch target size in pixels.
        /// </summary>
        public float GetScaledTouchTarget()
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            return TouchTargetMinSize * (dpi / 160f) * UIScale;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("acc_colorblind", (int)CurrentColorblindMode);
            PlayerPrefs.SetFloat("acc_ui_scale", UIScale);
            PlayerPrefs.SetInt("acc_high_contrast", HighContrast ? 1 : 0);
            PlayerPrefs.SetInt("acc_reduced_motion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt("acc_haptic", HapticFeedback ? 1 : 0);
            PlayerPrefs.SetInt("acc_captions", ClosedCaptions ? 1 : 0);
            PlayerPrefs.SetFloat("acc_master_vol", MasterVolume);
            PlayerPrefs.SetFloat("acc_music_vol", MusicVolume);
            PlayerPrefs.SetFloat("acc_sfx_vol", SFXVolume);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            CurrentColorblindMode = (ColorblindMode)PlayerPrefs.GetInt("acc_colorblind", 0);
            UIScale = PlayerPrefs.GetFloat("acc_ui_scale", 1f);
            HighContrast = PlayerPrefs.GetInt("acc_high_contrast", 0) == 1;
            ReducedMotion = PlayerPrefs.GetInt("acc_reduced_motion", 0) == 1;
            HapticFeedback = PlayerPrefs.GetInt("acc_haptic", 1) == 1;
            ClosedCaptions = PlayerPrefs.GetInt("acc_captions", 0) == 1;
            MasterVolume = PlayerPrefs.GetFloat("acc_master_vol", 1f);
            MusicVolume = PlayerPrefs.GetFloat("acc_music_vol", 0.5f);
            SFXVolume = PlayerPrefs.GetFloat("acc_sfx_vol", 0.8f);

            AudioListener.volume = MasterVolume;
        }
    }

    public enum ColorblindMode { Normal, Deuteranopia, Protanopia, Tritanopia }
    public enum HapticType { Light, Medium, Heavy }
}
