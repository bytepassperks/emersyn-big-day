using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Audio
{
    /// <summary>
    /// Enhancement #13: Adaptive music system with layered tracks per room/mood.
    /// Crossfades between room themes, adds layers based on activity and mood.
    /// Like Animal Crossing's hourly music and Sims FreePlay's mood-reactive soundtrack.
    /// </summary>
    public class AdaptiveMusicSystem : MonoBehaviour
    {
        public static AdaptiveMusicSystem Instance { get; private set; }

        [Header("Settings")]
        public float CrossfadeDuration = 2f;
        public float BaseVolume = 0.4f;
        public float LayerFadeSpeed = 1f;

        [Header("Music Layers")]
        public AudioSource BaseLayer;
        public AudioSource MelodyLayer;
        public AudioSource PercussionLayer;
        public AudioSource AmbienceLayer;

        private string currentRoomTheme = "bedroom";
        private string currentMood = "happy";
        private float melodyTarget = 0f;
        private float percussionTarget = 0f;
        private float ambienceTarget = 0.3f;
        private bool isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SetupAudioSources();
        }

        private void Update()
        {
            UpdateLayerVolumes();
        }

        private void SetupAudioSources()
        {
            if (BaseLayer == null)
            {
                BaseLayer = CreateAudioSource("BaseLayer");
                BaseLayer.volume = BaseVolume;
                BaseLayer.loop = true;
            }
            if (MelodyLayer == null)
            {
                MelodyLayer = CreateAudioSource("MelodyLayer");
                MelodyLayer.volume = 0f;
                MelodyLayer.loop = true;
            }
            if (PercussionLayer == null)
            {
                PercussionLayer = CreateAudioSource("PercussionLayer");
                PercussionLayer.volume = 0f;
                PercussionLayer.loop = true;
            }
            if (AmbienceLayer == null)
            {
                AmbienceLayer = CreateAudioSource("AmbienceLayer");
                AmbienceLayer.volume = 0.3f;
                AmbienceLayer.loop = true;
            }
        }

        private AudioSource CreateAudioSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D music
            return src;
        }

        private void UpdateLayerVolumes()
        {
            float speed = LayerFadeSpeed * Time.deltaTime;
            if (MelodyLayer != null)
                MelodyLayer.volume = Mathf.MoveTowards(MelodyLayer.volume, melodyTarget, speed);
            if (PercussionLayer != null)
                PercussionLayer.volume = Mathf.MoveTowards(PercussionLayer.volume, percussionTarget, speed);
            if (AmbienceLayer != null)
                AmbienceLayer.volume = Mathf.MoveTowards(AmbienceLayer.volume, ambienceTarget, speed);
        }

        /// <summary>
        /// Set music layers based on room type.
        /// </summary>
        public void SetRoomTheme(string roomName)
        {
            currentRoomTheme = roomName.ToLower();

            switch (currentRoomTheme)
            {
                case "bedroom":
                    melodyTarget = 0.2f;
                    percussionTarget = 0f;
                    ambienceTarget = 0.4f; // Cozy ambience
                    break;
                case "kitchen":
                    melodyTarget = 0.3f;
                    percussionTarget = 0.15f;
                    ambienceTarget = 0.2f; // Light cooking sounds
                    break;
                case "bathroom":
                    melodyTarget = 0.1f;
                    percussionTarget = 0f;
                    ambienceTarget = 0.5f; // Water ambience
                    break;
                case "park":
                    melodyTarget = 0.35f;
                    percussionTarget = 0.1f;
                    ambienceTarget = 0.5f; // Birds, wind
                    break;
                case "school":
                    melodyTarget = 0.25f;
                    percussionTarget = 0.2f;
                    ambienceTarget = 0.15f;
                    break;
                case "arcade":
                    melodyTarget = 0.4f;
                    percussionTarget = 0.35f;
                    ambienceTarget = 0.1f; // Upbeat
                    break;
                case "studio":
                    melodyTarget = 0.3f;
                    percussionTarget = 0.25f;
                    ambienceTarget = 0.2f;
                    break;
                case "shop":
                    melodyTarget = 0.25f;
                    percussionTarget = 0.15f;
                    ambienceTarget = 0.2f;
                    break;
                case "garden":
                    melodyTarget = 0.3f;
                    percussionTarget = 0.05f;
                    ambienceTarget = 0.5f; // Nature sounds
                    break;
                default:
                    melodyTarget = 0.2f;
                    percussionTarget = 0.1f;
                    ambienceTarget = 0.3f;
                    break;
            }
        }

        /// <summary>
        /// Adjust music based on character mood.
        /// </summary>
        public void SetMoodLayer(string mood)
        {
            currentMood = mood.ToLower();

            switch (currentMood)
            {
                case "ecstatic":
                case "happy":
                    melodyTarget = Mathf.Min(melodyTarget + 0.15f, 0.5f);
                    percussionTarget = Mathf.Min(percussionTarget + 0.1f, 0.4f);
                    break;
                case "sad":
                case "miserable":
                    melodyTarget = Mathf.Max(melodyTarget - 0.1f, 0.05f);
                    percussionTarget = 0f;
                    break;
                case "uncomfortable":
                    percussionTarget = Mathf.Max(percussionTarget - 0.05f, 0f);
                    break;
            }
        }

        /// <summary>
        /// Boost music energy during mini-games.
        /// </summary>
        public void SetActivityBoost(bool active)
        {
            if (active)
            {
                melodyTarget = 0.45f;
                percussionTarget = 0.35f;
                ambienceTarget = 0.05f;
            }
            else
            {
                SetRoomTheme(currentRoomTheme); // Reset to room defaults
            }
        }

        /// <summary>
        /// Night time quiet music.
        /// </summary>
        public void SetNightMode(bool isNight)
        {
            if (isNight)
            {
                melodyTarget *= 0.3f;
                percussionTarget = 0f;
                ambienceTarget *= 0.5f;
                if (BaseLayer != null) BaseLayer.volume = BaseVolume * 0.4f;
            }
            else
            {
                if (BaseLayer != null) BaseLayer.volume = BaseVolume;
            }
        }
    }
}
