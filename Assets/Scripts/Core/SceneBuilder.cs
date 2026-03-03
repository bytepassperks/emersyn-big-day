using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GLTFast;
// Round 11 (Claude 4.5 Bedrock): GLTFast RE-ENABLED with proper IL2CPP protection.
// Root cause of original blue screen was IL2CPP code stripping, NOT GLTFast itself.
// Fix: Comprehensive link.xml preserving glTFast + Unity assemblies + Newtonsoft.Json.
// This enables full 3D GLB model loading for AAA-quality character rendering.

namespace EmersynBigDay.Core
{
    public class SceneBuilder : MonoBehaviour
    {
        // Claude Bedrock Round 5: Native Android logging to bypass IL2CPP Debug.Log suppression
        #if UNITY_ANDROID && !UNITY_EDITOR
        private static void AndroidLog(string msg)
        {
            try
            {
                using (var logClass = new AndroidJavaClass("android.util.Log"))
                {
                    logClass.CallStatic<int>("d", "SCENEBUILDER", msg);
                }
            }
            catch (System.Exception) { /* fallback silently */ }
        }
        #else
        private static void AndroidLog(string msg) { Debug.Log(msg); }
        #endif

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
        private Shader standardShader; // Round 11: Standard shader for PBR rendering
        private Shader unlitTextureShader; // Fallback: Unlit/Texture for surfaces where Standard fails
        private Texture2D whitePixelTex;
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private Dictionary<int, Texture2D> colorTexCache = new Dictionary<int, Texture2D>();
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
            AndroidLog("[SceneBuilder] ===== Awake() CALLED =====");

            // Round 11 (Claude 4.5): Enable proper shadow quality for AAA visuals
            #if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.SetQualityLevel(2, true); // High quality for AAA look
            QualitySettings.antiAliasing = 2; // 2x MSAA for smoother edges
            QualitySettings.shadows = LightShadows.Soft != 0 ? ShadowQuality.All : ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.softParticles = false; // Keep off for mobile perf
            QualitySettings.pixelLightCount = 3; // Support key + fill + rim lights
            AndroidLog("[SceneBuilder] Android quality: High, shadows enabled, 2x MSAA");
            #endif

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

            // Claude Bedrock Round 5: Log graphics info via native Android logging
            #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidLog($"[SceneBuilder] Graphics API: {SystemInfo.graphicsDeviceType}");
            AndroidLog($"[SceneBuilder] Graphics device: {SystemInfo.graphicsDeviceName}");
            AndroidLog($"[SceneBuilder] Color space: {QualitySettings.activeColorSpace}");
            AndroidLog($"[SceneBuilder] Screen: {Screen.width}x{Screen.height}");
            #endif

            // Claude Bedrock Round 5: Warm up ALL shaders before creating any geometry
            AndroidLog("[SceneBuilder] Warming up shaders...");
            Shader.WarmupAllShaders();
            AndroidLog("[SceneBuilder] Shader warmup complete");

            AndroidLog("[SceneBuilder] Starting programmatic scene construction...");

            // Round 11 (Claude 4.5 Bedrock): Use Standard shader for full PBR rendering.
            // The original blue screen was caused by IL2CPP stripping, NOT by Standard shader.
            // With proper link.xml, Standard shader + GLTFast work correctly on Android.
            standardShader = Shader.Find("Standard");
            AndroidLog($"[SceneBuilder] Standard shader: {(standardShader != null ? "FOUND" : "NULL")}");
            unlitTextureShader = Shader.Find("Unlit/Texture");
            AndroidLog($"[SceneBuilder] Unlit/Texture shader: {(unlitTextureShader != null ? "FOUND" : "NULL")}");

            // Keep white pixel texture as fallback
            whitePixelTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] whitePixels = new Color[4];
            for (int i = 0; i < 4; i++) whitePixels[i] = Color.white;
            whitePixelTex.SetPixels(whitePixels);
            whitePixelTex.Apply();
            AndroidLog("[SceneBuilder] White pixel texture created");

            // CRITICAL: Each phase is independently try-caught so a failure in one
            // (e.g. a manager system) does NOT prevent room geometry, characters, or UI from rendering.

            try { baseMat = CreateBaseMaterial(); AndroidLog("[SceneBuilder] BaseMat created OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] BaseMat FAILED: {e.Message}"); baseMat = CreateEmergencyMaterial(); }

            try { CreateCamera(); AndroidLog("[SceneBuilder] Camera created OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] Camera FAILED: {e.Message}"); }

            try { CreateManagers(); AndroidLog("[SceneBuilder] Managers created OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] Managers FAILED (non-fatal): {e.Message}"); }

            try { SetupLighting(); AndroidLog("[SceneBuilder] Lighting setup OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] Lighting FAILED: {e.Message}"); }

            try { CreateRoomContainer(); BuildRoom(0); AndroidLog("[SceneBuilder] Room built OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] Room FAILED: {e.Message}"); }

            try { CreateCharacters(); AndroidLog("[SceneBuilder] Characters created OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] Characters FAILED: {e.Message}"); }

            try { CreateUI(); AndroidLog("[SceneBuilder] UI created OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] UI FAILED: {e.Message}"); }

            try { WireUpSystems(); AndroidLog("[SceneBuilder] Systems wired OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] WireUp FAILED: {e.Message}"); }

            // Claude Bedrock fix: Validate geometry was actually created
            ValidateGeometry();

            AndroidLog("[SceneBuilder] Scene fully initialized!");
            // Start async loading of GLB models and PBR textures
            StartCoroutine(SafeCoroutine(LoadGLBCharactersCoroutine()));
            StartCoroutine(SafeCoroutine(LoadAndApplyRoomTextures(currentRoomIndex)));

            // Claude Bedrock Priority 3: Delayed diagnostic for Android rendering debug
            StartCoroutine(DiagnoseRendering());
        }

        /// <summary>
        /// Claude Bedrock Priority 3: Delayed rendering diagnostic to identify blue screen cause.
        /// Runs 2 seconds after scene init to capture state after all systems are up.
        /// </summary>
        private IEnumerator DiagnoseRendering()
        {
            yield return new WaitForSeconds(2f);

            AndroidLog("=== ANDROID RENDER DIAGNOSTIC ===");
            AndroidLog($"Cameras: {FindObjectsByType<Camera>(FindObjectsSortMode.None).Length}");
            AndroidLog($"Lights: {FindObjectsByType<Light>(FindObjectsSortMode.None).Length}");
            AndroidLog($"Renderers: {FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length}");
            AndroidLog($"Canvas objects: {FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length}");
            AndroidLog($"Graphics API: {SystemInfo.graphicsDeviceType}");
            AndroidLog($"Render pipeline asset: {UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset}");

            Camera cam = Camera.main;
            if (cam != null)
            {
                AndroidLog($"Camera position: {cam.transform.position}");
                AndroidLog($"Camera rotation: {cam.transform.rotation.eulerAngles}");
                AndroidLog($"Camera culling mask: {cam.cullingMask}");
                AndroidLog($"Camera clear flags: {cam.clearFlags}");
                AndroidLog($"Camera background: {cam.backgroundColor}");
                AndroidLog($"Camera rendering path: {cam.renderingPath}");
                AndroidLog($"Camera HDR: {cam.allowHDR}, MSAA: {cam.allowMSAA}");
            }
            else
            {
                AndroidLog("NO MAIN CAMERA FOUND after scene init!");
            }

            // Claude Bedrock Round 5: Log all renderer shader states
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r.material != null && r.material.shader != null)
                    AndroidLog($"Renderer: {r.name} shader={r.material.shader.name} visible={r.isVisible}");
                else
                    AndroidLog($"Renderer: {r.name} has NULL material or shader!");
            }
            AndroidLog("=== END DIAGNOSTIC ===");
        }

        /// <summary>
        /// Claude Bedrock fix: Debug validation to confirm geometry, cameras, and UI exist after init.
        /// </summary>
        private void ValidateGeometry()
        {
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            AndroidLog($"[GEOMETRY CHECK] MeshRenderers: {renderers.Length}");
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            AndroidLog($"[CAMERA CHECK] Cameras: {cameras.Length}");
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            AndroidLog($"[UI CHECK] Canvases: {canvases.Length}");
            int fixedCount = 0;
            foreach (var r in renderers)
            {
                if (r.material != null && r.material.shader != null)
                {
                    if (r.material.shader.name == "Hidden/InternalErrorShader")
                    {
                        AndroidLog($"[SHADER ERROR] {r.name} has broken shader! Fixing...");
                        // Claude Bedrock Round 5: Use primitive material as fallback instead of Shader.Find
                        var tempPrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        r.material = new Material(tempPrim.GetComponent<Renderer>().sharedMaterial);
                        r.material.color = Color.magenta; // Bright color to show fixed shaders
                        DestroyImmediate(tempPrim);
                        fixedCount++;
                    }
                }
                else if (r.material == null)
                {
                    AndroidLog($"[MATERIAL NULL] {r.name} has no material! Creating one...");
                    r.material = CreateEmergencyMaterial();
                    fixedCount++;
                }
            }
            AndroidLog($"[GEOMETRY VALIDATE] Fixed {fixedCount} broken materials");
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

        // Claude Bedrock Round 5: Emergency material when all else fails
        private Material CreateEmergencyMaterial()
        {
            AndroidLog("[SceneBuilder] Creating EMERGENCY material from primitive");
            var refPrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var emergencyMat = new Material(refPrim.GetComponent<Renderer>().sharedMaterial);
            DestroyImmediate(refPrim);
            emergencyMat.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            return emergencyMat;
        }

        private Material CreateBaseMaterial()
        {
            // Round 11 (Claude 4.5): Use Standard shader as PRIMARY for full PBR.
            // The original blue screen was caused by IL2CPP stripping, NOT Standard shader.
            // With proper link.xml, Standard shader works on all Android GPUs.
            Shader shader = standardShader;
            if (shader == null)
            {
                // Fallback chain if Standard not yet cached
                string[] shaderNames = { "Standard", "Legacy Shaders/Diffuse", "Mobile/Diffuse", "Sprites/Default" };
                foreach (string shaderName in shaderNames)
                {
                    shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        AndroidLog($"[SceneBuilder] Found shader: {shaderName}");
                        break;
                    }
                    AndroidLog($"[SceneBuilder] Shader NOT found: {shaderName}");
                }
            }
            if (shader == null)
            {
                AndroidLog("[SceneBuilder] All shader lookups failed! Using primitive fallback.");
                return CreateEmergencyMaterial();
            }
            var mat = new Material(shader);
            mat.renderQueue = 2000;
            if (shader.name.Contains("Standard"))
            {
                mat.SetFloat("_Mode", 0f); // Opaque
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.SetFloat("_Glossiness", 0.3f);
                mat.SetFloat("_Metallic", 0.0f);
            }
            AndroidLog($"[SceneBuilder] Base material shader: {mat.shader?.name ?? "NULL"}");
            return mat;
        }

        // Round 10: Create or retrieve a cached 2x2 texture filled with the given color
        private Texture2D GetColorTexture(Color c)
        {
            // Use color hash as key (avoids floating point comparison issues)
            int key = ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | (int)(c.a * 255);
            if (colorTexCache.TryGetValue(key, out Texture2D tex) && tex != null)
                return tex;
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[4];
            for (int i = 0; i < 4; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            colorTexCache[key] = tex;
            return tex;
        }

        private Material ColorMat(Color c)
        {
            // Round 11 (Claude 4.5): Use Standard shader for colored materials.
            // Standard shader supports lighting, shadows, and PBR properties.
            // The white pixel texture + _Color tinting gives proper colored surfaces with lighting.
            if (standardShader != null)
            {
                var mat = new Material(standardShader);
                mat.color = c;
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", c);
                // Set white texture so _Color tinting works correctly
                if (whitePixelTex != null)
                    mat.SetTexture("_MainTex", whitePixelTex);
                mat.SetFloat("_Glossiness", 0.3f);
                mat.SetFloat("_Metallic", 0.0f);
                mat.renderQueue = 2000;
                return mat;
            }
            // Fallback to Unlit/Texture if Standard not available
            if (unlitTextureShader != null)
            {
                var m = new Material(unlitTextureShader);
                m.SetTexture("_MainTex", GetColorTexture(c));
                m.renderQueue = 2000;
                return m;
            }
            // Last resort fallback
            var fallback = new Material(baseMat);
            fallback.color = c;
            fallback.renderQueue = 2000;
            return fallback;
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
            // Claude Bedrock fix #1: FORCE Forward rendering - Deferred fails silently on Android GPUs
            mainCamera.renderingPath = RenderingPath.Forward;
            // Claude Bedrock fix #1b: Disable HDR and MSAA on camera for mobile compatibility
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;
            // Claude Bedrock Priority 4: Force render ALL layers and disable occlusion culling on Android
            mainCamera.cullingMask = -1; // Render all layers
            mainCamera.depth = 0;
            #if UNITY_ANDROID && !UNITY_EDITOR
            mainCamera.useOcclusionCulling = false;
            #endif
            Debug.Log($"[SceneBuilder] Camera rendering path: {mainCamera.renderingPath}, HDR: {mainCamera.allowHDR}, MSAA: {mainCamera.allowMSAA}, CullingMask: {mainCamera.cullingMask}");
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
            // Claude Bedrock fix: Each manager creation is independently try-caught
            // so a failure in one system doesn't prevent all other systems from loading.
            SafeAddManager("GameManager", () => {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                DontDestroyOnLoad(gmObj);
            });

            SafeAddManager("NeedSystem", () => {
                new GameObject("NeedSystem").AddComponent<NeedSystem>();
            });

            SafeAddManager("InputManager", () => {
                var inputObj = new GameObject("InputManager");
                var inputMgr = inputObj.AddComponent<InputSystem.InputManager>();
                inputMgr.MainCamera = mainCamera;
            });

            SafeAddManager("AudioManager", () => {
                var audioObj = new GameObject("AudioManager");
                var audioMgr = audioObj.AddComponent<Audio.AudioManager>();
                audioMgr.MusicSource = audioObj.AddComponent<AudioSource>();
                audioMgr.AmbientSource = MakeChildAudio(audioObj, "Ambient");
                audioMgr.SFXSource = MakeChildAudio(audioObj, "SFX");
                audioMgr.UISource = MakeChildAudio(audioObj, "UI");
                audioMgr.FootstepSource = MakeChildAudio(audioObj, "Footstep");
                DontDestroyOnLoad(audioObj);
            });

            SafeAddManager("ParticleManager", () => new GameObject("ParticleManager").AddComponent<Particles.ParticleManager>());

            SafeAddManager("SaveManager", () => {
                var sObj = new GameObject("SaveManager");
                sObj.AddComponent<Data.SaveManager>();
                DontDestroyOnLoad(sObj);
            });

            SafeAddManager("RewardSystem", () => new GameObject("RewardSystem").AddComponent<RewardSystem>());
            SafeAddManager("ShopSystem", () => new GameObject("ShopSystem").AddComponent<ShopSystem>());
            SafeAddManager("AchievementSystem", () => new GameObject("AchievementSystem").AddComponent<AchievementSystem>());
            SafeAddManager("DailyEventSystem", () => new GameObject("DailyEventSystem").AddComponent<DailyEventSystem>());
            SafeAddManager("RoomManager", () => new GameObject("RoomManager").AddComponent<Rooms.RoomManager>());
            SafeAddManager("PostProcessing", () => new GameObject("PostProcessing").AddComponent<PostProcessingSetup>());

            // === Enhancement Systems ===
            SafeAddManager("ToonShading", () => new GameObject("ToonShading").AddComponent<Visual.ToonShading>());
            SafeAddManager("ProceduralParticles", () => new GameObject("ProceduralParticles").AddComponent<Visual.ProceduralParticles>());
            SafeAddManager("DynamicLighting", () => new GameObject("DynamicLighting").AddComponent<Visual.DynamicLighting>());

            SafeAddManager("QuestSystem", () => new GameObject("QuestSystem").AddComponent<Gameplay.QuestSystem>());
            SafeAddManager("CollectionSystem", () => new GameObject("CollectionSystem").AddComponent<Gameplay.CollectionSystem>());
            SafeAddManager("CharacterCustomization", () => new GameObject("CharacterCustomization").AddComponent<Gameplay.CharacterCustomization>());
            SafeAddManager("MiniGameLauncher", () => new GameObject("MiniGameLauncher").AddComponent<Gameplay.MiniGameLauncher>());
            SafeAddManager("RoomDecorator", () => new GameObject("RoomDecorator").AddComponent<Gameplay.RoomDecorator>());
            SafeAddManager("PhotoMode", () => new GameObject("PhotoMode").AddComponent<Gameplay.PhotoMode>());

            SafeAddManager("AdaptiveMusicSystem", () => new GameObject("AdaptiveMusicSystem").AddComponent<Audio.AdaptiveMusicSystem>());
            SafeAddManager("CharacterVoiceSystem", () => new GameObject("CharacterVoiceSystem").AddComponent<Audio.CharacterVoiceSystem>());
            SafeAddManager("SpatialAudioSystem", () => new GameObject("SpatialAudioSystem").AddComponent<Audio.SpatialAudioSystem>());

            SafeAddManager("EmotionalAnimator", () => new GameObject("EmotionalAnimator").AddComponent<Animation.EmotionalAnimator>());
            SafeAddManager("ActivityAnimations", () => new GameObject("ActivityAnimations").AddComponent<Animation.ActivityAnimations>());

            SafeAddManager("LODManager", () => new GameObject("LODManager").AddComponent<Performance.LODManager>());
            SafeAddManager("ObjectPoolManager", () => new GameObject("ObjectPoolManager").AddComponent<Performance.ObjectPoolManager>());
            SafeAddManager("PerformanceOptimizer", () => new GameObject("PerformanceOptimizer").AddComponent<Performance.PerformanceOptimizer>());

            SafeAddManager("TutorialSystem", () => new GameObject("TutorialSystem").AddComponent<Systems.TutorialSystem>());
            SafeAddManager("RoomProgressionSystem", () => new GameObject("RoomProgressionSystem").AddComponent<Systems.RoomProgressionSystem>());
            SafeAddManager("AnalyticsManager", () => new GameObject("AnalyticsManager").AddComponent<Systems.AnalyticsManager>());
            SafeAddManager("SocialSystem", () => new GameObject("SocialSystem").AddComponent<Systems.SocialSystem>());
            SafeAddManager("ParentGate", () => new GameObject("ParentGate").AddComponent<Systems.ParentGate>());
            SafeAddManager("AccessibilityManager", () => new GameObject("AccessibilityManager").AddComponent<Systems.AccessibilityManager>());
            SafeAddManager("AdIntegration", () => new GameObject("AdIntegration").AddComponent<Systems.AdIntegration>());
            SafeAddManager("CosmeticPackSystem", () => new GameObject("CosmeticPackSystem").AddComponent<Systems.CosmeticPackSystem>());
            SafeAddManager("DailyRewardSystem", () => new GameObject("DailyRewardSystem").AddComponent<Systems.DailyRewardSystem>());
        }

        /// <summary>
        /// Claude Bedrock fix: Safely adds a manager system with independent error handling.
        /// A failure in one manager will NOT prevent other managers from loading.
        /// </summary>
        private void SafeAddManager(string name, System.Action createAction)
        {
            try
            {
                createAction();
                AndroidLog($"[SceneBuilder] Manager OK: {name}");
            }
            catch (System.Exception e)
            {
                AndroidLog($"[SceneBuilder] Manager FAILED: {name} - {e.Message}");
            }
        }

        private AudioSource MakeChildAudio(GameObject parent, string name)
        {
            var child = new GameObject(name + "Source");
            child.transform.SetParent(parent.transform);
            return child.AddComponent<AudioSource>();
        }

        private void SetupLighting()
        {
            // Claude Bedrock fix #3: Apply RenderSettings IMMEDIATELY (not deferred)
            // Using Flat ambient mode for maximum compatibility on Android
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.85f, 0.88f, 0.92f);
            RenderSettings.ambientIntensity = 1.1f;

            // Claude Bedrock fix #3: Create new lights BEFORE destroying old ones
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

            // NOW destroy old lights (keep new ones intact)
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var existing in allLights)
            {
                if (existing != ml && existing != fl && existing != rl)
                    Destroy(existing.gameObject);
            }
            Debug.Log($"[SceneBuilder] Lighting setup complete: {FindObjectsByType<Light>(FindObjectsSortMode.None).Length} lights active");
        }

        // Claude Bedrock fix #3: RenderSettings now applied immediately in SetupLighting()
        // Keeping this as a no-op in case any coroutine references it
        private IEnumerator SetupRenderSettingsDeferred()
        {
            yield return null; // No-op - settings applied immediately now
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

        // === GLB Model Loading via glTFast (Round 11: RE-ENABLED with IL2CPP protection) ===
        private IEnumerator LoadGLBCharactersCoroutine()
        {
            AndroidLog("[SceneBuilder] Starting GLB character loading (GLTFast RE-ENABLED Round 11)...");
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
            AndroidLog("[SceneBuilder] GLB character loading complete!");
        }

        private IEnumerator LoadSingleGLBCharacter(string glbFileName, string charName, bool isMain)
        {
            string uri = Path.Combine(Application.streamingAssetsPath, "Characters", glbFileName + ".glb");
            AndroidLog($"[SceneBuilder] Loading GLB: {uri}");

            // Step 1: Load GLB bytes via UnityWebRequest (required for Android StreamingAssets in APK)
            byte[] glbData = null;
            using (var request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    AndroidLog($"[SceneBuilder] Failed to download GLB {glbFileName}: {request.error}");
                    yield break;
                }
                glbData = request.downloadHandler.data;
                AndroidLog($"[SceneBuilder] GLB data loaded: {glbData.Length} bytes for {charName}");
            }

            // Step 2: Parse GLB with GltfImport
            var gltf = new GltfImport();
            var loadTask = gltf.LoadGltfBinary(glbData, new System.Uri(uri));

            // Wait for async task to complete (coroutine-safe polling)
            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.IsFaulted)
            {
                AndroidLog($"[SceneBuilder] GLTFast parse EXCEPTION for {charName}: {loadTask.Exception?.Message}");
                yield break;
            }
            if (!loadTask.Result)
            {
                AndroidLog($"[SceneBuilder] GLTFast parse FAILED for {charName}");
                yield break;
            }

            AndroidLog($"[SceneBuilder] GLB parsed successfully for {charName}");

            // Step 3: Find the primitive placeholder
            Transform existing = characterContainer != null ? characterContainer.Find(charName) : null;
            if (existing == null)
            {
                AndroidLog($"[SceneBuilder] Could not find primitive {charName} to replace");
                yield break;
            }

            Vector3 savedPos = existing.position;
            Quaternion savedRot = existing.rotation;
            Vector3 savedScale = existing.localScale;
            Transform savedParent = existing.parent;

            // Step 4: Create parent object and instantiate GLB model
            var glbObj = new GameObject(charName);
            glbObj.transform.SetParent(savedParent, false);
            glbObj.transform.position = savedPos;
            glbObj.transform.rotation = savedRot;
            glbObj.transform.localScale = savedScale;

            var instantiateTask = gltf.InstantiateMainSceneAsync(glbObj.transform);
            while (!instantiateTask.IsCompleted)
                yield return null;

            if (instantiateTask.IsFaulted)
            {
                AndroidLog($"[SceneBuilder] GLB instantiate EXCEPTION for {charName}: {instantiateTask.Exception?.Message}");
                Destroy(glbObj);
                yield break;
            }

            AndroidLog($"[SceneBuilder] GLB instantiated for {charName}");

            // Round 12 (Claude 4.5 Bedrock): DO NOT convert GLTFast materials!
            // GLTFast 6.7.1 uses its own Built-in RP compatible PBR shaders natively.
            // Converting to Standard shader DESTROYS texture references, causing white models.
            // Instead, just enable shadows on GLTFast's native materials.
            EnableGLTFShadows(glbObj);

            // Step 6: Add interaction components
            var col = glbObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.size = new Vector3(1f, 2.2f, 0.8f);
            glbObj.AddComponent<CharacterBob>();

            // Step 7: Update main character reference
            if (isMain)
            {
                emersynObj = glbObj;
                if (CameraSystem.CameraController.Instance != null)
                    CameraSystem.CameraController.Instance.Target = glbObj.transform;
                if (Gameplay.CharacterCustomization.Instance != null)
                    Gameplay.CharacterCustomization.Instance.ApplyToCharacter(glbObj);
            }

            // Step 8: Destroy primitive placeholder
            Destroy(existing.gameObject);
            AndroidLog($"[SceneBuilder] SUCCESS: Replaced {charName} with GLB 3D model!");
        }

        private IEnumerator LoadSingleGLBPet(string glbFileName, string petName)
        {
            string uri = Path.Combine(Application.streamingAssetsPath, "Characters", glbFileName + ".glb");
            AndroidLog($"[SceneBuilder] Loading pet GLB: {uri}");

            // Step 1: Load GLB bytes
            byte[] glbData = null;
            using (var request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    AndroidLog($"[SceneBuilder] Failed to download pet GLB {glbFileName}: {request.error}");
                    yield break;
                }
                glbData = request.downloadHandler.data;
                AndroidLog($"[SceneBuilder] Pet GLB data: {glbData.Length} bytes for {petName}");
            }

            // Step 2: Parse with GltfImport
            var gltf = new GltfImport();
            var loadTask = gltf.LoadGltfBinary(glbData, new System.Uri(uri));
            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.IsFaulted || !loadTask.Result)
            {
                AndroidLog($"[SceneBuilder] Pet GLB parse failed for {petName}");
                yield break;
            }

            // Step 3: Replace primitive
            Transform existing = characterContainer != null ? characterContainer.Find(petName) : null;
            if (existing == null) yield break;

            Vector3 savedPos = existing.position;
            Transform savedParent = existing.parent;

            var glbObj = new GameObject(petName);
            glbObj.transform.SetParent(savedParent, false);
            glbObj.transform.position = savedPos;
            glbObj.transform.localScale = Vector3.one * 0.5f;

            var instantiateTask = gltf.InstantiateMainSceneAsync(glbObj.transform);
            while (!instantiateTask.IsCompleted)
                yield return null;

            if (instantiateTask.IsFaulted)
            {
                Destroy(glbObj);
                yield break;
            }

            // Round 12: Use GLTFast native materials, just enable shadows
            EnableGLTFShadows(glbObj);
            var col = glbObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.4f, 0);
            col.size = new Vector3(0.6f, 0.8f, 0.8f);
            glbObj.AddComponent<CharacterBob>();

            Destroy(existing.gameObject);
            AndroidLog($"[SceneBuilder] SUCCESS: Replaced pet {petName} with GLB 3D model!");
        }

        /// <summary>
        /// Round 12 (Claude 4.5 Bedrock): DO NOT convert GLTFast materials to Standard shader.
        /// GLTFast 6.7.1 uses its own Built-in RP compatible PBR shaders that already support
        /// lighting, shadows, and proper texture rendering. Converting to Standard shader
        /// DESTROYS the internal texture references, causing white/untextured models.
        /// Instead, just enable shadow casting/receiving on the native GLTFast materials.
        /// </summary>
        private void EnableGLTFShadows(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            int texCount = 0;
            foreach (var renderer in renderers)
            {
                // Enable shadows on GLTFast's native materials
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;

                // Log material info for diagnostics
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.mainTexture != null)
                        texCount++;
                }
            }
            AndroidLog($"[SceneBuilder] GLTFast native materials: {renderers.Length} renderers, {texCount} with textures, shadows enabled");
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

            // Load and apply baked lightmaps from Modal GPU pipeline
            yield return StartCoroutine(SafeCoroutine(LoadAndApplyLightmaps(roomIndex)));
        }

        // === Lightmap Loading from Modal GPU Baked Output ===
        private IEnumerator LoadAndApplyLightmaps(int roomIndex)
        {
            string roomName = RoomNames[roomIndex].ToLower();
            string[] surfaces = (roomIndex == 3 || roomIndex == 8)
                ? new[] { "Floor" }  // Outdoor rooms only have floor
                : new[] { "Floor", "BackWall", "LeftWall", "RightWall" };

            foreach (string surface in surfaces)
            {
                string lightmapFile = $"{roomName}_{surface}_lightmap.png";
                string lightmapPath = Path.Combine(Application.streamingAssetsPath, "Lightmaps", lightmapFile);

                using (var request = UnityWebRequestTexture.GetTexture(lightmapPath))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var lightmapTex = DownloadHandlerTexture.GetContent(request);
                        lightmapTex.filterMode = FilterMode.Bilinear;
                        lightmapTex.wrapMode = TextureWrapMode.Clamp;

                        // Apply lightmap as emission/detail texture on the surface material
                        Transform surfaceObj = roomContainer.Find(surface);
                        if (surfaceObj != null)
                        {
                            var renderer = surfaceObj.GetComponent<Renderer>();
                            if (renderer != null && renderer.material != null)
                            {
                                // Use detail albedo for lightmap blending on Standard shader
                                renderer.material.SetTexture("_DetailAlbedoMap", lightmapTex);
                                renderer.material.SetTextureScale("_DetailAlbedoMap", Vector2.one);
                                renderer.material.EnableKeyword("_DETAIL_MULX2");
                                renderer.material.SetFloat("_DetailNormalMapScale", 1.0f);
                                Debug.Log($"[SceneBuilder] Lightmap applied: {lightmapFile}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneBuilder] Lightmap not found: {lightmapFile}");
                    }
                }
            }
            Debug.Log($"[SceneBuilder] Lightmaps applied to room {RoomNames[roomIndex]}");
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
            // Round 11 (Claude 4.5): Use Standard shader for full PBR with textures.
            // Standard shader gives us proper lighting, shadows, and normal mapping.
            Material mat;
            if (standardShader != null)
            {
                mat = new Material(standardShader);
                mat.SetTexture("_MainTex", albedo);
                mat.color = Color.white;
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetFloat("_BumpScale", 1.0f);
                }
                mat.SetFloat("_Glossiness", 0.3f);
                mat.SetFloat("_Metallic", 0.0f);
                mat.enableInstancing = true;
                mat.renderQueue = 2000;
                return mat;
            }
            // Fallback to Unlit/Texture if Standard not available
            if (unlitTextureShader != null)
            {
                mat = new Material(unlitTextureShader);
                mat.SetTexture("_MainTex", albedo);
                mat.renderQueue = 2000;
                return mat;
            }
            // Last resort
            mat = new Material(baseMat);
            mat.SetTexture("_MainTex", albedo);
            mat.color = Color.white;
            mat.enableInstancing = true;
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
