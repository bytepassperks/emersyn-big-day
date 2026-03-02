using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Audio
{
    /// <summary>
    /// Enhancement #25: Spatial audio system - sounds come from their 3D positions.
    /// Footsteps on different surfaces, ambient room sounds, distance-based volume.
    /// Like Animal Crossing's spatial sound design and Sims' environmental audio.
    /// </summary>
    public class SpatialAudioSystem : MonoBehaviour
    {
        public static SpatialAudioSystem Instance { get; private set; }

        [Header("Settings")]
        public float MaxAudioDistance = 20f;
        public float SpatialBlend = 0.8f;
        public int MaxConcurrentSounds = 8;

        private List<AudioSource> activeSources = new List<AudioSource>();
        private Queue<AudioSource> sourcePool = new Queue<AudioSource>();
        private Dictionary<string, float> surfaceFootstepPitch = new Dictionary<string, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializePool();
            InitializeSurfaceData();
        }

        private void InitializePool()
        {
            for (int i = 0; i < MaxConcurrentSounds; i++)
            {
                var go = new GameObject($"SpatialSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = SpatialBlend;
                src.maxDistance = MaxAudioDistance;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.dopplerLevel = 0f;
                go.SetActive(false);
                sourcePool.Enqueue(src);
            }
        }

        private void InitializeSurfaceData()
        {
            surfaceFootstepPitch["carpet"] = 0.9f;
            surfaceFootstepPitch["tile"] = 1.1f;
            surfaceFootstepPitch["grass"] = 0.85f;
            surfaceFootstepPitch["wood"] = 1.0f;
            surfaceFootstepPitch["stone"] = 1.15f;
            surfaceFootstepPitch["sand"] = 0.8f;
        }

        private void Update()
        {
            // Return finished sources to pool
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                if (activeSources[i] != null && !activeSources[i].isPlaying)
                {
                    ReturnToPool(activeSources[i]);
                    activeSources.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Play a sound at a 3D position.
        /// </summary>
        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            if (sourcePool.Count == 0)
            {
                Debug.LogWarning("[SpatialAudio] All sources busy, skipping sound");
                return;
            }

            var src = sourcePool.Dequeue();
            src.gameObject.SetActive(true);
            src.transform.position = position;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch + Random.Range(-0.05f, 0.05f);
            src.spatialBlend = SpatialBlend;
            src.Play();
            activeSources.Add(src);
        }

        /// <summary>
        /// Play footstep sound based on surface type.
        /// </summary>
        public void PlayFootstep(Vector3 position, string surfaceType)
        {
            float pitch = surfaceFootstepPitch.ContainsKey(surfaceType)
                ? surfaceFootstepPitch[surfaceType] : 1f;

            // Generate procedural footstep
            AudioClip clip = GenerateFootstepClip(surfaceType);
            PlayAt(clip, position, 0.3f, pitch);
            // Destroy generated clip after playback
            if (clip != null) StartCoroutine(DestroyClipDelayed(clip, 0.5f));
        }

        private System.Collections.IEnumerator DestroyClipDelayed(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (clip != null) Destroy(clip);
        }

        /// <summary>
        /// Play ambient loop at a position (e.g., fountain, fire, wind chime).
        /// </summary>
        public AudioSource PlayAmbientLoop(AudioClip clip, Vector3 position, float volume = 0.3f)
        {
            if (clip == null || sourcePool.Count == 0) return null;

            var src = sourcePool.Dequeue();
            src.gameObject.SetActive(true);
            src.transform.position = position;
            src.clip = clip;
            src.volume = volume;
            src.loop = true;
            src.spatialBlend = SpatialBlend;
            src.Play();
            activeSources.Add(src);
            return src;
        }

        /// <summary>
        /// Generate a simple procedural footstep clip.
        /// </summary>
        private AudioClip GenerateFootstepClip(string surfaceType)
        {
            int sampleRate = 22050;
            float duration = 0.08f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            float noiseAmount = surfaceType == "grass" || surfaceType == "sand" ? 0.8f : 0.4f;
            float toneFreq = surfaceType == "tile" ? 800f : surfaceType == "wood" ? 400f : 200f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 15f);
                float noise = (Random.value * 2f - 1f) * noiseAmount;
                float tone = Mathf.Sin(2f * Mathf.PI * toneFreq * i / sampleRate) * (1f - noiseAmount);
                data[i] = (noise + tone) * envelope * 0.3f;
            }

            var clip = AudioClip.Create("footstep", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void ReturnToPool(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            src.clip = null;
            src.loop = false;
            src.gameObject.SetActive(false);
            sourcePool.Enqueue(src);
        }

        public void StopAll()
        {
            foreach (var src in activeSources)
            {
                if (src != null) ReturnToPool(src);
            }
            activeSources.Clear();
        }
    }
}
