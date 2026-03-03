using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Audio
{
    /// <summary>
    /// Enhancement #12: Simlish-style character voice system.
    /// Generates cute gibberish vocalizations from procedural audio.
    /// Like Sims' Simlish and Talking Tom's voice reactions.
    /// Emersyn is 6 years old — voice should be high-pitched, cute, excited.
    /// </summary>
    public class CharacterVoiceSystem : MonoBehaviour
    {
        public static CharacterVoiceSystem Instance { get; private set; }

        [Header("Voice Settings")]
        public float BasePitch = 1.4f; // Higher for a 6-year-old
        public float PitchVariation = 0.2f;
        public float VoiceVolume = 0.6f;
        public float MinTimeBetweenVoices = 1f;

        [Header("Character Pitch Offsets")]
        public float EmersynPitch = 1.4f; // 6-year-old girl
        public float AvaPitch = 1.35f;
        public float MiaPitch = 1.3f;
        public float LeoPitch = 1.25f; // Slightly lower for boy

        private AudioSource voiceSource;
        private float lastVoiceTime;
        private Dictionary<string, float> characterPitches = new Dictionary<string, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SetupVoiceSource();
            InitializeCharacterPitches();
        }

        private void SetupVoiceSource()
        {
            var go = new GameObject("VoiceSource");
            go.transform.SetParent(transform);
            voiceSource = go.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0.3f; // Slightly spatial
            voiceSource.volume = VoiceVolume;
        }

        private void InitializeCharacterPitches()
        {
            characterPitches["Emersyn"] = EmersynPitch;
            characterPitches["Ava"] = AvaPitch;
            characterPitches["Mia"] = MiaPitch;
            characterPitches["Leo"] = LeoPitch;
            characterPitches["Kitty"] = 1.8f; // Cat sounds
            characterPitches["Puppy"] = 1.5f;
            characterPitches["Bunny"] = 1.9f;
        }

        /// <summary>
        /// Play a procedurally generated voice clip for an emotion.
        /// </summary>
        public void Speak(string characterName, VoiceEmotion emotion)
        {
            if (Time.time - lastVoiceTime < MinTimeBetweenVoices) return;
            lastVoiceTime = Time.time;

            float pitch = characterPitches.ContainsKey(characterName)
                ? characterPitches[characterName]
                : BasePitch;

            // Adjust pitch based on emotion
            switch (emotion)
            {
                case VoiceEmotion.Happy:
                    pitch += 0.15f;
                    break;
                case VoiceEmotion.Sad:
                    pitch -= 0.2f;
                    break;
                case VoiceEmotion.Excited:
                    pitch += 0.25f;
                    break;
                case VoiceEmotion.Angry:
                    pitch -= 0.1f;
                    break;
                case VoiceEmotion.Sleepy:
                    pitch -= 0.15f;
                    break;
                case VoiceEmotion.Surprised:
                    pitch += 0.3f;
                    break;
                case VoiceEmotion.Giggle:
                    pitch += 0.2f;
                    break;
            }

            pitch += Random.Range(-PitchVariation, PitchVariation);

            // Generate procedural voice clip
            AudioClip clip = GenerateVoiceClip(emotion, pitch);
            if (clip != null && voiceSource != null)
            {
                voiceSource.pitch = pitch;
                voiceSource.PlayOneShot(clip, VoiceVolume);
                // Destroy clip after playback to prevent memory leak
                StartCoroutine(DestroyClipDelayed(clip, clip.length + 0.5f));
            }
        }

        /// <summary>
        /// Generate a simple procedural voice clip (sine wave-based gibberish).
        /// </summary>
        private AudioClip GenerateVoiceClip(VoiceEmotion emotion, float pitch)
        {
            int sampleRate = 44100;
            float duration;
            float[] frequencies;

            switch (emotion)
            {
                case VoiceEmotion.Happy:
                    duration = 0.3f;
                    frequencies = new float[] { 400, 500, 600 };
                    break;
                case VoiceEmotion.Sad:
                    duration = 0.5f;
                    frequencies = new float[] { 300, 250, 200 };
                    break;
                case VoiceEmotion.Excited:
                    duration = 0.25f;
                    frequencies = new float[] { 500, 600, 700, 600 };
                    break;
                case VoiceEmotion.Angry:
                    duration = 0.35f;
                    frequencies = new float[] { 350, 400, 350, 300 };
                    break;
                case VoiceEmotion.Sleepy:
                    duration = 0.6f;
                    frequencies = new float[] { 250, 200, 180 };
                    break;
                case VoiceEmotion.Surprised:
                    duration = 0.2f;
                    frequencies = new float[] { 400, 700 };
                    break;
                case VoiceEmotion.Giggle:
                    duration = 0.4f;
                    frequencies = new float[] { 500, 400, 500, 400, 500, 600 };
                    break;
                default:
                    duration = 0.3f;
                    frequencies = new float[] { 350, 400, 350 };
                    break;
            }

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            int samplesPerNote = sampleCount / frequencies.Length;

            for (int noteIdx = 0; noteIdx < frequencies.Length; noteIdx++)
            {
                float freq = frequencies[noteIdx] * pitch;
                int startSample = noteIdx * samplesPerNote;
                int endSample = Mathf.Min(startSample + samplesPerNote, sampleCount);

                for (int i = startSample; i < endSample; i++)
                {
                    float t = (float)(i - startSample) / samplesPerNote;
                    float envelope = Mathf.Sin(t * Mathf.PI); // Smooth envelope
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate);
                    // Add some harmonics for richness
                    wave += 0.3f * Mathf.Sin(4f * Mathf.PI * freq * i / sampleRate);
                    wave += 0.1f * Mathf.Sin(6f * Mathf.PI * freq * i / sampleRate);
                    samples[i] = wave * envelope * 0.3f;
                }
            }

            AudioClip clip = AudioClip.Create("voice", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private System.Collections.IEnumerator DestroyClipDelayed(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (clip != null) Destroy(clip);
        }

        /// <summary>
        /// Play a greeting when characters meet.
        /// </summary>
        public void PlayGreeting(string characterName)
        {
            Speak(characterName, VoiceEmotion.Happy);
        }

        /// <summary>
        /// Play reaction to being touched/poked.
        /// </summary>
        public void PlayTouchReaction(string characterName, bool isGentle)
        {
            Speak(characterName, isGentle ? VoiceEmotion.Happy : VoiceEmotion.Surprised);
        }

        /// <summary>
        /// Play voice for need satisfaction.
        /// </summary>
        public void PlayNeedReaction(string characterName, string needName, bool satisfied)
        {
            if (satisfied)
                Speak(characterName, needName == "Hunger" ? VoiceEmotion.Happy : VoiceEmotion.Excited);
            else
                Speak(characterName, VoiceEmotion.Sad);
        }
    }

    public enum VoiceEmotion
    {
        Neutral, Happy, Sad, Excited, Angry, Sleepy, Surprised, Giggle
    }
}
