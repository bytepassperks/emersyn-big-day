using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Music Maker: tap colored pads to create melodies.
    /// Playback mode to record and listen. Bonus for matching a pattern.
    /// Satisfies Fun and Creativity needs.
    /// </summary>
    public class MusicMakerGame : MonoBehaviour
    {
        [Header("Settings")]
        public int PadCount = 8;
        public float RecordDuration = 15f;
        public int PatternLength = 4;

        [Header("Audio")]
        public AudioClip[] NoteClips;
        public AudioSource NoteSource;

        [Header("Challenge")]
        public bool HasPatternChallenge = true;

        [Header("UI")]
        public GameObject PadPrefab;
        public Transform PadContainer;
        public UnityEngine.UI.Text StatusText;
        public UnityEngine.UI.Button PlayButton;
        public UnityEngine.UI.Button RecordButton;

        private List<NoteEvent> recordedNotes = new List<NoteEvent>();
        private int[] challengePattern;
        private int patternProgress = 0;
        private float gameTimer;
        private int score = 0;
        private bool isRecording = false;
        private bool isActive = false;
        private float recordStartTime;

        public void StartGame()
        {
            gameTimer = RecordDuration;
            score = 0;
            recordedNotes.Clear();
            patternProgress = 0;
            isActive = true;

            // Generate challenge pattern
            if (HasPatternChallenge)
            {
                challengePattern = new int[PatternLength];
                for (int i = 0; i < PatternLength; i++)
                    challengePattern[i] = UnityEngine.Random.Range(0, PadCount);
                ShowPattern();
            }

            SetupPads();
            if (RecordButton != null) RecordButton.onClick.AddListener(ToggleRecord);
            if (PlayButton != null) PlayButton.onClick.AddListener(PlaybackRecording);
        }

        private void Update()
        {
            if (!isActive) return;

            if (isRecording)
            {
                gameTimer -= Time.deltaTime;
                if (StatusText != null) StatusText.text = $"Recording: {Mathf.CeilToInt(gameTimer)}s";
                if (gameTimer <= 0f) StopRecording();
            }
        }

        private void SetupPads()
        {
            if (PadPrefab == null || PadContainer == null) return;

            Color[] padColors = {
                Color.red, Color.blue, Color.green, Color.yellow,
                new Color(1f, 0.5f, 0f), Color.magenta, Color.cyan, new Color(0.5f, 0f, 1f)
            };

            for (int i = 0; i < PadCount; i++)
            {
                var pad = Instantiate(PadPrefab, PadContainer);
                var image = pad.GetComponent<UnityEngine.UI.Image>();
                if (image != null && i < padColors.Length) image.color = padColors[i];

                int noteIndex = i;
                pad.GetComponent<UnityEngine.UI.Button>()?.onClick.AddListener(() => OnPadTapped(noteIndex));
            }
        }

        public void OnPadTapped(int padIndex)
        {
            if (!isActive) return;

            // Play note
            if (NoteClips != null && padIndex < NoteClips.Length && NoteSource != null)
            {
                NoteSource.pitch = 0.5f + (padIndex / (float)PadCount);
                NoteSource.PlayOneShot(NoteClips[padIndex % NoteClips.Length]);
            }

            // Record
            if (isRecording)
            {
                recordedNotes.Add(new NoteEvent
                {
                    PadIndex = padIndex,
                    Time = Time.time - recordStartTime
                });
            }

            // Check pattern challenge
            if (HasPatternChallenge && challengePattern != null && patternProgress < challengePattern.Length)
            {
                if (padIndex == challengePattern[patternProgress])
                {
                    patternProgress++;
                    score += 25;

                    if (Particles.ParticleManager.Instance != null)
                        Particles.ParticleManager.Instance.SpawnSparkles(Vector3.up * 2f);

                    if (patternProgress >= challengePattern.Length)
                    {
                        // Pattern complete!
                        score += 100;
                        if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("win");
                        if (Particles.ParticleManager.Instance != null)
                            Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                    }
                }
                else
                {
                    patternProgress = 0; // Reset pattern
                }
            }

            score += 5; // Points for any note
        }

        public void ToggleRecord()
        {
            if (isRecording) StopRecording();
            else StartRecording();
        }

        private void StartRecording()
        {
            isRecording = true;
            recordStartTime = Time.time;
            recordedNotes.Clear();
            gameTimer = RecordDuration;
            if (StatusText != null) StatusText.text = "Recording...";
        }

        private void StopRecording()
        {
            isRecording = false;
            if (StatusText != null) StatusText.text = $"Recorded {recordedNotes.Count} notes!";
        }

        public void PlaybackRecording()
        {
            if (recordedNotes.Count == 0) return;
            StartCoroutine(PlaybackCoroutine());
        }

        private System.Collections.IEnumerator PlaybackCoroutine()
        {
            if (StatusText != null) StatusText.text = "Playing...";
            float startTime = Time.time;

            int noteIndex = 0;
            while (noteIndex < recordedNotes.Count)
            {
                float elapsed = Time.time - startTime;
                if (elapsed >= recordedNotes[noteIndex].Time)
                {
                    int pad = recordedNotes[noteIndex].PadIndex;
                    if (NoteClips != null && pad < NoteClips.Length && NoteSource != null)
                    {
                        NoteSource.pitch = 0.5f + (pad / (float)PadCount);
                        NoteSource.PlayOneShot(NoteClips[pad % NoteClips.Length]);
                    }
                    noteIndex++;
                }
                yield return null;
            }

            if (StatusText != null) StatusText.text = "Done!";
        }

        private void ShowPattern()
        {
            if (challengePattern == null || StatusText == null) return;
            string patternStr = "Pattern: ";
            foreach (int p in challengePattern) patternStr += (p + 1) + " ";
            StatusText.text = patternStr;
        }

        public void FinishGame()
        {
            isActive = false;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(score >= 50);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Fun", 20f);
                needSystem.SatisfyNeed("Creativity", 20f);
            }
        }

        public class NoteEvent
        {
            public int PadIndex;
            public float Time;
        }
    }
}
