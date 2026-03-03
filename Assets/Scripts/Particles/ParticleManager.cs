using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Particles
{
    /// <summary>
    /// Manages all particle effects: sparkles, hearts, confetti, bubbles, stars,
    /// food steam, sleep Zs, musical notes, fireworks, rain, snow, leaves, dust.
    /// Pool-based system for performance on mobile (60fps target).
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        [Header("Particle Prefabs")]
        public ParticleSystem SparklePrefab;
        public ParticleSystem HeartsPrefab;
        public ParticleSystem ConfettiPrefab;
        public ParticleSystem BubblesPrefab;
        public ParticleSystem StarBurstPrefab;
        public ParticleSystem FoodSteamPrefab;
        public ParticleSystem SleepZPrefab;
        public ParticleSystem MusicNotesPrefab;
        public ParticleSystem FireworksPrefab;
        public ParticleSystem RainPrefab;
        public ParticleSystem SnowPrefab;
        public ParticleSystem LeavesPrefab;
        public ParticleSystem DustPrefab;
        public ParticleSystem LevelUpPrefab;
        public ParticleSystem CoinCollectPrefab;
        public ParticleSystem AngerPrefab;
        public ParticleSystem SadTearsPrefab;
        public ParticleSystem ExcitementPrefab;

        [Header("Pool Settings")]
        public int PoolSizePerType = 5;

        private Dictionary<ParticleType, Queue<ParticleSystem>> pools = new Dictionary<ParticleType, Queue<ParticleSystem>>();
        private Transform poolContainer;

        public enum ParticleType
        {
            Sparkle, Hearts, Confetti, Bubbles, StarBurst,
            FoodSteam, SleepZ, MusicNotes, Fireworks,
            Rain, Snow, Leaves, Dust, LevelUp,
            CoinCollect, Anger, SadTears, Excitement
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            poolContainer = new GameObject("ParticlePool").transform;
            poolContainer.SetParent(transform);

            InitializePools();
        }

        private void InitializePools()
        {
            InitPool(ParticleType.Sparkle, SparklePrefab);
            InitPool(ParticleType.Hearts, HeartsPrefab);
            InitPool(ParticleType.Confetti, ConfettiPrefab);
            InitPool(ParticleType.Bubbles, BubblesPrefab);
            InitPool(ParticleType.StarBurst, StarBurstPrefab);
            InitPool(ParticleType.FoodSteam, FoodSteamPrefab);
            InitPool(ParticleType.SleepZ, SleepZPrefab);
            InitPool(ParticleType.MusicNotes, MusicNotesPrefab);
            InitPool(ParticleType.Fireworks, FireworksPrefab);
            InitPool(ParticleType.Rain, RainPrefab);
            InitPool(ParticleType.Snow, SnowPrefab);
            InitPool(ParticleType.Leaves, LeavesPrefab);
            InitPool(ParticleType.Dust, DustPrefab);
            InitPool(ParticleType.LevelUp, LevelUpPrefab);
            InitPool(ParticleType.CoinCollect, CoinCollectPrefab);
            InitPool(ParticleType.Anger, AngerPrefab);
            InitPool(ParticleType.SadTears, SadTearsPrefab);
            InitPool(ParticleType.Excitement, ExcitementPrefab);
        }

        private void InitPool(ParticleType type, ParticleSystem prefab)
        {
            if (prefab == null) return;
            var queue = new Queue<ParticleSystem>();
            for (int i = 0; i < PoolSizePerType; i++)
            {
                ParticleSystem ps = Instantiate(prefab, poolContainer);
                ps.gameObject.SetActive(false);
                queue.Enqueue(ps);
            }
            pools[type] = queue;
        }

        /// <summary>
        /// Play a particle effect at the given world position.
        /// Returns the ParticleSystem instance (auto-returns to pool when done).
        /// </summary>
        public ParticleSystem Play(ParticleType type, Vector3 position, float duration = 0f)
        {
            if (!pools.ContainsKey(type) || pools[type].Count == 0) return null;

            ParticleSystem ps = pools[type].Dequeue();
            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Play();

            float returnTime = duration > 0 ? duration : ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(type, ps, returnTime));

            return ps;
        }

        /// <summary>
        /// Play a particle effect attached to a transform (follows it).
        /// </summary>
        public ParticleSystem PlayAttached(ParticleType type, Transform parent, Vector3 localOffset = default, float duration = 0f)
        {
            if (!pools.ContainsKey(type) || pools[type].Count == 0) return null;

            ParticleSystem ps = pools[type].Dequeue();
            ps.transform.SetParent(parent);
            ps.transform.localPosition = localOffset;
            ps.gameObject.SetActive(true);
            ps.Play();

            float returnTime = duration > 0 ? duration : ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnToPoolDetach(type, ps, returnTime));

            return ps;
        }

        private System.Collections.IEnumerator ReturnToPool(ParticleType type, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
                if (pools.ContainsKey(type)) pools[type].Enqueue(ps);
            }
        }

        private System.Collections.IEnumerator ReturnToPoolDetach(ParticleType type, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop();
                ps.transform.SetParent(poolContainer);
                ps.gameObject.SetActive(false);
                if (pools.ContainsKey(type)) pools[type].Enqueue(ps);
            }
        }

        // --- CONVENIENCE METHODS ---
        public void SpawnSparkles(Vector3 pos) => Play(ParticleType.Sparkle, pos);
        public void SpawnHearts(Vector3 pos) => Play(ParticleType.Hearts, pos);
        public void SpawnConfetti(Vector3 pos) => Play(ParticleType.Confetti, pos, 3f);
        public void SpawnBubbles(Vector3 pos) => Play(ParticleType.Bubbles, pos, 2f);
        public void SpawnStarBurst(Vector3 pos) => Play(ParticleType.StarBurst, pos);
        public void SpawnFoodSteam(Vector3 pos) => Play(ParticleType.FoodSteam, pos, 3f);
        public void SpawnFireworks(Vector3 pos) => Play(ParticleType.Fireworks, pos, 4f);
        public void SpawnLevelUp(Vector3 pos) => Play(ParticleType.LevelUp, pos, 2f);
        public void SpawnCoinCollect(Vector3 pos) => Play(ParticleType.CoinCollect, pos);
        public void SpawnAnger(Vector3 pos) => Play(ParticleType.Anger, pos, 2f);
        public void SpawnSadTears(Vector3 pos) => Play(ParticleType.SadTears, pos, 3f);
        public void SpawnExcitement(Vector3 pos) => Play(ParticleType.Excitement, pos, 2f);

        public ParticleSystem AttachSleepZ(Transform parent) => PlayAttached(ParticleType.SleepZ, parent, Vector3.up * 1.5f, 0f);
        public ParticleSystem AttachMusicNotes(Transform parent) => PlayAttached(ParticleType.MusicNotes, parent, Vector3.up * 1.2f, 0f);

        // --- WEATHER ---
        public void StartRain() => Play(ParticleType.Rain, Vector3.up * 15f, 999f);
        public void StartSnow() => Play(ParticleType.Snow, Vector3.up * 15f, 999f);
        public void StartLeaves() => Play(ParticleType.Leaves, Vector3.up * 10f, 999f);

        public void StopAllWeather()
        {
            StopType(ParticleType.Rain);
            StopType(ParticleType.Snow);
            StopType(ParticleType.Leaves);
        }

        private void StopType(ParticleType type)
        {
            var allPS = poolContainer.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allPS)
            {
                if (ps.isPlaying) ps.Stop();
            }
        }
    }
}
