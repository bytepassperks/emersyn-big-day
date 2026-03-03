using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Rooms
{
    /// <summary>
    /// Manages all 9 game rooms, transitions, decorations, and interactable objects.
    /// Each room has unique lighting, ambient sounds, and interactive elements.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Room Configuration")]
        public RoomData[] Rooms;
        public int CurrentRoomIndex = 0;
        public float TransitionDuration = 1.5f;

        [Header("Room References")]
        public Transform RoomContainer;
        public Camera MainCamera;

        private GameObject currentRoomInstance;
        private bool isTransitioning = false;

        public event Action<RoomData> OnRoomChanged;
        public event Action<string> OnRoomLoading;
        public event Action OnRoomReady;

        public RoomData CurrentRoom => Rooms != null && CurrentRoomIndex < Rooms.Length ? Rooms[CurrentRoomIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (Rooms != null && Rooms.Length > 0) LoadRoom(0);
        }

        public void LoadRoom(int index)
        {
            if (isTransitioning || index < 0 || Rooms == null || index >= Rooms.Length) return;
            StartCoroutine(TransitionToRoom(index));
        }

        public void LoadRoom(string roomName)
        {
            if (Rooms == null) return;
            for (int i = 0; i < Rooms.Length; i++)
            {
                if (Rooms[i].RoomName == roomName) { LoadRoom(i); return; }
            }
        }

        private System.Collections.IEnumerator TransitionToRoom(int newIndex)
        {
            isTransitioning = true;
            OnRoomLoading?.Invoke(Rooms[newIndex].RoomName);

            // Fade out
            float timer = 0f;
            while (timer < TransitionDuration * 0.5f)
            {
                timer += Time.deltaTime;
                float alpha = timer / (TransitionDuration * 0.5f);
                SetFadeAlpha(alpha);
                yield return null;
            }

            // Destroy old room
            if (currentRoomInstance != null) Destroy(currentRoomInstance);

            // Instantiate new room
            CurrentRoomIndex = newIndex;
            RoomData room = Rooms[newIndex];
            if (room.RoomPrefab != null)
            {
                currentRoomInstance = Instantiate(room.RoomPrefab, RoomContainer);
                currentRoomInstance.transform.localPosition = Vector3.zero;
            }

            // Apply room lighting
            ApplyRoomLighting(room);

            // Randomize decorations
            RandomizeDecorations(room);

            // Setup interactable objects
            SetupInteractables(room);

            OnRoomChanged?.Invoke(room);

            // Fade in
            timer = 0f;
            while (timer < TransitionDuration * 0.5f)
            {
                timer += Time.deltaTime;
                float alpha = 1f - (timer / (TransitionDuration * 0.5f));
                SetFadeAlpha(alpha);
                yield return null;
            }

            SetFadeAlpha(0f);
            isTransitioning = false;
            OnRoomReady?.Invoke();
        }

        private void ApplyRoomLighting(RoomData room)
        {
            RenderSettings.ambientLight = room.AmbientColor;
            RenderSettings.ambientIntensity = room.AmbientIntensity;

            if (room.Skybox != null) RenderSettings.skybox = room.Skybox;

            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight != null)
            {
                mainLight.color = room.MainLightColor;
                mainLight.intensity = room.MainLightIntensity;
            }
        }

        private void RandomizeDecorations(RoomData room)
        {
            if (room.DecorationSpots == null || room.DecorationPrefabs == null) return;
            foreach (var spot in room.DecorationSpots)
            {
                if (spot == null) continue;
                // Clear existing decorations
                foreach (Transform child in spot) Destroy(child.gameObject);

                // Place random decoration
                if (room.DecorationPrefabs.Length > 0 && UnityEngine.Random.value > 0.3f)
                {
                    int idx = UnityEngine.Random.Range(0, room.DecorationPrefabs.Length);
                    GameObject decor = Instantiate(room.DecorationPrefabs[idx], spot);
                    decor.transform.localPosition = Vector3.zero;
                    decor.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
                }
            }
        }

        private void SetupInteractables(RoomData room)
        {
            if (currentRoomInstance == null) return;
            var interactables = currentRoomInstance.GetComponentsInChildren<InteractableObject>();
            foreach (var obj in interactables)
            {
                obj.Initialize();
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            // UI fade overlay controlled by UIManager
            if (UI.UIManager.Instance != null) UI.UIManager.Instance.SetFadeOverlay(alpha);
        }

        public void GoToNextRoom()
        {
            if (Rooms == null) return;
            int next = (CurrentRoomIndex + 1) % Rooms.Length;
            LoadRoom(next);
        }

        public void GoToPreviousRoom()
        {
            if (Rooms == null) return;
            int prev = CurrentRoomIndex - 1;
            if (prev < 0) prev = Rooms.Length - 1;
            LoadRoom(prev);
        }
    }

    [Serializable]
    public class RoomData
    {
        public string RoomName;
        public RoomType Type;
        public GameObject RoomPrefab;
        public Sprite RoomIcon;
        public string Description;

        [Header("Lighting")]
        public Color AmbientColor = new Color(0.8f, 0.85f, 0.9f);
        public float AmbientIntensity = 1f;
        public Color MainLightColor = Color.white;
        public float MainLightIntensity = 1f;
        public Material Skybox;

        [Header("Decorations")]
        public Transform[] DecorationSpots;
        public GameObject[] DecorationPrefabs;

        [Header("Audio")]
        public AudioClip AmbientSound;
        public float AmbientVolume = 0.3f;

        [Header("Camera")]
        public Vector3 CameraOffset = new Vector3(0, 3, -5);
        public float CameraFOV = 60f;
    }

    public enum RoomType
    {
        Bedroom, Kitchen, Bathroom, Park,
        School, Arcade, Studio, Shop, Garden
    }

    /// <summary>
    /// An interactable object in a room that characters can use.
    /// Uses the Advertisement system from Sims-style AI.
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        public string ObjectName;
        public string[] Actions;
        public Advertisement[] Advertisements;
        public float InteractionRadius = 2f;
        public Transform InteractionPoint;
        public float CooldownTime = 30f;

        [Header("Visual Feedback")]
        public GameObject HighlightEffect;
        public ParticleSystem UseEffect;

        private float lastUsedTime = -999f;
        private bool isInUse = false;

        public bool IsAvailable => !isInUse && (Time.time - lastUsedTime) >= CooldownTime;

        public void Initialize()
        {
            if (HighlightEffect != null) HighlightEffect.SetActive(false);
        }

        public void ShowHighlight(bool show)
        {
            if (HighlightEffect != null) HighlightEffect.SetActive(show);
        }

        public void StartUse()
        {
            isInUse = true;
            if (UseEffect != null) UseEffect.Play();
        }

        public void EndUse()
        {
            isInUse = false;
            lastUsedTime = Time.time;
            if (UseEffect != null) UseEffect.Stop();
        }
    }

    [Serializable]
    public class Advertisement
    {
        public string ActionName;
        public string NeedAffected;
        public float NeedDelta;
        public float Duration;
        public float BaseScore = 1f;
        public string AnimationName;
    }
}
