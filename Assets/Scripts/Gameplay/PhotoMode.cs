using UnityEngine;
using System;
using System.IO;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #22: Photo mode for taking in-game screenshots with filters and stickers.
    /// Like My Talking Angela 2's photo booth and Toca Life's screenshot sharing.
    /// </summary>
    public class PhotoMode : MonoBehaviour
    {
        public static PhotoMode Instance { get; private set; }

        [Header("Settings")]
        public bool IsActive;
        public int PhotoResolution = 1080;

        [Header("Filters")]
        public PhotoFilter CurrentFilter = PhotoFilter.None;

        private Camera photoCamera;
        private int photosTaken;

        public event Action<string> OnPhotoTaken;
        public event Action OnPhotoModeEntered;
        public event Action OnPhotoModeExited;

        public int PhotosTaken => photosTaken;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void EnterPhotoMode()
        {
            IsActive = true;
            if (photoCamera == null)
            {
                var camObj = new GameObject("PhotoCamera");
                camObj.transform.SetParent(transform);
                photoCamera = camObj.AddComponent<Camera>();
                photoCamera.CopyFrom(Camera.main);
                photoCamera.enabled = false;
            }
            OnPhotoModeEntered?.Invoke();
        }

        public void ExitPhotoMode()
        {
            IsActive = false;
            OnPhotoModeExited?.Invoke();
        }

        public string TakePhoto()
        {
            if (photoCamera == null) photoCamera = Camera.main;

            int width = PhotoResolution;
            int height = Mathf.CeilToInt(width * 1.778f); // 16:9 portrait
            RenderTexture rt = new RenderTexture(width, height, 24);
            photoCamera.targetTexture = rt;
            photoCamera.Render();

            RenderTexture.active = rt;
            Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            // Apply filter
            ApplyFilter(photo);

            photo.Apply();
            photoCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            // Save to persistent data
            string filename = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(Application.persistentDataPath, "Photos", filename);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, photo.EncodeToPNG());
            Destroy(photo);

            photosTaken++;

            // Rewards
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.AddCoins(3);
                Core.GameManager.Instance.AddXP(5);
            }

            // Collection tracking
            if (CollectionSystem.Instance != null)
                CollectionSystem.Instance.CollectItem("photos_selfie", filename);

            // Quest tracking
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.ReportProgress("take_photo");

            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnSparkles(Vector3.up * 2f);

            OnPhotoTaken?.Invoke(path);
            return path;
        }

        private void ApplyFilter(Texture2D photo)
        {
            if (CurrentFilter == PhotoFilter.None) return;

            Color[] pixels = photo.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                switch (CurrentFilter)
                {
                    case PhotoFilter.Warm:
                        pixels[i].r = Mathf.Min(1f, pixels[i].r * 1.15f);
                        pixels[i].g = pixels[i].g * 1.05f;
                        pixels[i].b = pixels[i].b * 0.9f;
                        break;
                    case PhotoFilter.Cool:
                        pixels[i].r = pixels[i].r * 0.9f;
                        pixels[i].g = pixels[i].g * 1.0f;
                        pixels[i].b = Mathf.Min(1f, pixels[i].b * 1.15f);
                        break;
                    case PhotoFilter.Vintage:
                        float gray = pixels[i].grayscale;
                        pixels[i] = Color.Lerp(pixels[i], new Color(gray * 1.1f, gray * 0.95f, gray * 0.8f), 0.5f);
                        break;
                    case PhotoFilter.Sparkle:
                        pixels[i] = Color.Lerp(pixels[i], Color.white, Mathf.Max(0, pixels[i].grayscale - 0.8f) * 2f);
                        break;
                    case PhotoFilter.Rainbow:
                        float hue = (float)i / pixels.Length;
                        Color rainbow = Color.HSVToRGB(hue % 1f, 0.3f, 1f);
                        pixels[i] = Color.Lerp(pixels[i], rainbow, 0.15f);
                        break;
                }
            }
            photo.SetPixels(pixels);
        }

        public void SetFilter(PhotoFilter filter) { CurrentFilter = filter; }
        public void NextFilter()
        {
            CurrentFilter = (PhotoFilter)(((int)CurrentFilter + 1) % System.Enum.GetValues(typeof(PhotoFilter)).Length);
        }
    }

    public enum PhotoFilter { None, Warm, Cool, Vintage, Sparkle, Rainbow, Dreamy, Pastel }
}
