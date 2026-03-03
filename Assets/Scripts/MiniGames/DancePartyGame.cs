using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Dance Party rhythm game: tap arrows/circles in sync with music beats.
    /// Increasing difficulty with faster beats and more complex patterns.
    /// Satisfies Fun need on completion.
    /// </summary>
    public class DancePartyGame : MonoBehaviour
    {
        [Header("Game Settings")]
        public float BeatInterval = 0.8f;
        public float HitWindow = 0.3f;
        public float MissWindow = 0.6f;
        public int TotalBeats = 20;

        [Header("Visual")]
        public Transform NoteSpawnPoint;
        public Transform HitZone;
        public GameObject NotePrefab;
        public float NoteSpeed = 5f;

        [Header("Scoring")]
        public int PerfectScore = 100;
        public int GoodScore = 50;
        public int MissScore = -10;

        private int currentBeat = 0;
        private int score = 0;
        private int combo = 0;
        private int maxCombo = 0;
        private int perfects = 0;
        private int goods = 0;
        private int misses = 0;
        private float beatTimer = 0f;
        private bool isActive = false;
        private List<DanceNote> activeNotes = new List<DanceNote>();

        public void StartGame()
        {
            currentBeat = 0;
            score = 0;
            combo = 0;
            maxCombo = 0;
            perfects = 0;
            goods = 0;
            misses = 0;
            beatTimer = 0f;
            isActive = true;

            // Start music
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("button");
        }

        private void Update()
        {
            if (!isActive) return;

            beatTimer += Time.deltaTime;

            // Spawn new beats
            if (beatTimer >= BeatInterval && currentBeat < TotalBeats)
            {
                beatTimer = 0f;
                SpawnNote();
                currentBeat++;
            }

            // Move active notes
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];
                if (note.NoteObject == null) { activeNotes.RemoveAt(i); continue; }

                note.NoteObject.transform.position += Vector3.down * NoteSpeed * Time.deltaTime;
                note.Age += Time.deltaTime;

                // Check if missed
                if (note.Age > note.LifeTime)
                {
                    OnMiss(note);
                    activeNotes.RemoveAt(i);
                }
            }

            // Check for game end
            if (currentBeat >= TotalBeats && activeNotes.Count == 0)
            {
                EndGame();
            }
        }

        private void SpawnNote()
        {
            if (NotePrefab == null || NoteSpawnPoint == null) return;

            // Random lane (left, center, right)
            float lane = UnityEngine.Random.Range(-1, 2) * 1.5f;
            Vector3 spawnPos = NoteSpawnPoint.position + Vector3.right * lane;

            GameObject noteObj = Instantiate(NotePrefab, spawnPos, Quaternion.identity);
            var note = new DanceNote
            {
                NoteObject = noteObj,
                Lane = Mathf.RoundToInt(lane / 1.5f),
                Age = 0f,
                LifeTime = 3f,
                IsHit = false
            };
            activeNotes.Add(note);
        }

        /// <summary>
        /// Called when player taps (from InputManager)
        /// </summary>
        public void OnPlayerTap(Vector3 tapPosition)
        {
            if (!isActive || activeNotes.Count == 0) return;

            // Find closest note to hit zone
            DanceNote closestNote = null;
            float closestDist = float.MaxValue;

            foreach (var note in activeNotes)
            {
                if (note.IsHit || note.NoteObject == null) continue;
                float dist = Vector3.Distance(note.NoteObject.transform.position, HitZone != null ? HitZone.position : Vector3.zero);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestNote = note;
                }
            }

            if (closestNote == null) return;

            if (closestDist < HitWindow)
            {
                OnPerfect(closestNote);
            }
            else if (closestDist < MissWindow)
            {
                OnGood(closestNote);
            }
        }

        private void OnPerfect(DanceNote note)
        {
            perfects++;
            combo++;
            if (combo > maxCombo) maxCombo = combo;
            int points = PerfectScore + (combo * 10);
            score += points;
            note.IsHit = true;

            if (note.NoteObject != null) Destroy(note.NoteObject);
            activeNotes.Remove(note);

            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnStarBurst(HitZone != null ? HitZone.position : Vector3.zero);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("coin");
        }

        private void OnGood(DanceNote note)
        {
            goods++;
            combo++;
            if (combo > maxCombo) maxCombo = combo;
            score += GoodScore + (combo * 5);
            note.IsHit = true;

            if (note.NoteObject != null) Destroy(note.NoteObject);
            activeNotes.Remove(note);

            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("tap");
        }

        private void OnMiss(DanceNote note)
        {
            misses++;
            combo = 0;
            score = Mathf.Max(0, score + MissScore);

            if (note.NoteObject != null) Destroy(note.NoteObject);

            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.ShakeSmall();
        }

        private void EndGame()
        {
            isActive = false;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                bool won = perfects + goods > misses;
                MiniGameManager.Instance.CompleteGame(won);
            }

            // Satisfy Fun need
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Fun", 30f);
        }
    }

    public class DanceNote
    {
        public GameObject NoteObject;
        public int Lane;
        public float Age;
        public float LifeTime;
        public bool IsHit;
    }
}
