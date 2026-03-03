using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Asset Bundle Manager for on-demand asset downloading from iDrive S3.
    /// Implements the AAA mobile game pattern: small APK + on-demand asset download.
    /// 
    /// Architecture:
    /// 1. APK contains only core code, loading screen UI, and minimal assets (~50-80MB)
    /// 2. On first launch, downloads asset bundles from iDrive S3 to persistent storage
    /// 3. Subsequent launches load from local cache (no re-download needed)
    /// 4. Supports incremental updates (only download changed bundles)
    /// 
    /// Asset Bundle Categories:
    /// - characters: GLB character models (emersyn, mom, dad, baby_brother, ava, sophia, cat, dog, bunny)
    /// - textures_room: Room textures (walls, floors, furniture)
    /// - textures_character: Character texture overlays (Modal GPU-upscaled)
    /// - textures_ui: UI element textures
    /// - audio: Sound effects and music
    /// - lightmaps: Baked lightmap data
    /// </summary>
    public class AssetBundleManager : MonoBehaviour
    {
        public static AssetBundleManager Instance { get; private set; }

        [Header("S3 Configuration")]
        [SerializeField] private string s3Endpoint = "https://s3.us-west-1.idrivee2.com";
        [SerializeField] private string s3Bucket = "crop-spray-uploads";
        [SerializeField] private string s3AssetPath = "emersyn-big-day/bundles";

        [Header("Download Settings")]
        [SerializeField] private int maxConcurrentDownloads = 3;
        [SerializeField] private int downloadTimeoutSeconds = 120;
        [SerializeField] private int maxRetries = 3;

        // Events for UI updates
        public event Action<float> OnOverallProgressChanged;
        public event Action<string, float> OnBundleProgressChanged;
        public event Action<string> OnStatusChanged;
        public event Action OnAllDownloadsComplete;
        public event Action<string> OnDownloadError;

        // Bundle manifest tracking
        private Dictionary<string, BundleInfo> bundleManifest = new Dictionary<string, BundleInfo>();
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private HashSet<string> downloadingBundles = new HashSet<string>();
        private int totalBundles;
        private int completedBundles;
        private bool isInitialized;

        // Local cache path
        private string CachePath => Path.Combine(Application.persistentDataPath, "AssetBundles");
        private string ManifestPath => Path.Combine(CachePath, "manifest.json");

        [Serializable]
        public class BundleInfo
        {
            public string name;
            public string hash;
            public long sizeBytes;
            public string[] assets;
            public bool required; // If true, must download before game starts
            public int priority; // Lower = higher priority
        }

        [Serializable]
        public class BundleManifest
        {
            public string version;
            public long timestamp;
            public BundleInfo[] bundles;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure cache directory exists
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);
        }

        /// <summary>
        /// Initialize the asset bundle system. Call this on app launch.
        /// Downloads the manifest from S3 and determines which bundles need updating.
        /// </summary>
        public IEnumerator Initialize()
        {
            OnStatusChanged?.Invoke("Checking for updates...");
            Debug.Log("[AssetBundleManager] Initializing...");

            // Step 1: Download remote manifest from S3
            string manifestUrl = $"{s3Endpoint}/{s3Bucket}/{s3AssetPath}/manifest.json";
            BundleManifest remoteManifest = null;

            using (var request = UnityWebRequest.Get(manifestUrl))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        remoteManifest = JsonUtility.FromJson<BundleManifest>(request.downloadHandler.text);
                        Debug.Log($"[AssetBundleManager] Remote manifest v{remoteManifest.version} with {remoteManifest.bundles.Length} bundles");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[AssetBundleManager] Failed to parse remote manifest: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AssetBundleManager] Failed to download manifest: {request.error}");
                    // Try loading local manifest as fallback
                    if (File.Exists(ManifestPath))
                    {
                        string localJson = File.ReadAllText(ManifestPath);
                        remoteManifest = JsonUtility.FromJson<BundleManifest>(localJson);
                        Debug.Log("[AssetBundleManager] Using cached manifest");
                    }
                }
            }

            if (remoteManifest == null)
            {
                Debug.Log("[AssetBundleManager] No manifest available - using StreamingAssets fallback");
                isInitialized = true;
                OnAllDownloadsComplete?.Invoke();
                yield break;
            }

            // Step 2: Compare with local cache to determine what needs downloading
            BundleManifest localManifest = null;
            if (File.Exists(ManifestPath))
            {
                try
                {
                    localManifest = JsonUtility.FromJson<BundleManifest>(File.ReadAllText(ManifestPath));
                }
                catch { }
            }

            var bundlesToDownload = new List<BundleInfo>();
            foreach (var bundle in remoteManifest.bundles)
            {
                bundleManifest[bundle.name] = bundle;
                string localPath = Path.Combine(CachePath, bundle.name);

                bool needsDownload = !File.Exists(localPath);
                if (!needsDownload && localManifest != null)
                {
                    // Check if hash changed (content updated)
                    var localBundle = System.Array.Find(localManifest.bundles, b => b.name == bundle.name);
                    if (localBundle == null || localBundle.hash != bundle.hash)
                        needsDownload = true;
                }

                if (needsDownload)
                    bundlesToDownload.Add(bundle);
            }

            totalBundles = bundlesToDownload.Count;
            completedBundles = 0;

            if (totalBundles == 0)
            {
                Debug.Log("[AssetBundleManager] All bundles up to date");
                OnStatusChanged?.Invoke("Ready!");
                isInitialized = true;
                OnAllDownloadsComplete?.Invoke();
                yield break;
            }

            // Step 3: Download bundles in priority order with concurrency control
            bundlesToDownload.Sort((a, b) => a.priority.CompareTo(b.priority));

            long totalBytes = 0;
            foreach (var b in bundlesToDownload) totalBytes += b.sizeBytes;
            OnStatusChanged?.Invoke($"Downloading {totalBundles} asset packs ({FormatBytes(totalBytes)})...");

            // Download required bundles first (blocking), then optional in background
            var requiredBundles = bundlesToDownload.FindAll(b => b.required);
            var optionalBundles = bundlesToDownload.FindAll(b => !b.required);

            // Download required bundles with concurrency
            yield return StartCoroutine(DownloadBundlesConcurrent(requiredBundles));

            // Save manifest after required bundles are done
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(remoteManifest, true));

            isInitialized = true;
            OnStatusChanged?.Invoke("Loading game...");

            // Download optional bundles in background (non-blocking)
            if (optionalBundles.Count > 0)
                StartCoroutine(DownloadBundlesConcurrent(optionalBundles));
            else
                OnAllDownloadsComplete?.Invoke();
        }

        private IEnumerator DownloadBundlesConcurrent(List<BundleInfo> bundles)
        {
            var activeDownloads = new List<Coroutine>();
            int index = 0;

            while (index < bundles.Count || activeDownloads.Count > 0)
            {
                // Start new downloads up to max concurrent
                while (index < bundles.Count && downloadingBundles.Count < maxConcurrentDownloads)
                {
                    var bundle = bundles[index];
                    index++;
                    var coroutine = StartCoroutine(DownloadSingleBundle(bundle));
                    activeDownloads.Add(coroutine);
                }

                yield return null;

                // Clean up completed downloads
                activeDownloads.RemoveAll(c => c == null);
            }
        }

        private IEnumerator DownloadSingleBundle(BundleInfo bundle)
        {
            string url = $"{s3Endpoint}/{s3Bucket}/{s3AssetPath}/{bundle.name}";
            string localPath = Path.Combine(CachePath, bundle.name);
            downloadingBundles.Add(bundle.name);

            Debug.Log($"[AssetBundleManager] Downloading: {bundle.name} ({FormatBytes(bundle.sizeBytes)})");

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = downloadTimeoutSeconds;
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        OnBundleProgressChanged?.Invoke(bundle.name, operation.progress);
                        UpdateOverallProgress();
                        yield return null;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Save to cache
                        try
                        {
                            File.WriteAllBytes(localPath, request.downloadHandler.data);
                            Debug.Log($"[AssetBundleManager] Downloaded: {bundle.name} ({FormatBytes(request.downloadHandler.data.Length)})");
                            completedBundles++;
                            downloadingBundles.Remove(bundle.name);
                            UpdateOverallProgress();
                            yield break; // Success
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[AssetBundleManager] Failed to save {bundle.name}: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AssetBundleManager] Download attempt {attempt + 1}/{maxRetries} failed for {bundle.name}: {request.error}");
                        if (attempt < maxRetries - 1)
                            yield return new WaitForSeconds(2f * (attempt + 1)); // Exponential backoff
                    }
                }
            }

            // All retries failed
            downloadingBundles.Remove(bundle.name);
            string errorMsg = $"Failed to download {bundle.name} after {maxRetries} attempts";
            Debug.LogError($"[AssetBundleManager] {errorMsg}");
            OnDownloadError?.Invoke(errorMsg);
        }

        private void UpdateOverallProgress()
        {
            float progress = totalBundles > 0 ? (float)completedBundles / totalBundles : 1f;
            OnOverallProgressChanged?.Invoke(progress);

            if (completedBundles >= totalBundles)
                OnAllDownloadsComplete?.Invoke();
        }

        /// <summary>
        /// Load a specific asset from a bundle. Returns null if bundle not available.
        /// Falls back to StreamingAssets if bundle not cached.
        /// </summary>
        public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            // Try loading from cached bundle first
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                return bundle.LoadAsset<T>(assetName);
            }

            // Try loading bundle from cache
            string localPath = Path.Combine(CachePath, bundleName);
            if (File.Exists(localPath))
            {
                var loadedBundle = AssetBundle.LoadFromFile(localPath);
                if (loadedBundle != null)
                {
                    loadedBundles[bundleName] = loadedBundle;
                    return loadedBundle.LoadAsset<T>(assetName);
                }
            }

            Debug.LogWarning($"[AssetBundleManager] Bundle '{bundleName}' not available, asset '{assetName}' not loaded");
            return null;
        }

        /// <summary>
        /// Async version of LoadAsset for coroutine-based loading.
        /// </summary>
        public IEnumerator LoadAssetAsync<T>(string bundleName, string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            // Try from already-loaded bundle
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                var request = bundle.LoadAssetAsync<T>(assetName);
                yield return request;
                callback?.Invoke(request.asset as T);
                yield break;
            }

            // Try loading bundle from cache
            string localPath = Path.Combine(CachePath, bundleName);
            if (File.Exists(localPath))
            {
                var bundleRequest = AssetBundle.LoadFromFileAsync(localPath);
                yield return bundleRequest;

                if (bundleRequest.assetBundle != null)
                {
                    loadedBundles[bundleName] = bundleRequest.assetBundle;
                    var assetRequest = bundleRequest.assetBundle.LoadAssetAsync<T>(assetName);
                    yield return assetRequest;
                    callback?.Invoke(assetRequest.asset as T);
                    yield break;
                }
            }

            Debug.LogWarning($"[AssetBundleManager] Bundle '{bundleName}' not found in cache");
            callback?.Invoke(null);
        }

        /// <summary>
        /// Load raw bytes from cache or StreamingAssets fallback.
        /// Used for GLB files that need to be loaded via GLTFast.
        /// </summary>
        public IEnumerator LoadBytesAsync(string bundleName, string fileName, Action<byte[]> callback)
        {
            // Try from cache first
            string cachedPath = Path.Combine(CachePath, bundleName, fileName);
            if (File.Exists(cachedPath))
            {
                byte[] data = File.ReadAllBytes(cachedPath);
                callback?.Invoke(data);
                yield break;
            }

            // Fallback: load from StreamingAssets (included in APK)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Characters", fileName);
            using (var request = UnityWebRequest.Get(streamingPath))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(request.downloadHandler.data);
                }
                else
                {
                    Debug.LogWarning($"[AssetBundleManager] Failed to load {fileName}: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Check if a specific bundle is cached locally.
        /// </summary>
        public bool IsBundleCached(string bundleName)
        {
            return File.Exists(Path.Combine(CachePath, bundleName));
        }

        /// <summary>
        /// Get total cache size in bytes.
        /// </summary>
        public long GetCacheSize()
        {
            if (!Directory.Exists(CachePath)) return 0;
            long size = 0;
            foreach (var file in Directory.GetFiles(CachePath, "*", SearchOption.AllDirectories))
                size += new FileInfo(file).Length;
            return size;
        }

        /// <summary>
        /// Clear all cached bundles. Forces re-download on next launch.
        /// </summary>
        public void ClearCache()
        {
            foreach (var kvp in loadedBundles)
                kvp.Value.Unload(true);
            loadedBundles.Clear();

            if (Directory.Exists(CachePath))
                Directory.Delete(CachePath, true);
            Directory.CreateDirectory(CachePath);

            Debug.Log("[AssetBundleManager] Cache cleared");
        }

        /// <summary>
        /// Unload a specific bundle to free memory.
        /// </summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                loadedBundles.Remove(bundleName);
                Debug.Log($"[AssetBundleManager] Unloaded bundle: {bundleName}");
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576f:F1} MB";
            return $"{bytes / 1073741824f:F1} GB";
        }

        private void OnDestroy()
        {
            foreach (var kvp in loadedBundles)
            {
                try { kvp.Value.Unload(false); }
                catch { }
            }
            loadedBundles.Clear();
        }
    }
}
