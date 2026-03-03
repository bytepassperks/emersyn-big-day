using UnityEngine;
using UnityEngine.UI;
using EmersynBigDay.Core;

namespace EmersynBigDay.UI
{
    /// <summary>
    /// Loading screen UI for asset bundle downloads.
    /// Shows progress bar, status text, and download details during first-launch asset download.
    /// Implements the AAA mobile game loading pattern (Genshin Impact, PUBG Mobile style).
    /// </summary>
    public class DownloadProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject downloadPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text statusText;
        [SerializeField] private Text percentText;
        [SerializeField] private Text detailText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button skipButton;

        [Header("Visual Settings")]
        [SerializeField] private Color progressBarColor = new Color(1f, 0.4f, 0.6f); // Emersyn pink
        [SerializeField] private Color progressBarBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private bool hasError;

        private void Start()
        {
            if (downloadPanel != null)
                downloadPanel.SetActive(false);

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(false);
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Subscribe to AssetBundleManager events
            if (AssetBundleManager.Instance != null)
            {
                AssetBundleManager.Instance.OnOverallProgressChanged += UpdateProgress;
                AssetBundleManager.Instance.OnStatusChanged += UpdateStatus;
                AssetBundleManager.Instance.OnBundleProgressChanged += UpdateBundleDetail;
                AssetBundleManager.Instance.OnAllDownloadsComplete += OnComplete;
                AssetBundleManager.Instance.OnDownloadError += OnError;
            }
        }

        /// <summary>
        /// Show the download progress UI. Call when asset download begins.
        /// </summary>
        public void Show()
        {
            if (downloadPanel != null)
                downloadPanel.SetActive(true);

            hasError = false;
            UpdateProgress(0f);
            UpdateStatus("Preparing download...");
        }

        /// <summary>
        /// Hide the download progress UI. Call when downloads complete.
        /// </summary>
        public void Hide()
        {
            if (downloadPanel != null)
                downloadPanel.SetActive(false);
        }

        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
                progressBar.value = progress;

            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
        }

        private void UpdateBundleDetail(string bundleName, float bundleProgress)
        {
            if (detailText != null)
                detailText.text = $"Downloading: {bundleName} ({Mathf.RoundToInt(bundleProgress * 100)}%)";
        }

        private void OnComplete()
        {
            UpdateStatus("Download complete!");
            UpdateProgress(1f);

            if (detailText != null)
                detailText.text = "";

            // Auto-hide after brief delay
            Invoke(nameof(Hide), 1.5f);
        }

        private void OnError(string error)
        {
            hasError = true;
            UpdateStatus($"Download error: {error}");

            if (retryButton != null)
                retryButton.gameObject.SetActive(true);
            if (skipButton != null)
                skipButton.gameObject.SetActive(true);
        }

        private void OnRetryClicked()
        {
            hasError = false;
            if (retryButton != null)
                retryButton.gameObject.SetActive(false);
            if (skipButton != null)
                skipButton.gameObject.SetActive(false);

            // Re-initialize downloads
            if (AssetBundleManager.Instance != null)
                StartCoroutine(AssetBundleManager.Instance.Initialize());
        }

        private void OnSkipClicked()
        {
            Hide();
            // Game will use StreamingAssets fallback
            Debug.Log("[DownloadProgressUI] User skipped download, using bundled assets");
        }

        /// <summary>
        /// Create the download progress UI programmatically (no prefab needed).
        /// Returns the created DownloadProgressUI component.
        /// </summary>
        public static DownloadProgressUI CreateUI(Transform canvasTransform)
        {
            // Panel background
            var panelObj = new GameObject("DownloadProgressPanel");
            panelObj.transform.SetParent(canvasTransform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.05f, 0.15f, 0.95f); // Dark purple overlay

            // Status text
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(panelObj.transform, false);
            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.55f);
            statusRect.anchorMax = new Vector2(0.9f, 0.65f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            var statusTxt = statusObj.AddComponent<Text>();
            statusTxt.text = "Preparing download...";
            statusTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusTxt.fontSize = 24;
            statusTxt.color = Color.white;
            statusTxt.alignment = TextAnchor.MiddleCenter;

            // Progress bar background
            var barBgObj = new GameObject("ProgressBarBg");
            barBgObj.transform.SetParent(panelObj.transform, false);
            var barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.1f, 0.45f);
            barBgRect.anchorMax = new Vector2(0.9f, 0.5f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            var barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Progress bar slider
            var sliderObj = new GameObject("ProgressSlider");
            sliderObj.transform.SetParent(panelObj.transform, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.1f, 0.45f);
            sliderRect.anchorMax = new Vector2(0.9f, 0.5f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            // Fill area
            var fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.4f, 0.6f); // Emersyn pink
            slider.fillRect = fillRect;

            // Percent text
            var percentObj = new GameObject("PercentText");
            percentObj.transform.SetParent(panelObj.transform, false);
            var percentRect = percentObj.AddComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.1f, 0.38f);
            percentRect.anchorMax = new Vector2(0.9f, 0.45f);
            percentRect.offsetMin = Vector2.zero;
            percentRect.offsetMax = Vector2.zero;
            var percentTxt = percentObj.AddComponent<Text>();
            percentTxt.text = "0%";
            percentTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentTxt.fontSize = 20;
            percentTxt.color = Color.white;
            percentTxt.alignment = TextAnchor.MiddleCenter;

            // Detail text
            var detailObj = new GameObject("DetailText");
            detailObj.transform.SetParent(panelObj.transform, false);
            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.1f, 0.32f);
            detailRect.anchorMax = new Vector2(0.9f, 0.38f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;
            var detailTxt = detailObj.AddComponent<Text>();
            detailTxt.text = "";
            detailTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailTxt.fontSize = 16;
            detailTxt.color = new Color(0.7f, 0.7f, 0.7f);
            detailTxt.alignment = TextAnchor.MiddleCenter;

            // Create component and wire up references
            var ui = panelObj.AddComponent<DownloadProgressUI>();
            ui.downloadPanel = panelObj;
            ui.progressBar = slider;
            ui.statusText = statusTxt;
            ui.percentText = percentTxt;
            ui.detailText = detailTxt;

            return ui;
        }

        private void OnDestroy()
        {
            if (AssetBundleManager.Instance != null)
            {
                AssetBundleManager.Instance.OnOverallProgressChanged -= UpdateProgress;
                AssetBundleManager.Instance.OnStatusChanged -= UpdateStatus;
                AssetBundleManager.Instance.OnBundleProgressChanged -= UpdateBundleDetail;
                AssetBundleManager.Instance.OnAllDownloadsComplete -= OnComplete;
                AssetBundleManager.Instance.OnDownloadError -= OnError;
            }
        }
    }
}
