using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Builds the initial game scene: creates all managers, sets up lighting,
    /// loads the first room, spawns characters, and initializes all systems.
    /// This is the main entry point for the game.
    /// </summary>
    public class SceneBuilder : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject GameManagerPrefab;
        public GameObject UIManagerPrefab;
        public GameObject AudioManagerPrefab;
        public GameObject InputManagerPrefab;
        public GameObject SaveManagerPrefab;
        public GameObject CameraControllerPrefab;
        public GameObject ParticleManagerPrefab;
        public GameObject PostProcessingPrefab;

        [Header("Character Prefabs")]
        public GameObject EmersynPrefab;
        public GameObject[] FriendPrefabs;
        public GameObject[] PetPrefabs;
        public GameObject[] NPCPrefabs;

        [Header("Room Configuration")]
        public Rooms.RoomData[] RoomDataList;

        [Header("Lighting")]
        public Color AmbientColor = new Color(0.85f, 0.88f, 0.92f);
        public float AmbientIntensity = 1.1f;
        public Material DefaultSkybox;

        [Header("Initial Settings")]
        public int StartingRoom = 0;
        public Vector3 CharacterSpawnPosition = new Vector3(0, 0, 0);

        private bool isInitialized = false;

        private void Awake()
        {
            if (isInitialized) return;
            isInitialized = true;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;

            InitializeManagers();
            SetupLighting();
            SpawnCharacters();
            InitializeGameSystems();
        }

        private void InitializeManagers()
        {
            // Create all singleton managers
            SafeInstantiate(GameManagerPrefab, "GameManager");
            SafeInstantiate(UIManagerPrefab, "UIManager");
            SafeInstantiate(AudioManagerPrefab, "AudioManager");
            SafeInstantiate(InputManagerPrefab, "InputManager");
            SafeInstantiate(SaveManagerPrefab, "SaveManager");
            SafeInstantiate(ParticleManagerPrefab, "ParticleManager");
            SafeInstantiate(PostProcessingPrefab, "PostProcessing");

            // Camera controller
            if (CameraControllerPrefab != null)
            {
                var camObj = Instantiate(CameraControllerPrefab);
                camObj.name = "MainCamera";
            }
        }

        private void SafeInstantiate(GameObject prefab, string fallbackName)
        {
            if (prefab != null)
            {
                var obj = Instantiate(prefab);
                obj.name = fallbackName;
            }
            else
            {
                // Create empty manager container if no prefab assigned
                var obj = new GameObject(fallbackName);
                Debug.LogWarning($"No prefab assigned for {fallbackName}, created empty GameObject");
            }
        }

        private void SetupLighting()
        {
            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            RenderSettings.ambientIntensity = AmbientIntensity;

            if (DefaultSkybox != null)
            {
                RenderSettings.skybox = DefaultSkybox;
            }

            // Main directional light
            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight == null)
            {
                var lightObj = new GameObject("MainDirectionalLight");
                mainLight = lightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
            }

            mainLight.color = new Color(1f, 0.96f, 0.88f); // Warm sunlight
            mainLight.intensity = 1.2f;
            mainLight.shadows = LightShadows.Soft;
            mainLight.shadowStrength = 0.6f;
            mainLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Fill light (opposite side, softer)
            var fillLightObj = new GameObject("FillLight");
            var fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.7f, 0.8f, 1f); // Cool blue fill
            fillLight.intensity = 0.4f;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

            // Rim light (from behind, for character outline)
            var rimLightObj = new GameObject("RimLight");
            var rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(1f, 0.9f, 0.8f);
            rimLight.intensity = 0.3f;
            rimLight.shadows = LightShadows.None;
            rimLight.transform.rotation = Quaternion.Euler(-20f, 180f, 0f);
        }

        private void SpawnCharacters()
        {
            // Spawn main character (Emersyn)
            if (EmersynPrefab != null)
            {
                var emersyn = Instantiate(EmersynPrefab, CharacterSpawnPosition, Quaternion.identity);
                emersyn.name = "Emersyn";

                var controller = emersyn.GetComponent<Characters.CharacterController3D>();
                if (controller != null)
                {
                    controller.CharacterName = "Emersyn";
                    controller.IsMainCharacter = true;
                    controller.Type = Characters.CharacterController3D.CharacterType.Player;
                }

                // Set camera target
                if (CameraSystem.CameraController.Instance != null)
                {
                    CameraSystem.CameraController.Instance.Target = emersyn.transform;
                }
            }

            // Spawn NPC friends at offset positions
            if (FriendPrefabs != null)
            {
                string[] friendNames = { "Ava", "Mia", "Leo" };
                for (int i = 0; i < FriendPrefabs.Length && i < friendNames.Length; i++)
                {
                    if (FriendPrefabs[i] == null) continue;
                    Vector3 offset = new Vector3(2f + i * 2f, 0, UnityEngine.Random.Range(-1f, 1f));
                    var friend = Instantiate(FriendPrefabs[i], CharacterSpawnPosition + offset, Quaternion.identity);
                    friend.name = friendNames[i];

                    var controller = friend.GetComponent<Characters.CharacterController3D>();
                    if (controller != null)
                    {
                        controller.CharacterName = friendNames[i];
                        controller.Type = Characters.CharacterController3D.CharacterType.Friend;
                    }
                }
            }

            // Spawn pets
            if (PetPrefabs != null)
            {
                string[] petNames = { "Kitty", "Puppy", "Bunny" };
                for (int i = 0; i < PetPrefabs.Length && i < petNames.Length; i++)
                {
                    if (PetPrefabs[i] == null) continue;
                    Vector3 offset = new Vector3(-1f - i * 1.5f, 0, UnityEngine.Random.Range(-0.5f, 0.5f));
                    var pet = Instantiate(PetPrefabs[i], CharacterSpawnPosition + offset, Quaternion.identity);
                    pet.name = petNames[i];

                    var controller = pet.GetComponent<Characters.CharacterController3D>();
                    if (controller != null)
                    {
                        controller.CharacterName = petNames[i];
                        controller.Type = Characters.CharacterController3D.CharacterType.Pet;
                    }
                }
            }
        }

        private void InitializeGameSystems()
        {
            // Load room
            if (Rooms.RoomManager.Instance != null && RoomDataList != null)
            {
                Rooms.RoomManager.Instance.Rooms = RoomDataList;
                Rooms.RoomManager.Instance.LoadRoom(StartingRoom);
            }

            // Auto-detect quality
            if (PostProcessingSetup.Instance != null)
            {
                PostProcessingSetup.Instance.AutoDetectQuality();
            }

            // Start music
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayGameplayMusic();
            }

            // Wire up input events
            WireInputEvents();

            // Load save data (done by SaveManager.Start())

            // Check daily reward
            if (DailyEventSystem.Instance != null)
            {
                // Will auto-check on Start
            }

            Debug.Log("Emersyn's Big Day - Scene fully initialized!");
        }

        private void WireInputEvents()
        {
            var input = InputSystem.InputManager.Instance;
            if (input == null) return;

            // Tap to interact
            input.OnTap += (worldPos) =>
            {
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("tap");
                if (UI.UIManager.Instance != null)
                    UI.UIManager.Instance.SpawnTapEffect(worldPos);
            };

            // Object tap
            input.OnObjectTapped += (obj) =>
            {
                var interactable = obj.GetComponent<Rooms.InteractableObject>();
                if (interactable != null && interactable.IsAvailable)
                {
                    interactable.ShowHighlight(true);
                }

                var character = obj.GetComponent<Characters.CharacterController3D>();
                if (character != null)
                {
                    character.OnPokeHead();
                    if (RewardSystem.Instance != null)
                        RewardSystem.Instance.GrantInteractionReward("poke");
                }
            };

            // Swipe for room navigation
            input.OnSwipe += (direction) =>
            {
                // Skip room navigation if a mini-game is active
                // MiniGameManager is in a separate assembly, check via FindFirstObjectByType
                var miniGameMgr = FindFirstObjectByType<MonoBehaviour>();
                // Simple guard: we'll let room navigation always work for now
                // Mini-game blocking will be handled by the MiniGameManager itself
                if (false) return; // Placeholder for cross-assembly mini-game check

                if (direction == InputSystem.InputManager.SwipeDirection.Left)
                    Rooms.RoomManager.Instance?.GoToNextRoom();
                else if (direction == InputSystem.InputManager.SwipeDirection.Right)
                    Rooms.RoomManager.Instance?.GoToPreviousRoom();
            };

            // Pinch to zoom camera
            input.OnPinchZoom += (delta) =>
            {
                if (CameraSystem.CameraController.Instance != null)
                    CameraSystem.CameraController.Instance.Zoom(delta);
            };

            // Drag for camera orbit
            input.OnDrag += (pos, delta) =>
            {
                if (CameraSystem.CameraController.Instance != null)
                {
                    CameraSystem.CameraController.Instance.OrbitHorizontal(delta.x * 0.01f);
                    CameraSystem.CameraController.Instance.OrbitVertical(-delta.y * 0.01f);
                }
            };
        }
    }
}
