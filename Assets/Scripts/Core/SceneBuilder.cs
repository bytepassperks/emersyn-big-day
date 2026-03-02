using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GLTFast;
// NavMesh baking uses collider-based approach (no AI Navigation package needed)

namespace EmersynBigDay.Core
{
    public class SceneBuilder : MonoBehaviour
    {
        private bool isInitialized = false;
        private Camera mainCamera;
        private Canvas uiCanvas;
        private Transform roomContainer;
        private Transform characterContainer;
        private GameObject emersynObj;
        private int currentRoomIndex = 0;

        private Text coinText;
        private Text starText;
        private Text levelText;
        private Text dayText;
        private Text roomNameText;
        private Text moodText;
        private Slider xpBar;
        private Slider[] needBars;
        private Text[] needLabels;

        private static readonly string[] RoomNames = {
            "Bedroom", "Kitchen", "Bathroom", "Park",
            "School", "Arcade", "Studio", "Shop", "Garden"
        };
        private static readonly Color[] RoomFloorColors = {
            new Color(0.95f, 0.85f, 0.90f), new Color(0.90f, 0.95f, 0.85f),
            new Color(0.85f, 0.92f, 0.98f), new Color(0.70f, 0.90f, 0.60f),
            new Color(0.95f, 0.92f, 0.80f), new Color(0.80f, 0.75f, 0.95f),
            new Color(0.98f, 0.90f, 0.80f), new Color(0.95f, 0.88f, 0.75f),
            new Color(0.75f, 0.92f, 0.75f)
        };
        private static readonly Color[] RoomWallColors = {
            new Color(1.0f, 0.92f, 0.95f), new Color(1.0f, 1.0f, 0.93f),
            new Color(0.93f, 0.97f, 1.0f), new Color(0.55f, 0.80f, 0.95f),
            new Color(1.0f, 0.97f, 0.90f), new Color(0.20f, 0.15f, 0.30f),
            new Color(1.0f, 0.95f, 0.88f), new Color(1.0f, 0.95f, 0.85f),
            new Color(0.55f, 0.80f, 0.95f)
        };

        private static readonly string[] CharacterNames = { "Emersyn", "Ava", "Mia", "Leo" };
        private static readonly Color[] CharBodyColors = {
            new Color(1.0f, 0.75f, 0.80f), new Color(0.75f, 0.85f, 1.0f),
            new Color(0.80f, 1.0f, 0.75f), new Color(1.0f, 0.90f, 0.60f)
        };
        private static readonly string[] PetNames = { "Kitty", "Puppy", "Bunny" };
        private static readonly Color[] PetColors = {
            new Color(1.0f, 0.85f, 0.50f), new Color(0.80f, 0.65f, 0.45f), new Color(1.0f, 1.0f, 1.0f)
        };
        private static readonly string[] NeedNames = {
            "Hunger", "Energy", "Hygiene", "Fun", "Social", "Comfort", "Bladder", "Creativity"
        };
        private static readonly Color[] NeedColors = {
            new Color(1.0f, 0.6f, 0.2f), new Color(1.0f, 0.9f, 0.2f),
            new Color(0.3f, 0.7f, 1.0f), new Color(1.0f, 0.3f, 0.6f),
            new Color(0.6f, 0.3f, 1.0f), new Color(0.5f, 0.8f, 0.4f),
            new Color(0.2f, 0.6f, 0.9f), new Color(1.0f, 0.5f, 0.8f)
        };

        private Material baseMat;
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private bool glbLoadingComplete = false;

        // Room texture mappings: room index -> (floor texture path, wall texture path)
        private static readonly string[][] RoomTexturePaths = {
            new[] { "rooms/bedroom/bedroom_floor_wood_albedo.png", "rooms/bedroom/bedroom_wall_lavender_albedo.png",
                    "rooms/bedroom/bedroom_floor_wood_normal.png", "rooms/bedroom/bedroom_wall_lavender_normal.png" },
            new[] { "rooms/kitchen/kitchen_floor_tile_albedo.png", "rooms/kitchen/kitchen_wall_mint_albedo.png",
                    "rooms/kitchen/kitchen_floor_tile_normal.png", "rooms/kitchen/kitchen_wall_mint_normal.png" },
            new[] { "rooms/bathroom/bathroom_floor_tile_albedo.png", "rooms/bathroom/bathroom_wall_blue_albedo.png",
                    "rooms/bathroom/bathroom_floor_tile_normal.png", "rooms/bathroom/bathroom_wall_blue_normal.png" },
            new[] { "rooms/park/park_grass_lush_albedo.png", "",
                    "rooms/park/park_grass_lush_normal.png", "" },
            new[] { "rooms/school/school_floor_linoleum_albedo.png", "rooms/school/school_wall_green_albedo.png",
                    "rooms/school/school_floor_linoleum_normal.png", "rooms/school/school_wall_green_normal.png" },
            new[] { "", "", "", "" }, // Arcade - no textures yet
            new[] { "", "", "", "" }, // Studio - no textures yet
            new[] { "rooms/shop/shop_floor_wood_albedo.png", "rooms/shop/shop_wall_warm_albedo.png",
                    "rooms/shop/shop_floor_wood_normal.png", "rooms/shop/shop_wall_warm_normal.png" },
            new[] { "rooms/garden/garden_grass_albedo.png", "",
                    "rooms/garden/garden_grass_normal.png", "" },
        };

        // GLB character name mappings
        private static readonly string[] GLBCharacterFiles = { "emersyn", "ava", "mia", "leo" };
        private static readonly string[] GLBPetFiles = { "cat", "dog", "bunny" };

        private void Awake()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Force portrait orientation on Android
            #if UNITY_ANDROID
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            #endif

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Debug.Log("[SceneBuilder] Starting programmatic scene construction...");

            try
            {
                baseMat = CreateBaseMaterial();
                Debug.Log("[SceneBuilder] BaseMat created OK");
                CreateCamera();
                Debug.Log("[SceneBuilder] Camera created OK");
                CreateManagers();
                Debug.Log("[SceneBuilder] Managers created OK");
                SetupLighting();
                Debug.Log("[SceneBuilder] Lighting setup OK");
                CreateRoomContainer();
                BuildRoom(0);
                Debug.Log("[SceneBuilder] Room built OK");
                CreateCharacters();
                Debug.Log("[SceneBuilder] Characters created OK");
                CreateUI();
                Debug.Log("[SceneBuilder] UI created OK");
                WireUpSystems();
                Debug.Log("[SceneBuilder] Systems wired OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneBuilder] FATAL ERROR in Awake: {e.Message}\n{e.StackTrace}");
            }

            Debug.Log("[SceneBuilder] Scene fully initialized!");
            // Start async loading of GLB models and PBR textures
            StartCoroutine(SafeCoroutine(LoadGLBCharactersCoroutine()));
            StartCoroutine(SafeCoroutine(LoadAndApplyRoomTextures(currentRoomIndex)));
        }

        // Wrapper to catch exceptions in coroutines (which normally fail silently)
        private IEnumerator SafeCoroutine(IEnumerator coroutine)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!coroutine.MoveNext()) yield break;
                    current = coroutine.Current;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SceneBuilder] Coroutine error: {e.Message}\n{e.StackTrace}");
                    yield break;
                }
                yield return current;
            }
        }

        private Material CreateBaseMaterial()
        {
            // NUCLEAR FIX: Use Built-in Render Pipeline with Standard shader.
            // URP renderer reference kept failing in headless Unity 6 builds.
            // Standard shader is ALWAYS available in Built-in Pipeline.
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[SceneBuilder] Standard shader not found! Falling back to primitive.");
                var refPrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                refPrim.name = "_ShaderRef";
                refPrim.SetActive(false);
                DontDestroyOnLoad(refPrim);
                shader = refPrim.GetComponent<Renderer>().sharedMaterial.shader;
            }
            var mat = new Material(shader);
            mat.renderQueue = 2000;
            Debug.Log($"[SceneBuilder] Base material shader: {mat.shader?.name ?? "NULL"}");
            return mat;
        }

        private Material ColorMat(Color c)
        {
            var m = new Material(baseMat);
            // Built-in Standard shader uses _Color
            m.color = c;
            return m;
        }

        private void CreateCamera()
        {
            // Reuse existing camera from scene if present
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camObj = new GameObject("MainCamera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.55f, 0.80f, 0.95f);
            // Fix #2/#4: FOV 50 for portrait 1080x1920
            mainCamera.fieldOfView = 50f;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            if (mainCamera.GetComponent<CameraSystem.CameraController>() == null)
            {
                var ctrl = mainCamera.gameObject.AddComponent<CameraSystem.CameraController>();
                ctrl.Offset = new Vector3(0f, 3f, -5f);
                ctrl.CurrentZoom = 6f;
            }
            // Fix #2: Camera position for portrait — pull back and up to see room
            mainCamera.transform.position = new Vector3(0f, 5f, -8f);
            mainCamera.transform.LookAt(new Vector3(0f, 1.5f, 0f));
        }

        private void CreateManagers()
        {
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            DontDestroyOnLoad(gmObj);

            var needObj = new GameObject("NeedSystem");
            needObj.AddComponent<NeedSystem>();

            var inputObj = new GameObject("InputManager");
            var inputMgr = inputObj.AddComponent<InputSystem.InputManager>();
            inputMgr.MainCamera = mainCamera;

            var audioObj = new GameObject("AudioManager");
            var audioMgr = audioObj.AddComponent<Audio.AudioManager>();
            audioMgr.MusicSource = audioObj.AddComponent<AudioSource>();
            audioMgr.AmbientSource = MakeChildAudio(audioObj, "Ambient");
            audioMgr.SFXSource = MakeChildAudio(audioObj, "SFX");
            audioMgr.UISource = MakeChildAudio(audioObj, "UI");
            audioMgr.FootstepSource = MakeChildAudio(audioObj, "Footstep");
            DontDestroyOnLoad(audioObj);

            var pObj = new GameObject("ParticleManager");
            pObj.AddComponent<Particles.ParticleManager>();

            var sObj = new GameObject("SaveManager");
            sObj.AddComponent<Data.SaveManager>();
            DontDestroyOnLoad(sObj);

            new GameObject("RewardSystem").AddComponent<RewardSystem>();
            new GameObject("ShopSystem").AddComponent<ShopSystem>();
            new GameObject("AchievementSystem").AddComponent<AchievementSystem>();
            new GameObject("DailyEventSystem").AddComponent<DailyEventSystem>();
            new GameObject("RoomManager").AddComponent<Rooms.RoomManager>();
            new GameObject("PostProcessing").AddComponent<PostProcessingSetup>();

            // === Enhancement Systems ===
            // Visual
            new GameObject("ToonShading").AddComponent<Visual.ToonShading>();
            new GameObject("ProceduralParticles").AddComponent<Visual.ProceduralParticles>();
            new GameObject("DynamicLighting").AddComponent<Visual.DynamicLighting>();

            // Gameplay
            new GameObject("QuestSystem").AddComponent<Gameplay.QuestSystem>();
            new GameObject("CollectionSystem").AddComponent<Gameplay.CollectionSystem>();
            new GameObject("CharacterCustomization").AddComponent<Gameplay.CharacterCustomization>();
            new GameObject("MiniGameLauncher").AddComponent<Gameplay.MiniGameLauncher>();
            new GameObject("RoomDecorator").AddComponent<Gameplay.RoomDecorator>();
            new GameObject("PhotoMode").AddComponent<Gameplay.PhotoMode>();

            // Audio
            new GameObject("AdaptiveMusicSystem").AddComponent<Audio.AdaptiveMusicSystem>();
            new GameObject("CharacterVoiceSystem").AddComponent<Audio.CharacterVoiceSystem>();
            new GameObject("SpatialAudioSystem").AddComponent<Audio.SpatialAudioSystem>();

            // Animation
            new GameObject("EmotionalAnimator").AddComponent<Animation.EmotionalAnimator>();
            new GameObject("ActivityAnimations").AddComponent<Animation.ActivityAnimations>();

            // Performance
            new GameObject("LODManager").AddComponent<Performance.LODManager>();
            new GameObject("ObjectPoolManager").AddComponent<Performance.ObjectPoolManager>();
            new GameObject("PerformanceOptimizer").AddComponent<Performance.PerformanceOptimizer>();

            // Systems
            new GameObject("TutorialSystem").AddComponent<Systems.TutorialSystem>();
            new GameObject("RoomProgressionSystem").AddComponent<Systems.RoomProgressionSystem>();
            new GameObject("AnalyticsManager").AddComponent<Systems.AnalyticsManager>();
            new GameObject("SocialSystem").AddComponent<Systems.SocialSystem>();
            new GameObject("ParentGate").AddComponent<Systems.ParentGate>();
            new GameObject("AccessibilityManager").AddComponent<Systems.AccessibilityManager>();
            new GameObject("AdIntegration").AddComponent<Systems.AdIntegration>();
            new GameObject("CosmeticPackSystem").AddComponent<Systems.CosmeticPackSystem>();
            new GameObject("DailyRewardSystem").AddComponent<Systems.DailyRewardSystem>();
        }

        private AudioSource MakeChildAudio(GameObject parent, string name)
        {
            var child = new GameObject(name + "Source");
            child.transform.SetParent(parent.transform);
            return child.AddComponent<AudioSource>();
        }

        private void SetupLighting()
        {
            // Remove existing lights from the scene to avoid duplicates
            foreach (var existing in FindObjectsByType<Light>(FindObjectsSortMode.None))
                Destroy(existing.gameObject);

            // Apply RenderSettings (Built-in Pipeline - no deferred init needed)
            StartCoroutine(SetupRenderSettingsDeferred());

            var lo = new GameObject("MainDirectionalLight");
            var ml = lo.AddComponent<Light>();
            ml.type = LightType.Directional;
            ml.color = new Color(1f, 0.96f, 0.88f);
            ml.intensity = 1.2f;
            ml.shadows = LightShadows.Soft;
            ml.shadowStrength = 0.6f;
            lo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            ml.cullingMask = ~0;

            var fo = new GameObject("FillLight");
            var fl = fo.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.7f, 0.8f, 1f);
            fl.intensity = 0.4f;
            fl.shadows = LightShadows.None;
            fo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fl.cullingMask = ~0;

            var ro = new GameObject("RimLight");
            var rl = ro.AddComponent<Light>();
            rl.type = LightType.Directional;
            rl.color = new Color(1f, 0.9f, 0.8f);
            rl.intensity = 0.3f;
            rl.shadows = LightShadows.None;
            ro.transform.rotation = Quaternion.Euler(-20f, 180f, 0f);
            rl.cullingMask = ~0;
        }

        // Apply RenderSettings after frame renders
        private IEnumerator SetupRenderSettingsDeferred()
        {
            yield return new WaitForEndOfFrame();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.85f, 0.88f, 0.92f);
            RenderSettings.ambientIntensity = 1.1f;
        }

        private void CreateRoomContainer()
        {
            roomContainer = new GameObject("RoomContainer").transform;
        }

        private void BuildRoom(int index)
        {
            currentRoomIndex = index;
            foreach (Transform child in roomContainer) Destroy(child.gameObject);
            string roomName = RoomNames[index];
            Color floorColor = RoomFloorColors[index];
            Color wallColor = RoomWallColors[index];
            bool isOutdoor = (index == 3 || index == 8);

            MakeCube("Floor", new Vector3(0, -0.25f, 0), new Vector3(12, 0.5f, 10), floorColor, roomContainer);
            if (!isOutdoor)
            {
                MakeCube("BackWall", new Vector3(0, 2.5f, 5), new Vector3(12, 5, 0.3f), wallColor, roomContainer);
                MakeCube("LeftWall", new Vector3(-6, 2.5f, 0), new Vector3(0.3f, 5, 10), wallColor, roomContainer);
                MakeCube("RightWall", new Vector3(6, 2.5f, 0), new Vector3(0.3f, 5, 10), wallColor, roomContainer);
                MakeCube("Ceiling", new Vector3(0, 5.25f, 0), new Vector3(12, 0.3f, 10), wallColor * 0.95f, roomContainer);
            }
            else
            {
                mainCamera.backgroundColor = new Color(0.55f, 0.80f, 0.95f);
            }
            BuildFurniture(index);
            // Fix #12: Bake NavMesh after room geometry is built
            BakeNavMesh();
            // Apply PBR textures to room surfaces
            StartCoroutine(LoadAndApplyRoomTextures(index));
            var rlo = new GameObject("RoomLight");
            rlo.transform.SetParent(roomContainer);
            rlo.transform.position = new Vector3(0, 4, 0);
            var pl = rlo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = floorColor;
            pl.intensity = 0.5f;
            pl.range = 15f;
            if (roomNameText != null) roomNameText.text = roomName;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.85f, 0.88f, 0.92f), floorColor, 0.3f);
        }

        private void BuildFurniture(int ri)
        {
            var p = roomContainer;
            switch (ri)
            {
                case 0:
                    // Fix #9: Furniture scales increased for proper room proportions
                    MakeCube("Bed", new Vector3(-2, 0.5f, 2), new Vector3(3f, 0.8f, 4f), new Color(0.9f, 0.6f, 0.7f), p);
                    MakeCube("Pillow", new Vector3(-2, 1.1f, 3.2f), new Vector3(1.5f, 0.4f, 0.6f), Color.white, p);
                    MakeCube("Wardrobe", new Vector3(4, 1.5f, 4), new Vector3(2f, 3f, 1f), new Color(0.8f, 0.6f, 0.4f), p);
                    MakeCube("Nightstand", new Vector3(-4, 0.4f, 3), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Rug", new Vector3(0, 0.01f, 0), new Vector3(4f, 0.02f, 3f), new Color(1f, 0.8f, 0.9f), p);
                    MakeCube("ToyBox", new Vector3(3, 0.4f, -2), new Vector3(1.5f, 0.8f, 1f), new Color(0.9f, 0.3f, 0.5f), p);
                    MakeSphere("Lamp", new Vector3(-4, 1.2f, 3), 0.25f, new Color(1f, 1f, 0.7f), p);
                    break;
                case 1:
                    MakeCube("Counter", new Vector3(0, 0.5f, 4), new Vector3(8f, 1f, 1.5f), new Color(0.9f, 0.9f, 0.85f), p);
                    MakeCube("Stove", new Vector3(-2, 1.2f, 4.2f), new Vector3(1.5f, 0.3f, 1f), new Color(0.3f, 0.3f, 0.3f), p);
                    MakeCube("Fridge", new Vector3(4, 1.5f, 4), new Vector3(1.5f, 3f, 1.2f), new Color(0.95f, 0.95f, 0.95f), p);
                    MakeCube("Table", new Vector3(0, 0.5f, -1), new Vector3(3f, 0.1f, 2f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Chair1", new Vector3(-1, 0.3f, -2.5f), new Vector3(0.6f, 0.6f, 0.6f), new Color(0.9f, 0.5f, 0.3f), p);
                    MakeCube("Chair2", new Vector3(1, 0.3f, -2.5f), new Vector3(0.6f, 0.6f, 0.6f), new Color(0.3f, 0.7f, 0.9f), p);
                    break;
                case 2:
                    MakeCube("Bathtub", new Vector3(-2, 0.5f, 3), new Vector3(3f, 1f, 2f), Color.white, p);
                    MakeCube("Water", new Vector3(-2, 0.9f, 3), new Vector3(2.6f, 0.3f, 1.6f), new Color(0.6f, 0.85f, 1f), p);
                    MakeCube("Sink", new Vector3(3, 0.7f, 4), new Vector3(1.5f, 0.2f, 1f), Color.white, p);
                    MakeCube("Mirror", new Vector3(3, 2.5f, 4.8f), new Vector3(1.5f, 2f, 0.1f), new Color(0.8f, 0.9f, 1f), p);
                    MakeCube("Toilet", new Vector3(-4, 0.4f, 4), new Vector3(0.8f, 0.8f, 1f), Color.white, p);
                    MakeCube("BathMat", new Vector3(-2, 0.01f, 1), new Vector3(3f, 0.02f, 2f), new Color(0.5f, 0.8f, 1f), p);
                    break;
                case 3:
                    MakeTree(new Vector3(-4, 0, 3), p);
                    MakeTree(new Vector3(4, 0, 4), p);
                    MakeTree(new Vector3(-3, 0, -3), p);
                    MakeCube("Bench", new Vector3(2, 0.4f, 0), new Vector3(2f, 0.2f, 0.8f), new Color(0.65f, 0.45f, 0.25f), p);
                    MakeCube("BenchBack", new Vector3(2, 0.8f, 0.3f), new Vector3(2f, 0.6f, 0.1f), new Color(0.65f, 0.45f, 0.25f), p);
                    MakeSphere("Flower1", new Vector3(-1, 0.3f, 2), 0.2f, new Color(1f, 0.3f, 0.5f), p);
                    MakeSphere("Flower2", new Vector3(0, 0.3f, 2.5f), 0.2f, new Color(1f, 0.8f, 0.2f), p);
                    MakeSphere("Flower3", new Vector3(1, 0.3f, 1.8f), 0.2f, new Color(0.8f, 0.3f, 1f), p);
                    MakeCube("Pond", new Vector3(0, 0.01f, -2), new Vector3(3f, 0.02f, 2f), new Color(0.3f, 0.6f, 0.9f), p);
                    break;
                case 4:
                    MakeCube("Desk1", new Vector3(-2, 0.4f, 0), new Vector3(1.5f, 0.1f, 1f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Desk2", new Vector3(2, 0.4f, 0), new Vector3(1.5f, 0.1f, 1f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Desk3", new Vector3(-2, 0.4f, 2.5f), new Vector3(1.5f, 0.1f, 1f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Desk4", new Vector3(2, 0.4f, 2.5f), new Vector3(1.5f, 0.1f, 1f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Blackboard", new Vector3(0, 2.5f, 4.8f), new Vector3(6f, 2.5f, 0.1f), new Color(0.1f, 0.3f, 0.15f), p);
                    MakeCube("TeacherDesk", new Vector3(0, 0.5f, 3.5f), new Vector3(3f, 0.1f, 1.2f), new Color(0.7f, 0.5f, 0.3f), p);
                    break;
                case 5:
                    MakeCube("Machine1", new Vector3(-3, 1f, 3), new Vector3(1.5f, 2f, 1f), new Color(0.9f, 0.2f, 0.3f), p);
                    MakeCube("Machine2", new Vector3(0, 1f, 3), new Vector3(1.5f, 2f, 1f), new Color(0.2f, 0.6f, 0.9f), p);
                    MakeCube("Machine3", new Vector3(3, 1f, 3), new Vector3(1.5f, 2f, 1f), new Color(0.2f, 0.9f, 0.3f), p);
                    MakeCube("PrizeCounter", new Vector3(0, 0.5f, -3), new Vector3(6f, 1f, 1f), new Color(0.9f, 0.75f, 0.3f), p);
                    MakeCube("Neon1", new Vector3(-4, 0.01f, 0), new Vector3(0.2f, 0.02f, 8f), new Color(1f, 0f, 1f), p);
                    MakeCube("Neon2", new Vector3(4, 0.01f, 0), new Vector3(0.2f, 0.02f, 8f), new Color(0f, 1f, 1f), p);
                    break;
                case 6:
                    MakeCube("Easel", new Vector3(-3, 1f, 2), new Vector3(0.1f, 2f, 1.5f), new Color(0.7f, 0.5f, 0.3f), p);
                    MakeCube("ArtCanvas", new Vector3(-3.1f, 1.5f, 2), new Vector3(0.05f, 1.5f, 1.2f), Color.white, p);
                    MakeCube("DanceFloor", new Vector3(2, 0.01f, 0), new Vector3(5f, 0.02f, 5f), new Color(0.9f, 0.8f, 0.6f), p);
                    MakeSphere("DiscoBall", new Vector3(2, 4.5f, 0), 0.4f, new Color(0.9f, 0.9f, 1f), p);
                    MakeCube("Speaker1", new Vector3(-0.5f, 0.5f, 3), new Vector3(0.8f, 1f, 0.8f), new Color(0.2f, 0.2f, 0.2f), p);
                    MakeCube("Speaker2", new Vector3(4.5f, 0.5f, 3), new Vector3(0.8f, 1f, 0.8f), new Color(0.2f, 0.2f, 0.2f), p);
                    break;
                case 7:
                    MakeCube("Shelf1", new Vector3(-4, 1.5f, 3), new Vector3(1f, 3f, 4f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("Shelf2", new Vector3(4, 1.5f, 3), new Vector3(1f, 3f, 4f), new Color(0.85f, 0.65f, 0.45f), p);
                    MakeCube("ShopCounter", new Vector3(0, 0.5f, -2), new Vector3(5f, 1f, 1.5f), new Color(0.9f, 0.8f, 0.6f), p);
                    MakeCube("Register", new Vector3(0, 1.2f, -2), new Vector3(0.6f, 0.4f, 0.5f), new Color(0.7f, 0.7f, 0.7f), p);
                    MakeSphere("Item1", new Vector3(-4, 2.5f, 2), 0.3f, new Color(1f, 0.3f, 0.5f), p);
                    MakeSphere("Item2", new Vector3(-4, 2.5f, 3), 0.3f, new Color(0.3f, 0.8f, 1f), p);
                    MakeSphere("Item3", new Vector3(4, 2.5f, 2), 0.3f, new Color(0.9f, 0.9f, 0.2f), p);
                    MakeSphere("Item4", new Vector3(4, 2.5f, 3), 0.3f, new Color(0.5f, 1f, 0.5f), p);
                    break;
                case 8:
                    MakeTree(new Vector3(-4, 0, 3), p);
                    MakeTree(new Vector3(4, 0, 4), p);
                    MakeCube("GardenBed1", new Vector3(-2, 0.2f, 1), new Vector3(2f, 0.4f, 2f), new Color(0.5f, 0.3f, 0.15f), p);
                    MakeCube("GardenBed2", new Vector3(2, 0.2f, 1), new Vector3(2f, 0.4f, 2f), new Color(0.5f, 0.3f, 0.15f), p);
                    MakeSphere("Plant1", new Vector3(-2, 0.6f, 1), 0.3f, new Color(0.2f, 0.8f, 0.2f), p);
                    MakeSphere("Plant2", new Vector3(2, 0.6f, 1), 0.3f, new Color(0.3f, 0.9f, 0.3f), p);
                    MakeCube("WateringCan", new Vector3(0, 0.3f, -2), new Vector3(0.5f, 0.5f, 0.3f), new Color(0.3f, 0.5f, 0.8f), p);
                    MakeCube("FenceRail", new Vector3(0, 0.6f, 4.8f), new Vector3(10f, 0.1f, 0.1f), new Color(0.8f, 0.7f, 0.5f), p);
                    break;
            }
        }

        private void MakeTree(Vector3 pos, Transform parent)
        {
            MakeCube("Trunk", pos + new Vector3(0, 1f, 0), new Vector3(0.4f, 2f, 0.4f), new Color(0.55f, 0.35f, 0.15f), parent);
            MakeSphere("Canopy", pos + new Vector3(0, 2.8f, 0), 1.2f, new Color(0.3f, 0.75f, 0.25f), parent);
        }

        private void CreateCharacters()
        {
            characterContainer = new GameObject("Characters").transform;
            emersynObj = MakeCharacter("Emersyn", Vector3.zero, CharBodyColors[0], 1f, true);
            emersynObj.transform.SetParent(characterContainer);
            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.Target = emersynObj.transform;
            for (int i = 1; i < CharacterNames.Length; i++)
            {
                var f = MakeCharacter(CharacterNames[i], new Vector3(2f + (i - 1) * 2.5f, 0, Random.Range(-1f, 1f)), CharBodyColors[i], 0.85f, false);
                f.transform.SetParent(characterContainer);
            }
            for (int i = 0; i < PetNames.Length; i++)
            {
                var pet = MakePet(PetNames[i], new Vector3(-1.5f - i * 1.5f, 0, Random.Range(-0.5f, 0.5f)), PetColors[i]);
                pet.transform.SetParent(characterContainer);
            }
        }

        private GameObject MakeCharacter(string cName, Vector3 pos, Color bodyColor, float scale, bool isMain)
        {
            var root = new GameObject(cName);
            root.transform.position = pos;
            // Fix #11: Characters scaled up 30% to be visible relative to rooms
            root.transform.localScale = Vector3.one * scale * 1.3f;
            // Emersyn's actual skin color: brown skin (age 6)
            Color skin = isMain ? new Color(0.55f, 0.38f, 0.28f) : new Color(1f, 0.88f, 0.78f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.5f);
            body.GetComponent<Renderer>().material = ColorMat(bodyColor);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            head.transform.localScale = new Vector3(0.7f, 0.7f, 0.65f);
            head.GetComponent<Renderer>().material = ColorMat(skin);

            SmallSphere("LEye", head.transform, new Vector3(-0.2f, 0.1f, -0.4f), new Vector3(0.2f, 0.25f, 0.15f), new Color(0.15f, 0.1f, 0.05f));
            SmallSphere("REye", head.transform, new Vector3(0.2f, 0.1f, -0.4f), new Vector3(0.2f, 0.25f, 0.15f), new Color(0.15f, 0.1f, 0.05f));
            SmallSphere("Mouth", head.transform, new Vector3(0, -0.15f, -0.42f), new Vector3(0.2f, 0.08f, 0.1f), new Color(0.9f, 0.4f, 0.4f));

            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0, 0.35f, 0);
            hair.transform.localScale = new Vector3(1.15f, 0.5f, 1.1f);
            Color hc = isMain ? new Color(0.35f, 0.2f, 0.1f) : new Color(Random.Range(0.2f, 0.9f), Random.Range(0.15f, 0.5f), Random.Range(0.1f, 0.3f));
            hair.GetComponent<Renderer>().material = ColorMat(hc);

            Limb("LArm", root.transform, new Vector3(-0.45f, 1.0f, 0), new Vector3(0.15f, 0.4f, 0.15f), skin);
            Limb("RArm", root.transform, new Vector3(0.45f, 1.0f, 0), new Vector3(0.15f, 0.4f, 0.15f), skin);
            Limb("LLeg", root.transform, new Vector3(-0.15f, 0.3f, 0), new Vector3(0.18f, 0.35f, 0.18f), bodyColor * 0.8f);
            Limb("RLeg", root.transform, new Vector3(0.15f, 0.3f, 0), new Vector3(0.18f, 0.35f, 0.18f), bodyColor * 0.8f);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.size = new Vector3(1f, 2.2f, 0.8f);
            root.AddComponent<CharacterBob>();
            return root;
        }

        private void SmallSphere(string name, Transform parent, Vector3 lp, Vector3 ls, Color c)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = lp;
            obj.transform.localScale = ls;
            obj.GetComponent<Renderer>().material = ColorMat(c);
        }

        private void Limb(string name, Transform parent, Vector3 lp, Vector3 ls, Color c)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = lp;
            obj.transform.localScale = ls;
            obj.GetComponent<Renderer>().material = ColorMat(c);
        }

        private GameObject MakePet(string pName, Vector3 pos, Color pc)
        {
            var root = new GameObject(pName);
            root.transform.position = pos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.35f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.6f);
            body.GetComponent<Renderer>().material = ColorMat(pc);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 0.65f, -0.15f);
            head.transform.localScale = new Vector3(0.4f, 0.35f, 0.35f);
            head.GetComponent<Renderer>().material = ColorMat(pc);

            SmallSphere("LEye", head.transform, new Vector3(-0.15f, 0.05f, -0.4f), new Vector3(0.2f, 0.25f, 0.15f), new Color(0.1f, 0.1f, 0.1f));
            SmallSphere("REye", head.transform, new Vector3(0.15f, 0.05f, -0.4f), new Vector3(0.2f, 0.25f, 0.15f), new Color(0.1f, 0.1f, 0.1f));

            if (pName == "Bunny")
            {
                Limb("LEar", head.transform, new Vector3(-0.1f, 0.5f, 0), new Vector3(0.1f, 0.4f, 0.08f), Color.white);
                Limb("REar", head.transform, new Vector3(0.1f, 0.5f, 0), new Vector3(0.1f, 0.4f, 0.08f), Color.white);
            }
            else
            {
                SmallSphere("LEar", head.transform, new Vector3(-0.2f, 0.35f, 0), new Vector3(0.2f, 0.2f, 0.15f), pc * 0.8f);
                SmallSphere("REar", head.transform, new Vector3(0.2f, 0.35f, 0), new Vector3(0.2f, 0.2f, 0.15f), pc * 0.8f);
            }
            SmallSphere("Tail", root.transform, new Vector3(0, 0.35f, 0.35f), new Vector3(0.15f, 0.15f, 0.15f), pc * 0.9f);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.4f, 0);
            col.size = new Vector3(0.6f, 0.8f, 0.8f);
            root.AddComponent<CharacterBob>();
            return root;
        }

        private void CreateUI()
        {
            var co = new GameObject("UICanvas");
            uiCanvas = co.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 10;
            var sc = co.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            co.AddComponent<GraphicRaycaster>();

            // Fix #4: TopBar anchored to TOP of screen (stretch horizontal)
            var topBar = MakePanel("TopBar", co.transform, new Vector2(0, -40), new Vector2(0, 70));
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.anchoredPosition = new Vector2(0, -10);
            topBarRect.sizeDelta = new Vector2(-40, 70); // 20px padding on each side
            SetPanelColor(topBar, new Color(0, 0, 0, 0.4f));
            UIText("CoinLbl", topBar.transform, new Vector2(-420, 0), "Coins:", 22, Color.white);
            coinText = UIText("CoinVal", topBar.transform, new Vector2(-350, 0), "100", 28, new Color(1f, 0.85f, 0.2f));
            UIText("StarLbl", topBar.transform, new Vector2(-210, 0), "Stars:", 22, Color.white);
            starText = UIText("StarVal", topBar.transform, new Vector2(-150, 0), "0", 28, new Color(0.6f, 0.85f, 1f));
            levelText = UIText("Level", topBar.transform, new Vector2(100, 0), "Lv.1", 28, new Color(0.5f, 1f, 0.5f));
            dayText = UIText("Day", topBar.transform, new Vector2(300, 0), "Day 1", 28, Color.white);

            // Fix #4: XP bar anchored below top bar
            var xpBg = MakePanel("XPBg", co.transform, new Vector2(0, -85), new Vector2(0, 16));
            var xpBgRect = xpBg.GetComponent<RectTransform>();
            xpBgRect.anchorMin = new Vector2(0.05f, 1);
            xpBgRect.anchorMax = new Vector2(0.95f, 1);
            xpBgRect.pivot = new Vector2(0.5f, 1);
            xpBgRect.anchoredPosition = new Vector2(0, -85);
            xpBgRect.sizeDelta = new Vector2(0, 16);
            SetPanelColor(xpBg, new Color(0.2f, 0.2f, 0.2f, 0.6f));
            xpBar = MakeSlider("XPBar", xpBg.transform, Vector2.zero, new Vector2(880, 12), new Color(0.3f, 0.9f, 0.3f));

            // Room name and mood text with proper spacing to prevent overlap
            roomNameText = UIText("RoomName", co.transform, new Vector2(0, -115), "Bedroom", 30, Color.white);
            var rnRect = roomNameText.GetComponent<RectTransform>();
            rnRect.anchorMin = new Vector2(0.5f, 1);
            rnRect.anchorMax = new Vector2(0.5f, 1);
            rnRect.pivot = new Vector2(0.5f, 1);
            rnRect.anchoredPosition = new Vector2(0, -115);
            rnRect.sizeDelta = new Vector2(400, 40);
            moodText = UIText("Mood", co.transform, new Vector2(0, -155), "Mood: Happy", 22, new Color(1f, 0.9f, 0.3f));
            var mRect = moodText.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 1);
            mRect.anchorMax = new Vector2(0.5f, 1);
            mRect.pivot = new Vector2(0.5f, 1);
            mRect.anchoredPosition = new Vector2(0, -155);
            mRect.sizeDelta = new Vector2(400, 36);

            CreateNeedBars(co.transform);
            CreateActionButtons(co.transform);
            CreateRoomNavButtons(co.transform);
        }

        private void CreateNeedBars(Transform ct)
        {
            int n = NeedNames.Length;
            needBars = new Slider[n];
            needLabels = new Text[n];
            // Fix #4: Need panel anchored to bottom, stretch horizontal
            var panel = MakePanel("NeedPanel", ct, Vector2.zero, new Vector2(0, 250));
            SetPanelColor(panel, new Color(0, 0, 0, 0.35f));
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0, 0);
            pr.anchorMax = new Vector2(1, 0);
            pr.pivot = new Vector2(0.5f, 0);
            pr.anchoredPosition = new Vector2(0, 140);
            pr.sizeDelta = new Vector2(-40, 250);
            for (int i = 0; i < n; i++)
            {
                float y = 100 - i * 28;
                needLabels[i] = UIText("Lbl_" + NeedNames[i], panel.transform, new Vector2(-400, y), NeedNames[i], 18, Color.white);
                needLabels[i].alignment = TextAnchor.MiddleLeft;
                needLabels[i].GetComponent<RectTransform>().sizeDelta = new Vector2(120, 24);
                needBars[i] = MakeSlider("Bar_" + NeedNames[i], panel.transform, new Vector2(50, y), new Vector2(600, 18), NeedColors[i]);
                needBars[i].value = 0.75f;
            }
        }

        private void CreateActionButtons(Transform ct)
        {
            string[] labels = { "Feed", "Play", "Clean", "Sleep", "Shop", "Dance" };
            Color[] colors = {
                new Color(1f, 0.6f, 0.2f), new Color(1f, 0.3f, 0.6f), new Color(0.3f, 0.7f, 1f),
                new Color(0.6f, 0.4f, 0.9f), new Color(1f, 0.85f, 0.2f), new Color(0.9f, 0.3f, 0.5f)
            };
            // Fix #4: Buttons distributed evenly at bottom
            for (int i = 0; i < labels.Length; i++)
            {
                float normalizedX = (i + 0.5f) / labels.Length; // Evenly spaced 0..1
                var btn = MakeButton(labels[i] + "Btn", ct, Vector2.zero, new Vector2(140, 55), labels[i], colors[i]);
                var r = btn.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(normalizedX, 0);
                r.anchorMax = new Vector2(normalizedX, 0);
                r.pivot = new Vector2(0.5f, 0);
                r.anchoredPosition = new Vector2(0, 70);
                int idx = i;
                btn.GetComponent<Button>().onClick.AddListener(() => OnActionButton(idx));
            }
        }

        private void CreateRoomNavButtons(Transform ct)
        {
            // Fix #13: Larger room nav buttons for touch
            var left = MakeButton("Prev", ct, Vector2.zero, new Vector2(100, 150), "<", new Color(0.3f, 0.3f, 0.3f, 0.6f));
            var lr = left.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 0.5f);
            lr.anchorMax = new Vector2(0, 0.5f);
            lr.anchoredPosition = new Vector2(60, 0); // More padding from edge
            left.GetComponent<Button>().onClick.AddListener(() => ChangeRoom(-1));

            var right = MakeButton("Next", ct, Vector2.zero, new Vector2(100, 150), ">", new Color(0.3f, 0.3f, 0.3f, 0.6f));
            var rr = right.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(1, 0.5f);
            rr.anchorMax = new Vector2(1, 0.5f);
            rr.anchoredPosition = new Vector2(-60, 0); // More padding from edge
            right.GetComponent<Button>().onClick.AddListener(() => ChangeRoom(1));
        }

        private void WireUpSystems()
        {
            var input = InputSystem.InputManager.Instance;
            if (input != null)
            {
                input.OnTap += OnTapHandler;
                input.OnSwipe += OnSwipeHandler;
                input.OnPinchZoom += OnPinchHandler;
                input.OnObjectTapped += OnObjectTappedHandler;
                input.OnDrag += OnDragHandler;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            // Wire enhancement systems
            // Apply toon shading to the initial scene
            if (Visual.ToonShading.Instance != null)
                Visual.ToonShading.Instance.ApplyToonShadingToScene();

            // Set initial room theme for adaptive music
            if (Audio.AdaptiveMusicSystem.Instance != null)
                Audio.AdaptiveMusicSystem.Instance.SetRoomTheme(RoomNames[currentRoomIndex]);

            // Set initial room lighting
            if (Visual.DynamicLighting.Instance != null)
                Visual.DynamicLighting.Instance.SetRoomLighting(currentRoomIndex, currentRoomIndex == 3 || currentRoomIndex == 8);

            // Apply customization colors to Emersyn
            if (Gameplay.CharacterCustomization.Instance != null && emersynObj != null)
                Gameplay.CharacterCustomization.Instance.ApplyToCharacter(emersynObj);

            // Batch static objects for performance
            if (Performance.PerformanceOptimizer.Instance != null)
            {
                Performance.PerformanceOptimizer.Instance.OptimizeTextures();
                Performance.PerformanceOptimizer.Instance.BatchStaticObjects();
            }

            // Register room objects with LOD manager
            if (Performance.LODManager.Instance != null)
            {
                foreach (Transform child in roomContainer)
                    Performance.LODManager.Instance.Register(child.gameObject);
            }

            // Track room visit
            if (Systems.AnalyticsManager.Instance != null)
                Systems.AnalyticsManager.Instance.TrackRoomVisit(RoomNames[currentRoomIndex]);

            // Claim daily reward on first load
            if (Systems.DailyRewardSystem.Instance != null)
                Systems.DailyRewardSystem.Instance.ClaimDailyReward();
        }

        // Fix #29: Cache NeedSystem reference instead of FindFirstObjectByType every frame
        private NeedSystem cachedNeedSystem;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (coinText != null) coinText.text = gm.Coins.ToString();
                if (starText != null) starText.text = gm.Stars.ToString();
                if (levelText != null) levelText.text = "Lv." + gm.Level;
                if (dayText != null) dayText.text = "Day " + gm.CurrentDay;
                if (xpBar != null) xpBar.value = gm.XPToNextLevel > 0 ? (float)gm.XP / gm.XPToNextLevel : 0f;
            }
            // Fix #29: Use cached reference, only re-find if null
            if (cachedNeedSystem == null)
                cachedNeedSystem = FindFirstObjectByType<NeedSystem>();
            var ns = cachedNeedSystem;
            if (ns != null && needBars != null)
            {
                for (int i = 0; i < NeedNames.Length && i < needBars.Length; i++)
                {
                    if (needBars[i] == null) continue;
                    var need = ns.GetNeed(NeedNames[i]);
                    if (need != null) needBars[i].value = need.Normalized;
                }
                if (moodText != null) moodText.text = "Mood: " + ns.CurrentMood;
            }
        }

        private void OnTapHandler(Vector3 wp) { }

        private void OnSwipeHandler(InputSystem.InputManager.SwipeDirection dir)
        {
            if (dir == InputSystem.InputManager.SwipeDirection.Left) ChangeRoom(1);
            else if (dir == InputSystem.InputManager.SwipeDirection.Right) ChangeRoom(-1);
        }

        private void OnPinchHandler(float delta)
        {
            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.Zoom(delta);
        }

        private void OnObjectTappedHandler(GameObject obj)
        {
            var bob = obj.GetComponentInParent<CharacterBob>();
            if (bob != null) bob.TriggerSquash();
            if (RewardSystem.Instance != null)
                RewardSystem.Instance.GrantInteractionReward("poke");

            // Enhancement integrations
            if (Systems.AnalyticsManager.Instance != null)
                Systems.AnalyticsManager.Instance.TrackTap();
            if (Audio.CharacterVoiceSystem.Instance != null)
                Audio.CharacterVoiceSystem.Instance.PlayTouchReaction(obj.name, true);
            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnSparkles(obj.transform.position + Vector3.up);
            if (Systems.TutorialSystem.Instance != null)
                Systems.TutorialSystem.Instance.ReportInteraction("tap");
            if (Systems.AccessibilityManager.Instance != null)
                Systems.AccessibilityManager.Instance.TriggerHaptic();
        }

        private void OnDragHandler(Vector2 pos, Vector2 delta)
        {
            if (CameraSystem.CameraController.Instance != null)
            {
                CameraSystem.CameraController.Instance.OrbitHorizontal(delta.x * 0.01f);
                CameraSystem.CameraController.Instance.OrbitVertical(-delta.y * 0.01f);
            }
        }

        private void OnActionButton(int idx)
        {
            // Fix #29: Use cached NeedSystem reference
            if (cachedNeedSystem == null)
                cachedNeedSystem = FindFirstObjectByType<NeedSystem>();
            var ns = cachedNeedSystem;
            if (ns == null) return;
            string activityName = "";
            switch (idx)
            {
                case 0: ns.SatisfyNeed("Hunger", 30f); activityName = "eat"; break;
                case 1: ns.SatisfyNeed("Fun", 25f); ns.SatisfyNeed("Social", 15f); activityName = "dance"; break;
                case 2: ns.SatisfyNeed("Hygiene", 35f); activityName = "bathe"; break;
                case 3: ns.SatisfyNeed("Energy", 40f); ns.SatisfyNeed("Comfort", 20f); activityName = "sleep"; break;
                case 4: activityName = "shop"; break;
                case 5: ns.SatisfyNeed("Fun", 20f); ns.SatisfyNeed("Creativity", 15f); activityName = "draw"; break;
            }
            if (RewardSystem.Instance != null)
                RewardSystem.Instance.GrantInteractionReward("action");

            // Trigger activity animation
            if (Animation.ActivityAnimations.Instance != null && !string.IsNullOrEmpty(activityName))
                Animation.ActivityAnimations.Instance.StartActivity(activityName, emersynObj != null ? emersynObj.transform : null);

            // Quest integration
            if (Gameplay.QuestSystem.Instance != null)
            {
                if (activityName == "eat") Gameplay.QuestSystem.Instance.ReportProgress("feed");
                if (activityName == "bathe") Gameplay.QuestSystem.Instance.ReportProgress("clean");
                if (activityName == "dance") Gameplay.QuestSystem.Instance.ReportProgress("create");
            }

            // Tutorial integration
            if (Systems.TutorialSystem.Instance != null)
                Systems.TutorialSystem.Instance.ReportInteraction(activityName);
        }

        private void ChangeRoom(int dir)
        {
            int next = (currentRoomIndex + dir + RoomNames.Length) % RoomNames.Length;

            // Check room progression unlock
            if (Systems.RoomProgressionSystem.Instance != null &&
                !Systems.RoomProgressionSystem.Instance.IsRoomUnlocked(RoomNames[next]))
            {
                // Show unlock prompt or skip to next unlocked room
                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("error");
                return;
            }

            // Performance: GC during transition
            if (Performance.PerformanceOptimizer.Instance != null)
                Performance.PerformanceOptimizer.Instance.SafeGarbageCollect();

            BuildRoom(next);

            // Update enhancement systems on room change
            if (Audio.AdaptiveMusicSystem.Instance != null)
                Audio.AdaptiveMusicSystem.Instance.SetRoomTheme(RoomNames[next]);
            if (Visual.DynamicLighting.Instance != null)
                Visual.DynamicLighting.Instance.SetRoomLighting(next, next == 3 || next == 8);
            if (Visual.ToonShading.Instance != null)
                Visual.ToonShading.Instance.ApplyToonShadingToScene();
            if (Systems.AnalyticsManager.Instance != null)
                Systems.AnalyticsManager.Instance.TrackRoomVisit(RoomNames[next]);
            if (Gameplay.QuestSystem.Instance != null)
                Gameplay.QuestSystem.Instance.ReportProgress("visit_room");
            if (Systems.TutorialSystem.Instance != null)
                Systems.TutorialSystem.Instance.ReportInteraction("change_room");

            // Register new room objects with LOD
            if (Performance.LODManager.Instance != null)
                foreach (Transform child in roomContainer)
                    Performance.LODManager.Instance.Register(child.gameObject);

            // Interstitial ad between rooms (respects cooldown)
            if (Systems.AdIntegration.Instance != null)
                Systems.AdIntegration.Instance.ShowInterstitial();
        }

        // === GLB Model Loading via glTFast ===
        private IEnumerator LoadGLBCharactersCoroutine()
        {
            Debug.Log("[SceneBuilder] Starting GLB character loading...");
            // Load main characters from GLB
            for (int i = 0; i < GLBCharacterFiles.Length && i < CharacterNames.Length; i++)
            {
                string glbName = GLBCharacterFiles[i];
                string charName = CharacterNames[i];
                yield return StartCoroutine(LoadSingleGLBCharacter(glbName, charName, i == 0));
            }
            // Load pets from GLB
            for (int i = 0; i < GLBPetFiles.Length && i < PetNames.Length; i++)
            {
                string glbName = GLBPetFiles[i];
                string petName = PetNames[i];
                yield return StartCoroutine(LoadSingleGLBPet(glbName, petName));
            }
            glbLoadingComplete = true;
            Debug.Log("[SceneBuilder] GLB character loading complete!");
        }

        private IEnumerator LoadSingleGLBCharacter(string glbFileName, string charName, bool isMain)
        {
            string uri = Path.Combine(Application.streamingAssetsPath, "Characters", glbFileName + ".glb");
            Debug.Log($"[SceneBuilder] Loading GLB: {uri}");

            var gltfImport = new GltfImport();
            var loadTask = gltfImport.Load(uri);

            // Wait for async load to complete
            while (!loadTask.IsCompleted)
                yield return null;

            if (!loadTask.Result)
            {
                Debug.LogWarning($"[SceneBuilder] Failed to load GLB for {charName}, keeping primitive fallback");
                yield break;
            }

            // Find and replace the primitive character
            Transform existing = characterContainer != null ? characterContainer.Find(charName) : null;
            if (existing == null)
            {
                Debug.LogWarning($"[SceneBuilder] Could not find primitive {charName} to replace");
                yield break;
            }

            Vector3 savedPos = existing.position;
            Quaternion savedRot = existing.rotation;
            Vector3 savedScale = existing.localScale;
            Transform savedParent = existing.parent;

            // Create new GLB object
            var glbObj = new GameObject(charName);
            glbObj.transform.SetParent(savedParent, false);
            glbObj.transform.position = savedPos;
            glbObj.transform.rotation = savedRot;
            glbObj.transform.localScale = savedScale;

            var instantiateTask = gltfImport.InstantiateMainSceneAsync(glbObj.transform);
            while (!instantiateTask.IsCompleted)
                yield return null;

            // Convert glTF materials to Standard shader for Built-in Pipeline
            ConvertGLTFMaterials(glbObj);

            // Add interaction components
            var col = glbObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.size = new Vector3(1f, 2.2f, 0.8f);
            glbObj.AddComponent<CharacterBob>();

            // Update emersynObj reference if this is the main character
            if (isMain)
            {
                emersynObj = glbObj;
                if (CameraSystem.CameraController.Instance != null)
                    CameraSystem.CameraController.Instance.Target = glbObj.transform;
                if (Gameplay.CharacterCustomization.Instance != null)
                    Gameplay.CharacterCustomization.Instance.ApplyToCharacter(glbObj);
            }

            // Destroy primitive version
            Destroy(existing.gameObject);
            Debug.Log($"[SceneBuilder] Replaced {charName} with GLB model!");
        }

        private IEnumerator LoadSingleGLBPet(string glbFileName, string petName)
        {
            string uri = Path.Combine(Application.streamingAssetsPath, "Characters", glbFileName + ".glb");
            Debug.Log($"[SceneBuilder] Loading pet GLB: {uri}");

            var gltfImport = new GltfImport();
            var loadTask = gltfImport.Load(uri);
            while (!loadTask.IsCompleted)
                yield return null;

            if (!loadTask.Result)
            {
                Debug.LogWarning($"[SceneBuilder] Failed to load GLB for pet {petName}, keeping primitive");
                yield break;
            }

            Transform existing = characterContainer != null ? characterContainer.Find(petName) : null;
            if (existing == null) yield break;

            Vector3 savedPos = existing.position;
            Transform savedParent = existing.parent;

            var glbObj = new GameObject(petName);
            glbObj.transform.SetParent(savedParent, false);
            glbObj.transform.position = savedPos;
            glbObj.transform.localScale = Vector3.one * 0.5f;

            var instantiateTask = gltfImport.InstantiateMainSceneAsync(glbObj.transform);
            while (!instantiateTask.IsCompleted)
                yield return null;

            ConvertGLTFMaterials(glbObj);
            var col = glbObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.4f, 0);
            col.size = new Vector3(0.6f, 0.8f, 0.8f);
            glbObj.AddComponent<CharacterBob>();

            Destroy(existing.gameObject);
            Debug.Log($"[SceneBuilder] Replaced pet {petName} with GLB model!");
        }

        private void ConvertGLTFMaterials(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) return;

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null) continue;
                    // Preserve textures from glTF material
                    Texture mainTex = null;
                    Texture normalTex = null;
                    Color baseColor = Color.white;

                    // Try glTF property names
                    if (mat.HasProperty("_BaseColorTexture")) mainTex = mat.GetTexture("_BaseColorTexture");
                    if (mainTex == null && mat.HasProperty("baseColorTexture")) mainTex = mat.GetTexture("baseColorTexture");
                    if (mainTex == null && mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                    if (mat.HasProperty("_NormalTexture")) normalTex = mat.GetTexture("_NormalTexture");
                    if (normalTex == null && mat.HasProperty("_BumpMap")) normalTex = mat.GetTexture("_BumpMap");
                    if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
                    else if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");

                    // Switch to Standard shader
                    mat.shader = standardShader;
                    mat.color = baseColor;
                    if (mainTex != null) mat.SetTexture("_MainTex", mainTex);
                    if (normalTex != null)
                    {
                        mat.SetTexture("_BumpMap", normalTex);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    mat.SetFloat("_Glossiness", 0.4f);
                    mat.SetFloat("_Metallic", 0.0f);
                    mat.enableInstancing = true;
                }
            }
        }

        // === PBR Texture Loading for Rooms ===
        private IEnumerator LoadAndApplyRoomTextures(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= RoomTexturePaths.Length) yield break;
            var paths = RoomTexturePaths[roomIndex];
            if (paths == null || paths.Length < 4) yield break;

            string floorAlbedoPath = paths[0];
            string wallAlbedoPath = paths[1];
            string floorNormalPath = paths[2];
            string wallNormalPath = paths[3];

            // Load floor textures
            if (!string.IsNullOrEmpty(floorAlbedoPath))
            {
                Texture2D floorAlbedo = null;
                Texture2D floorNormal = null;
                yield return StartCoroutine(LoadTextureFromStreamingAssets(floorAlbedoPath, tex => floorAlbedo = tex));
                if (!string.IsNullOrEmpty(floorNormalPath))
                    yield return StartCoroutine(LoadTextureFromStreamingAssets(floorNormalPath, tex => floorNormal = tex));

                if (floorAlbedo != null)
                {
                    Material floorMat = CreatePBRMaterial(floorAlbedo, floorNormal);
                    ApplyMaterialToChild(roomContainer, "Floor", floorMat);
                }
            }

            // Load wall textures
            if (!string.IsNullOrEmpty(wallAlbedoPath))
            {
                Texture2D wallAlbedo = null;
                Texture2D wallNormal = null;
                yield return StartCoroutine(LoadTextureFromStreamingAssets(wallAlbedoPath, tex => wallAlbedo = tex));
                if (!string.IsNullOrEmpty(wallNormalPath))
                    yield return StartCoroutine(LoadTextureFromStreamingAssets(wallNormalPath, tex => wallNormal = tex));

                if (wallAlbedo != null)
                {
                    Material wallMat = CreatePBRMaterial(wallAlbedo, wallNormal);
                    ApplyMaterialToChild(roomContainer, "BackWall", wallMat);
                    ApplyMaterialToChild(roomContainer, "LeftWall", wallMat);
                    ApplyMaterialToChild(roomContainer, "RightWall", wallMat);
                    ApplyMaterialToChild(roomContainer, "Ceiling", wallMat);
                }
            }

            Debug.Log($"[SceneBuilder] PBR textures applied to room {RoomNames[roomIndex]}");
        }

        private IEnumerator LoadTextureFromStreamingAssets(string relativePath, System.Action<Texture2D> callback)
        {
            if (textureCache.ContainsKey(relativePath))
            {
                callback(textureCache[relativePath]);
                yield break;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, "Textures", relativePath);
            using (var request = UnityWebRequestTexture.GetTexture(fullPath))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(request);
                    tex.filterMode = FilterMode.Trilinear;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    textureCache[relativePath] = tex;
                    callback(tex);
                    Debug.Log($"[SceneBuilder] Loaded texture: {relativePath}");
                }
                else
                {
                    Debug.LogWarning($"[SceneBuilder] Failed to load texture: {fullPath} - {request.error}");
                    callback(null);
                }
            }
        }

        private Material CreatePBRMaterial(Texture2D albedo, Texture2D normal)
        {
            Material mat = new Material(baseMat);
            mat.SetTexture("_MainTex", albedo);
            mat.color = Color.white; // Don't tint, let texture show through
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 1.0f);
            }
            mat.SetFloat("_Glossiness", 0.3f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.enableInstancing = true;
            // Set texture tiling for room scale
            mat.SetTextureScale("_MainTex", new Vector2(3f, 3f));
            if (normal != null)
                mat.SetTextureScale("_BumpMap", new Vector2(3f, 3f));
            return mat;
        }

        private void ApplyMaterialToChild(Transform parent, string childName, Material mat)
        {
            if (parent == null) return;
            Transform child = parent.Find(childName);
            if (child != null)
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = mat;
            }
        }

        // Fix #12: NavMesh baking for character pathfinding
        // Note: NavMeshSurface requires AI Navigation package — using simple collider-based movement instead
        private void BakeNavMesh()
        {
            // Ensure floor has a collider for raycasting-based movement
            var floor = roomContainer.Find("Floor");
            if (floor != null)
            {
                var col = floor.GetComponent<Collider>();
                if (col == null)
                    floor.gameObject.AddComponent<BoxCollider>();
            }
        }

        private GameObject MakeCube(string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = ColorMat(color);
            return obj;
        }

        private GameObject MakeSphere(string name, Vector3 pos, float radius, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * radius * 2f;
            obj.GetComponent<Renderer>().material = ColorMat(color);
            return obj;
        }

        private GameObject MakePanel(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var r = obj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            obj.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            return obj;
        }

        private void SetPanelColor(GameObject panel, Color c)
        {
            var img = panel.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        private Text UIText(string name, Transform parent, Vector2 pos, string text, int fontSize, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var r = obj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(200, 40);
            var t = obj.AddComponent<Text>();
            t.text = text;
            // Fix #6b: Scale text up 20% for portrait readability
            t.fontSize = Mathf.CeilToInt(fontSize * 1.2f);
            t.color = color;
            // Fix text garbling: LegacyRuntime.ttf causes garbled text on Android IL2CPP
            // Use OS font directly which is always available on Android
            t.font = Font.CreateDynamicFontFromOSFont("Roboto", fontSize);
            if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("sans-serif", fontSize);
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private Slider MakeSlider(string name, Transform parent, Vector2 pos, Vector2 size, Color fillColor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var r = obj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var slider = obj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.5f;
            slider.interactable = false;

            var bg = new GameObject("Bg");
            bg.transform.SetParent(obj.transform, false);
            var bgR = bg.AddComponent<RectTransform>();
            bgR.anchorMin = Vector2.zero;
            bgR.anchorMax = Vector2.one;
            bgR.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            var area = new GameObject("FillArea");
            area.transform.SetParent(obj.transform, false);
            var aR = area.AddComponent<RectTransform>();
            aR.anchorMin = Vector2.zero;
            aR.anchorMax = Vector2.one;
            aR.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(area.transform, false);
            var fR = fill.AddComponent<RectTransform>();
            fR.anchorMin = Vector2.zero;
            fR.anchorMax = new Vector2(0, 1);
            fR.sizeDelta = Vector2.zero;
            fill.AddComponent<Image>().color = fillColor;
            slider.fillRect = fR;
            return slider;
        }

        private GameObject MakeButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var r = obj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            obj.AddComponent<Image>().color = color;
            obj.AddComponent<Button>();
            var lbl = new GameObject("Label");
            lbl.transform.SetParent(obj.transform, false);
            var lr2 = lbl.AddComponent<RectTransform>();
            lr2.anchorMin = Vector2.zero;
            lr2.anchorMax = Vector2.one;
            lr2.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.text = label;
            t.fontSize = 22;
            t.color = Color.white;
            // Fix text garbling: use OS font on Android
            t.font = Font.CreateDynamicFontFromOSFont("Roboto", 22);
            if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("sans-serif", 22);
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.MiddleCenter;
            return obj;
        }
    }

    public class CharacterBob : MonoBehaviour
    {
        private Vector3 startPos;
        private float speed, amount, phase;
        private bool squashing;
        private float squashT;
        private Vector3 origScale;

        private void Start()
        {
            startPos = transform.position;
            origScale = transform.localScale;
            speed = Random.Range(1.5f, 2.5f);
            amount = Random.Range(0.03f, 0.08f);
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float y = startPos.y + Mathf.Sin(Time.time * speed + phase) * amount;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            if (squashing)
            {
                squashT += Time.deltaTime * 8f;
                if (squashT < 1f)
                {
                    float s = Mathf.Sin(squashT * Mathf.PI);
                    transform.localScale = new Vector3(
                        origScale.x * (1f + 0.15f * s),
                        origScale.y * (1f - 0.15f * s),
                        origScale.z * (1f + 0.15f * s));
                }
                else
                {
                    transform.localScale = origScale;
                    squashing = false;
                }
            }
        }

        public void TriggerSquash()
        {
            squashing = true;
            squashT = 0f;
        }
    }
}
