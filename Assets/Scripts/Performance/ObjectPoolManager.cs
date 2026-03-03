using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Performance
{
    /// <summary>
    /// Enhancement #17: Enhanced object pooling for all spawnable objects.
    /// Reduces GC pressure and allocation overhead for mobile performance.
    /// Pre-allocates and reuses GameObjects for particles, UI elements, audio sources.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();
        private int totalPooled;
        private int totalReused;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Create a pool for a given prefab.
        /// </summary>
        public void CreatePool(string poolId, GameObject prefab, int initialSize)
        {
            if (pools.ContainsKey(poolId)) return;
            if (prefab == null) return;

            var parent = new GameObject($"Pool_{poolId}");
            parent.transform.SetParent(transform);
            poolParents[poolId] = parent.transform;

            prefabs[poolId] = prefab;
            var queue = new Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var instance = Instantiate(prefab, parent.transform);
                instance.SetActive(false);
                instance.name = $"{poolId}_{i}";
                queue.Enqueue(instance);
                totalPooled++;
            }

            pools[poolId] = queue;
        }

        /// <summary>
        /// Get an object from the pool. Creates new if pool is empty.
        /// </summary>
        public GameObject Get(string poolId)
        {
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{poolId}' not found");
                return null;
            }

            var pool = pools[poolId];
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                totalReused++;
            }
            else
            {
                // Expand pool
                obj = Instantiate(prefabs[poolId], poolParents[poolId]);
                obj.name = $"{poolId}_expanded";
                totalPooled++;
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Get an object and position it.
        /// </summary>
        public GameObject Get(string poolId, Vector3 position, Quaternion rotation)
        {
            var obj = Get(poolId);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(string poolId, GameObject obj)
        {
            if (obj == null) return;

            if (!pools.ContainsKey(poolId))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            if (poolParents.ContainsKey(poolId))
                obj.transform.SetParent(poolParents[poolId]);
            pools[poolId].Enqueue(obj);
        }

        /// <summary>
        /// Return after delay.
        /// </summary>
        public void ReturnDelayed(string poolId, GameObject obj, float delay)
        {
            StartCoroutine(ReturnDelayedCoroutine(poolId, obj, delay));
        }

        private System.Collections.IEnumerator ReturnDelayedCoroutine(string poolId, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(poolId, obj);
        }

        /// <summary>
        /// Clear a specific pool.
        /// </summary>
        public void ClearPool(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return;
            var pool = pools[poolId];
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
            pools.Remove(poolId);
        }

        /// <summary>
        /// Clear all pools.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in pools)
            {
                while (kvp.Value.Count > 0)
                {
                    var obj = kvp.Value.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            pools.Clear();
        }

        public int GetPoolSize(string poolId) => pools.ContainsKey(poolId) ? pools[poolId].Count : 0;
        public int TotalPooled => totalPooled;
        public int TotalReused => totalReused;
        public float ReuseRate => totalPooled > 0 ? (float)totalReused / (totalReused + totalPooled) : 0f;
    }
}
