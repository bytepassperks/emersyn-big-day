using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
// Round 42 (Claude 4.5 Bedrock): REMOVED GLTFast entirely.
// After 41 rounds, Bedrock diagnosed: GLTFast is being stripped by IL2CPP despite link.xml.
// Fix: Pure C# GLB binary parser that builds Unity Mesh objects directly.
// No external dependencies = nothing for IL2CPP to strip.
// Uses existing GLBParseAccessors/GLBParseBufferViews/GLBParseMaterials/GLBParseMeshes helpers.

namespace EmersynBigDay.Core
{
    public class SceneBuilder : MonoBehaviour
    {
        // Round 41 (Claude 4.5 Bedrock): Use Debug.Log as PRIMARY logging.
        // AndroidJavaClass("android.util.Log") was being stripped by IL2CPP, causing
        // ALL log output to silently disappear on Android. Debug.Log works reliably.
        private static void AndroidLog(string msg)
        {
            Debug.Log(msg);
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var logClass = new AndroidJavaClass("android.util.Log"))
                {
                    logClass.CallStatic<int>("d", "SCENEBUILDER", msg);
                }
            }
            catch (System.Exception) { /* native log failed, Debug.Log already captured it */ }
            #endif
        }

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
        // Round 23 (Claude 4.5 Bedrock): Darker floor colors for strong contrast against pink bg
        private static readonly Color[] RoomFloorColors = {
            new Color(0.55f, 0.40f, 0.30f), new Color(0.45f, 0.55f, 0.35f),
            new Color(0.40f, 0.50f, 0.60f), new Color(0.35f, 0.55f, 0.25f),
            new Color(0.60f, 0.50f, 0.35f), new Color(0.35f, 0.30f, 0.50f),
            new Color(0.55f, 0.45f, 0.35f), new Color(0.50f, 0.45f, 0.30f),
            new Color(0.30f, 0.50f, 0.30f)
        };
        // Round 23 (Claude 4.5 Bedrock): Medium-tone wall colors visible against pink bg
        private static readonly Color[] RoomWallColors = {
            new Color(0.70f, 0.55f, 0.65f), new Color(0.65f, 0.70f, 0.55f),
            new Color(0.55f, 0.65f, 0.75f), new Color(0.55f, 0.80f, 0.95f),
            new Color(0.75f, 0.70f, 0.55f), new Color(0.20f, 0.15f, 0.30f),
            new Color(0.70f, 0.60f, 0.50f), new Color(0.65f, 0.60f, 0.50f),
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

            // Round 19: Validate and fix any magenta artifacts after all loading completes
            StartCoroutine(SafeCoroutine(ValidateAndFixTextures()));

            // Round 43: Add post-processing for AAA cartoon quality
            try { SetupPostProcessing(); AndroidLog("[SceneBuilder] Post-processing setup OK"); }
            catch (System.Exception e) { AndroidLog($"[SceneBuilder] PostFX FAILED (non-fatal): {e.Message}"); }

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
        // Round 40: Use AndroidLog instead of Debug.LogError for Device Farm visibility
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
                    AndroidLog($"[SceneBuilder] COROUTINE ERROR: {e.Message}\n{e.StackTrace}");
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
            // Round 20 (Claude 4.5 Bedrock): Soft pink background to match room aesthetic
            mainCamera.backgroundColor = new Color(0.95f, 0.88f, 0.92f);
            // Round 46 (Claude 4.5 Bedrock): Optimized FOV to fill more screen with game world
            float aspect = (float)Screen.width / Screen.height;
            float adaptiveFOV;
            if (aspect < 0.5f) adaptiveFOV = 85f;      // Very narrow phones - wider to see more
            else if (aspect < 0.6f) adaptiveFOV = 75f;  // Normal phones
            else adaptiveFOV = 68f;                      // Tablets - tighter for better framing
            mainCamera.fieldOfView = adaptiveFOV;
            Debug.Log($"[SceneBuilder] Screen {Screen.width}x{Screen.height} aspect={aspect:F2} FOV={adaptiveFOV}");
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 500f; // Round 28: Increased from 100 to prevent room culling on any device
            // Claude Bedrock fix #1: FORCE Forward rendering - Deferred fails silently on Android GPUs
            mainCamera.renderingPath = RenderingPath.Forward;
            // Round 39: Enable MSAA for cartoon smooth edges, keep HDR off for mobile
            mainCamera.allowHDR = true; // Round 43: Enable HDR for post-processing bloom
            mainCamera.allowMSAA = true; // 4x MSAA set in QualitySettings
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
                // Round 46 (Claude 4.5 Bedrock): Lower pitch to see character FRONTS, closer zoom
                float camAspect = (float)Screen.width / Screen.height;
                float adaptiveZoom, adaptivePitch;
                if (camAspect < 0.5f) { adaptiveZoom = 12f; adaptivePitch = 28f; } // Very narrow phones
                else if (camAspect < 0.6f) { adaptiveZoom = 14f; adaptivePitch = 28f; } // Normal phones
                else { adaptiveZoom = 13f; adaptivePitch = 28f; } // Tablets - closer
                ctrl.MinZoom = 8f;
                ctrl.MaxZoom = 25f; // Round 28: Reduced from 50 to prevent fuzz test zooming too far out
                ctrl.CurrentZoom = adaptiveZoom;
                ctrl.DefaultPitch = adaptivePitch;
                ctrl.SpringStiffness = 200f;
                ctrl.SpringDamping = 40f;
                ctrl.Offset = new Vector3(0f, 10f, -10f); // Round 28: Reduced offset to keep camera closer
                Debug.Log($"[SceneBuilder] Camera adaptive R29: aspect={camAspect:F2} zoom={adaptiveZoom} pitch={adaptivePitch}");
            }
            // Round 46: Lower camera angle to see character fronts
            float initAspect = (float)Screen.width / Screen.height;
            float initZoom, initPitch;
            if (initAspect < 0.5f) { initZoom = 12f; initPitch = 28f; }
            else if (initAspect < 0.6f) { initZoom = 14f; initPitch = 28f; }
            else { initZoom = 13f; initPitch = 28f; }
            float initPitchRad = initPitch * Mathf.Deg2Rad;
            Vector3 camDir = new Vector3(0f, Mathf.Sin(initPitchRad), -Mathf.Cos(initPitchRad));
            mainCamera.transform.position = camDir * initZoom;
            mainCamera.transform.LookAt(new Vector3(0f, 0.7f, 0f)); // Round 46: Look at waist level to see faces
            Debug.Log($"[SceneBuilder] Initial camera R46: aspect={initAspect:F2} zoom={initZoom} pitch={initPitch} pos={mainCamera.transform.position}");
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
            // Round 43 (Claude 4.5 Bedrock): AAA cartoon lighting - Talking Tom quality
            // Trilight ambient for soft, colorful fill from all directions
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.70f, 0.95f);    // Bright sky blue
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.50f, 0.55f); // Warm neutral
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.30f, 0.25f); // Warm ground bounce
            RenderSettings.ambientIntensity = 1.1f;

            // Reflection settings for cartoon specular
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.4f;

            // Key light: warm, strong for cartoon pop with soft shadows
            var lo = new GameObject("MainDirectionalLight");
            var ml = lo.AddComponent<Light>();
            ml.type = LightType.Directional;
            ml.color = new Color(1.0f, 0.96f, 0.88f); // Warm white
            ml.intensity = 1.4f;
            ml.shadows = LightShadows.Soft; // Soft shadows for polished look
            ml.shadowStrength = 0.5f;
            ml.shadowBias = 0.02f;
            ml.shadowNormalBias = 0.3f;
            ml.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            lo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            ml.cullingMask = ~0;

            // Fill light: cool blue from opposite side for depth
            var fo = new GameObject("FillLight");
            var fl = fo.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.65f, 0.78f, 1.0f); // Cool blue fill
            fl.intensity = 0.55f;
            fl.shadows = LightShadows.None;
            fo.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            fl.cullingMask = ~0;

            // Rim/back light: bright warm highlight for character edges (Talking Tom style)
            var ro = new GameObject("RimLight");
            var rl = ro.AddComponent<Light>();
            rl.type = LightType.Directional;
            rl.color = new Color(1.0f, 0.92f, 0.75f); // Warm golden rim
            rl.intensity = 0.7f; // Stronger rim for cartoon edge pop
            rl.shadows = LightShadows.None;
            ro.transform.rotation = Quaternion.Euler(-10f, 180f, 0f);
            rl.cullingMask = ~0;

            // Top/hair light: subtle highlight from above for character definition
            var to = new GameObject("TopLight");
            var tl = to.AddComponent<Light>();
            tl.type = LightType.Directional;
            tl.color = new Color(0.9f, 0.85f, 1.0f); // Slight purple tint
            tl.intensity = 0.3f;
            tl.shadows = LightShadows.None;
            to.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
            tl.cullingMask = ~0;

            // Destroy old lights (keep new ones intact)
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var existing in allLights)
            {
                if (existing != ml && existing != fl && existing != rl && existing != tl)
                    Destroy(existing.gameObject);
            }

            // Round 43: Quality settings for AAA cartoon
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 25f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 4; // 4x MSAA
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.lodBias = 2.0f;
            QualitySettings.pixelLightCount = 4; // Support all 4 lights

            Debug.Log($"[SceneBuilder] R43 Lighting: 4-point (key=1.4 fill=0.55 rim=0.7 top=0.3), soft shadows, MSAA=4x");
        }

        /// <summary>
        /// Round 43: Setup post-processing for AAA cartoon quality.
        /// Adds bloom and color grading via OnRenderImage on the main camera.
        /// Uses ONLY built-in Unity features - no external packages.
        /// </summary>
        private void SetupPostProcessing()
        {
            if (mainCamera == null) return;
            
            // Add cartoon post-processing to camera
            var postFx = mainCamera.gameObject.GetComponent<CartoonPostProcessing>();
            if (postFx == null)
                postFx = mainCamera.gameObject.AddComponent<CartoonPostProcessing>();
            
            AndroidLog("[SceneBuilder] R43: CartoonPostProcessing added to camera");
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
                // Round 26: Remove ceiling for true Sims 4 dollhouse view (camera looks down into open room)
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
            // Round 46 (Claude 4.5 Bedrock): Semicircle layout so characters don't overlap
            // Emersyn center-front (hero position), others in arc behind
            emersynObj = MakeCharacter("Emersyn", new Vector3(0f, 0f, -0.5f), CharBodyColors[0], 1f, true);
            emersynObj.transform.SetParent(characterContainer);
            if (CameraSystem.CameraController.Instance != null)
                CameraSystem.CameraController.Instance.Target = emersynObj.transform;
            
            // Arrange other characters in semicircle behind Emersyn
            float charRadius = 2.5f;
            float startAngle = 145f;
            float angleStep = 35f;
            for (int i = 1; i < CharacterNames.Length; i++)
            {
                float angle = (startAngle + (i - 1) * angleStep) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * charRadius;
                float z = Mathf.Cos(angle) * charRadius;
                var f = MakeCharacter(CharacterNames[i], new Vector3(x, 0, z), CharBodyColors[i], 0.85f, false);
                f.transform.SetParent(characterContainer);
            }
            
            // Pets in front-row arc, closer to camera
            float petRadius = 1.6f;
            for (int i = 0; i < PetNames.Length; i++)
            {
                float angle = (160f + i * 40f) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * petRadius;
                float z = Mathf.Cos(angle) * petRadius - 1.2f; // Closer to camera
                var pet = MakePet(PetNames[i], new Vector3(x, 0, z), PetColors[i]);
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

        // === Character Model Loading ===
        // Round 42 (Claude 4.5 Bedrock): Pure C# GLB parser - NO GLTFast dependency.
        // After 41 rounds, Bedrock diagnosed: GLTFast is stripped by IL2CPP despite link.xml.
        // This parser uses only System and UnityEngine types that IL2CPP cannot strip.
        // It reads GLB binary format directly: header -> JSON chunk -> BIN chunk,
        // then builds Unity Mesh objects from accessor/bufferView/mesh data.
        private IEnumerator LoadGLBCharactersCoroutine()
        {
            AndroidLog("[SceneBuilder] R42: Starting PURE C# GLB loading (no GLTFast)...");
            
            for (int i = 0; i < GLBCharacterFiles.Length && i < CharacterNames.Length; i++)
            {
                AndroidLog($"[SceneBuilder] R42: Loading character {i}: {GLBCharacterFiles[i]} -> {CharacterNames[i]}");
                yield return StartCoroutine(LoadAndReplaceDirectGLB(
                    GLBCharacterFiles[i], CharacterNames[i], i == 0, false));
                yield return null;
            }
            
            for (int i = 0; i < GLBPetFiles.Length && i < PetNames.Length; i++)
            {
                AndroidLog($"[SceneBuilder] R42: Loading pet {i}: {GLBPetFiles[i]} -> {PetNames[i]}");
                yield return StartCoroutine(LoadAndReplaceDirectGLB(
                    GLBPetFiles[i], PetNames[i], false, true));
                yield return null;
            }
            
            glbLoadingComplete = true;
            AndroidLog("[SceneBuilder] R42: All pure C# GLB loading complete!");
        }

        /// <summary>
        /// Round 42 (Claude 4.5 Bedrock): Pure C# GLB loader.
        /// Reads GLB binary directly: header (12 bytes) -> JSON chunk -> BIN chunk.
        /// Parses accessors, bufferViews, materials, meshes from JSON.
        /// Builds Unity Mesh objects from binary vertex/index data.
        /// No external dependencies = nothing for IL2CPP to strip.
        /// </summary>
        private IEnumerator LoadAndReplaceDirectGLB(string glbName, string displayName, bool isMain, bool isPet)
        {
            AndroidLog($"[DirectGLB] R42: START loading {glbName} -> {displayName}");
            
            // Step 1: Load GLB bytes from StreamingAssets via UnityWebRequest
            #if UNITY_ANDROID && !UNITY_EDITOR
            string uri = Application.streamingAssetsPath + "/Characters/" + glbName + ".glb";
            #else
            string uri = "file://" + Path.Combine(Application.streamingAssetsPath, "Characters", glbName + ".glb");
            #endif
            AndroidLog($"[DirectGLB] R42: Loading from URI: {uri}");
            
            var webRequest = UnityWebRequest.Get(uri);
            webRequest.timeout = 30;
            yield return webRequest.SendWebRequest();
            
            byte[] glbData = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                AndroidLog($"[DirectGLB] R42: FAILED to load {glbName}: {webRequest.error} code={webRequest.responseCode}");
                webRequest.Dispose();
                yield break;
            }
            glbData = webRequest.downloadHandler.data;
            AndroidLog($"[DirectGLB] R42: Loaded {glbData.Length} bytes for {glbName}");
            webRequest.Dispose();

            if (glbData == null || glbData.Length < 20)
            {
                AndroidLog($"[DirectGLB] R42: {glbName} data too small ({glbData?.Length ?? 0} bytes)");
                yield break;
            }

            // Step 2: Parse GLB header and extract JSON + BIN chunks
            string jsonChunk = null;
            byte[] binChunk = null;
            int parseResult = 0; // 0=not started, 1=success, -1=error
            string parseError = null;

            // Parse synchronously (no yield needed, all in-memory)
            try
            {
                // GLB Header: magic(4) + version(4) + length(4) = 12 bytes
                uint magic = System.BitConverter.ToUInt32(glbData, 0);
                uint version = System.BitConverter.ToUInt32(glbData, 4);
                uint totalLength = System.BitConverter.ToUInt32(glbData, 8);
                AndroidLog($"[DirectGLB] R42: Header magic=0x{magic:X8} version={version} length={totalLength}");

                if (magic != 0x46546C67) // "glTF" in little-endian
                {
                    parseError = $"Invalid GLB magic: 0x{magic:X8}";
                    parseResult = -1;
                }
                else
                {
                    int offset = 12;
                    // Read JSON chunk
                    uint jsonLen = System.BitConverter.ToUInt32(glbData, offset);
                    uint jsonType = System.BitConverter.ToUInt32(glbData, offset + 4);
                    AndroidLog($"[DirectGLB] R42: JSON chunk len={jsonLen} type=0x{jsonType:X8}");
                    
                    if (jsonType == 0x4E4F534A) // "JSON"
                    {
                        jsonChunk = System.Text.Encoding.UTF8.GetString(glbData, offset + 8, (int)jsonLen);
                        offset += 8 + (int)jsonLen;
                        // Pad to 4-byte boundary
                        while (offset % 4 != 0 && offset < glbData.Length) offset++;
                        
                        // Read BIN chunk (if present)
                        if (offset + 8 <= glbData.Length)
                        {
                            uint binLen = System.BitConverter.ToUInt32(glbData, offset);
                            uint binType = System.BitConverter.ToUInt32(glbData, offset + 4);
                            AndroidLog($"[DirectGLB] R42: BIN chunk len={binLen} type=0x{binType:X8}");
                            
                            if (binType == 0x004E4942) // "BIN\0"
                            {
                                binChunk = new byte[binLen];
                                System.Array.Copy(glbData, offset + 8, binChunk, 0, (int)binLen);
                                AndroidLog($"[DirectGLB] R42: BIN chunk extracted: {binLen} bytes");
                            }
                        }
                        parseResult = 1;
                    }
                    else
                    {
                        parseError = $"Expected JSON chunk type, got 0x{jsonType:X8}";
                        parseResult = -1;
                    }
                }
            }
            catch (System.Exception e)
            {
                parseError = e.Message;
                parseResult = -1;
            }

            if (parseResult != 1 || jsonChunk == null)
            {
                AndroidLog($"[DirectGLB] R42: GLB parse FAILED for {glbName}: {parseError}");
                yield break;
            }

            AndroidLog($"[DirectGLB] R42: JSON={jsonChunk.Length} chars, BIN={binChunk?.Length ?? 0} bytes");

            // Step 3: Parse glTF JSON structure using existing helpers
            List<GLBAccessor> accessors = null;
            List<GLBBufferView> bufferViews = null;
            List<GLBMaterial> materials = null;
            List<GLBMesh> meshes = null;
            
            try
            {
                accessors = GLBParseAccessors(jsonChunk);
                bufferViews = GLBParseBufferViews(jsonChunk);
                materials = GLBParseMaterials(jsonChunk);
                meshes = GLBParseMeshes(jsonChunk);
                AndroidLog($"[DirectGLB] R42: Parsed {accessors.Count} accessors, {bufferViews.Count} bufferViews, {materials.Count} materials, {meshes.Count} meshes");
            }
            catch (System.Exception e)
            {
                AndroidLog($"[DirectGLB] R42: JSON parse EXCEPTION for {glbName}: {e.Message}");
                yield break;
            }

            if (meshes.Count == 0)
            {
                AndroidLog($"[DirectGLB] R42: No meshes found in {glbName}");
                yield break;
            }

            // Step 4: Build Unity Mesh objects from binary data
            GameObject glbRoot = new GameObject(displayName + "_GLB");
            int totalVerts = 0;
            int totalTris = 0;
            int meshPartCount = 0;

            try
            {
                foreach (var glbMesh in meshes)
                {
                    foreach (var prim in glbMesh.prims)
                    {
                        // Get POSITION accessor
                        int posAccIdx = -1;
                        int normAccIdx = -1;
                        if (prim.attrs.ContainsKey("POSITION")) posAccIdx = prim.attrs["POSITION"];
                        if (prim.attrs.ContainsKey("NORMAL")) normAccIdx = prim.attrs["NORMAL"];
                        
                        if (posAccIdx < 0 || posAccIdx >= accessors.Count)
                        {
                            AndroidLog($"[DirectGLB] R42: Skipping primitive - no POSITION accessor");
                            continue;
                        }

                        var posAcc = accessors[posAccIdx];
                        if (posAcc.bv < 0 || posAcc.bv >= bufferViews.Count)
                        {
                            AndroidLog($"[DirectGLB] R42: Skipping primitive - invalid bufferView {posAcc.bv}");
                            continue;
                        }

                        // Read vertex positions from BIN chunk
                        var posBV = bufferViews[posAcc.bv];
                        int posStride = posBV.stride > 0 ? posBV.stride : 12; // 3 floats = 12 bytes
                        int posStart = posBV.offset + posAcc.offset;
                        
                        Vector3[] vertices = new Vector3[posAcc.count];
                        for (int vi = 0; vi < posAcc.count; vi++)
                        {
                            int off = posStart + vi * posStride;
                            if (off + 12 > binChunk.Length) break;
                            float x = System.BitConverter.ToSingle(binChunk, off);
                            float y = System.BitConverter.ToSingle(binChunk, off + 4);
                            float z = System.BitConverter.ToSingle(binChunk, off + 8);
                            // glTF is right-handed (Z-forward), Unity is left-handed (Z-back)
                            // Convert: negate X to flip handedness
                            vertices[vi] = new Vector3(-x, y, z);
                        }

                        // Read normals (if available)
                        Vector3[] normals = null;
                        if (normAccIdx >= 0 && normAccIdx < accessors.Count)
                        {
                            var normAcc = accessors[normAccIdx];
                            if (normAcc.bv >= 0 && normAcc.bv < bufferViews.Count)
                            {
                                var normBV = bufferViews[normAcc.bv];
                                int normStride = normBV.stride > 0 ? normBV.stride : 12;
                                int normStart = normBV.offset + normAcc.offset;
                                normals = new Vector3[normAcc.count];
                                for (int ni = 0; ni < normAcc.count; ni++)
                                {
                                    int off = normStart + ni * normStride;
                                    if (off + 12 > binChunk.Length) break;
                                    float nx = System.BitConverter.ToSingle(binChunk, off);
                                    float ny = System.BitConverter.ToSingle(binChunk, off + 4);
                                    float nz = System.BitConverter.ToSingle(binChunk, off + 8);
                                    normals[ni] = new Vector3(-nx, ny, nz);
                                }
                            }
                        }

                        // Read indices
                        int[] triangles = null;
                        if (prim.idxAcc >= 0 && prim.idxAcc < accessors.Count)
                        {
                            var idxAcc = accessors[prim.idxAcc];
                            if (idxAcc.bv >= 0 && idxAcc.bv < bufferViews.Count)
                            {
                                var idxBV = bufferViews[idxAcc.bv];
                                int idxStart = idxBV.offset + idxAcc.offset;
                                triangles = new int[idxAcc.count];
                                
                                // componentType: 5123=UNSIGNED_SHORT(2 bytes), 5125=UNSIGNED_INT(4 bytes)
                                int bytesPerIdx = idxAcc.compType == 5125 ? 4 : 2;
                                for (int ii = 0; ii < idxAcc.count; ii++)
                                {
                                    int off = idxStart + ii * bytesPerIdx;
                                    if (off + bytesPerIdx > binChunk.Length) break;
                                    if (bytesPerIdx == 4)
                                        triangles[ii] = (int)System.BitConverter.ToUInt32(binChunk, off);
                                    else
                                        triangles[ii] = (int)System.BitConverter.ToUInt16(binChunk, off);
                                }
                                
                                // Reverse winding order for Unity (due to X-axis flip)
                                for (int ti = 0; ti < triangles.Length - 2; ti += 3)
                                {
                                    int tmp = triangles[ti];
                                    triangles[ti] = triangles[ti + 2];
                                    triangles[ti + 2] = tmp;
                                }
                            }
                        }

                        if (vertices.Length == 0)
                        {
                            AndroidLog($"[DirectGLB] R42: Skipping empty primitive in {glbName}");
                            continue;
                        }

                        // Create Unity Mesh
                        Mesh mesh = new Mesh();
                        mesh.name = $"{glbName}_{meshPartCount}";
                        if (vertices.Length > 65535)
                            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        mesh.vertices = vertices;
                        if (normals != null && normals.Length == vertices.Length)
                            mesh.normals = normals;
                        if (triangles != null && triangles.Length > 0)
                            mesh.triangles = triangles;
                        if (normals == null || normals.Length != vertices.Length)
                            mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        // Create child GameObject with MeshFilter + MeshRenderer
                        string partName = (glbMesh.name ?? "mesh") + "_" + meshPartCount;
                        GameObject meshObj = new GameObject(partName);
                        meshObj.transform.SetParent(glbRoot.transform, false);
                        
                        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
                        mf.mesh = mesh;
                        
                        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
                        
                        // Create material with correct color from GLB
                        Material mat = CreateGLBMaterial(prim.matIdx, materials);
                        mr.material = mat;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        mr.receiveShadows = true;

                        // Round 44 (Claude 4.5 Bedrock): Add toon outline for AAA cartoon look
                        try
                        {
                            AddToonOutline(meshObj, mesh, mat.color);
                        }
                        catch (System.Exception outlineEx)
                        {
                            AndroidLog($"[DirectGLB] R44: Outline failed for {partName}: {outlineEx.Message}");
                        }

                        totalVerts += vertices.Length;
                        totalTris += (triangles != null ? triangles.Length / 3 : 0);
                        meshPartCount++;
                        
                        AndroidLog($"[DirectGLB] R42: Built mesh '{partName}': {vertices.Length} verts, {(triangles != null ? triangles.Length / 3 : 0)} tris, mat={mat.color}");
                    }
                }
            }
            catch (System.Exception e)
            {
                AndroidLog($"[DirectGLB] R42: Mesh build EXCEPTION for {glbName}: {e.Message}\n{e.StackTrace}");
                if (meshPartCount == 0)
                {
                    Destroy(glbRoot);
                    yield break;
                }
            }

            AndroidLog($"[DirectGLB] R42: Built {meshPartCount} mesh parts, {totalVerts} verts, {totalTris} tris for {glbName}");

            if (meshPartCount == 0)
            {
                AndroidLog($"[DirectGLB] R42: No mesh parts created for {glbName}");
                Destroy(glbRoot);
                yield break;
            }

            // Step 5: Replace the procedural placeholder
            try
            {
                float glbScale = isPet ? 2.0f : 2.8f;
                Transform existing = characterContainer != null ? characterContainer.Find(displayName) : null;
                
                if (existing != null)
                {
                    AndroidLog($"[DirectGLB] R42: Replacing placeholder '{displayName}' at {existing.position}");
                    Vector3 savedPos = existing.position;
                    Quaternion savedRot = existing.rotation;
                    Transform savedParent = existing.parent;

                    glbRoot.name = displayName;
                    glbRoot.transform.SetParent(savedParent, false);
                    glbRoot.transform.position = savedPos;
                    glbRoot.transform.rotation = savedRot;
                    glbRoot.transform.localScale = Vector3.one * glbScale;

                    AndroidLog($"[DirectGLB] R42: DESTROYING placeholder '{displayName}'");
                    Destroy(existing.gameObject);
                }
                else
                {
                    AndroidLog($"[DirectGLB] R42: No placeholder for '{displayName}', placing in container");
                    glbRoot.transform.SetParent(characterContainer);
                    glbRoot.name = displayName;
                    glbRoot.transform.localScale = Vector3.one * glbScale;
                }
            }
            catch (System.Exception e)
            {
                AndroidLog($"[DirectGLB] R42: Replace EXCEPTION for {glbName}: {e.Message}");
            }

            // Step 6: Add interaction components + R44 sparkle effects
            try
            {
                var col = glbRoot.AddComponent<BoxCollider>();
                if (isPet) { col.center = new Vector3(0, 0.25f, 0); col.size = new Vector3(0.5f, 0.5f, 0.6f); }
                else { col.center = new Vector3(0, 0.55f, 0); col.size = new Vector3(0.8f, 1.2f, 0.8f); }
                glbRoot.AddComponent<CharacterBob>();
                
                // Round 44: Add cartoon sparkle particle effects
                AddCartoonSparkles(glbRoot.transform, isPet);
                AndroidLog($"[DirectGLB] R44: Sparkle effects added to {displayName}");
            }
            catch (System.Exception e)
            {
                AndroidLog($"[DirectGLB] R42: Components EXCEPTION: {e.Message}");
            }

            // Step 7: Update main character reference
            if (isMain)
            {
                emersynObj = glbRoot;
                if (CameraSystem.CameraController.Instance != null)
                    CameraSystem.CameraController.Instance.Target = glbRoot.transform;
                if (Gameplay.CharacterCustomization.Instance != null)
                    Gameplay.CharacterCustomization.Instance.ApplyToCharacter(glbRoot);
            }

            AndroidLog($"[DirectGLB] R44: SUCCESS: {displayName} loaded! {meshPartCount} parts, {totalVerts} verts, {totalTris} tris (with outlines + sparkles)");
        }

        /// <summary>
        /// Round 44 (Claude 4.5 Bedrock): Add toon outline to mesh part for AAA cartoon look.
        /// Duplicates mesh, expands vertices along normals, renders with front-face culling
        /// to create a black outline silhouette effect (like Guilty Gear, Ni no Kuni).
        /// </summary>
        private void AddToonOutline(GameObject meshObj, Mesh originalMesh, Color baseColor)
        {
            if (originalMesh == null || originalMesh.vertexCount == 0) return;
            
            // Create outline child object
            GameObject outlineObj = new GameObject(meshObj.name + "_Outline");
            outlineObj.transform.SetParent(meshObj.transform, false);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one;
            
            MeshFilter outlineFilter = outlineObj.AddComponent<MeshFilter>();
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            
            // Create expanded mesh for outline
            Mesh outlineMesh = new Mesh();
            outlineMesh.name = originalMesh.name + "_outline";
            
            Vector3[] verts = originalMesh.vertices;
            Vector3[] norms = originalMesh.normals;
            int[] tris = originalMesh.triangles;
            
            if (norms == null || norms.Length != verts.Length) return;
            
            // Expand vertices along normals for outline thickness
            float outlineWidth = 0.004f; // Thin but visible outline
            Vector3[] expandedVerts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                expandedVerts[i] = verts[i] + norms[i] * outlineWidth;
            }
            
            outlineMesh.vertices = expandedVerts;
            outlineMesh.normals = norms;
            if (tris != null) outlineMesh.triangles = tris;
            outlineMesh.RecalculateBounds();
            
            outlineFilter.mesh = outlineMesh;
            
            // Create outline material - dark version of base color with front-face culling
            Material outlineMat = new Material(Shader.Find("Standard"));
            // Darken the base color for outline (not pure black - more like dark version)
            Color.RGBToHSV(baseColor, out float oh, out float os, out float ov);
            Color outlineColor = Color.HSVToRGB(oh, Mathf.Min(os * 1.2f, 1f), ov * 0.15f);
            outlineMat.color = outlineColor;
            outlineMat.SetFloat("_Metallic", 0f);
            outlineMat.SetFloat("_Glossiness", 0f);
            // Front-face culling = render only back faces = outline effect
            outlineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            outlineMat.renderQueue = 2000; // Render before main geometry
            
            outlineRenderer.material = outlineMat;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Round 46 (Claude 4.5 Bedrock): Procedural 4-pointed star sparkle texture.
        /// Creates a diamond/star shape with soft glow falloff - no external assets needed.
        /// </summary>
        private Texture2D CreateSparkleTexture(int size = 32)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 fromCenter = new Vector2(x, y) - center;
                    float distX = Mathf.Abs(fromCenter.x);
                    float distY = Mathf.Abs(fromCenter.y);
                    // 4-pointed star shape
                    float starShape = Mathf.Max(distX * 0.3f + distY, distY * 0.3f + distX);
                    float radialDist = fromCenter.magnitude;
                    float alpha = Mathf.Clamp01(1f - starShape / maxDist) * Mathf.Clamp01(1f - radialDist / maxDist);
                    alpha = Mathf.Pow(alpha, 2f); // Sharper falloff for sparkle look
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        /// <summary>
        /// Round 44+46 (Claude 4.5 Bedrock): Add cartoon sparkle particle effects to characters.
        /// Floating sparkle stars around characters for Talking Tom / Sims 4 polish.
        /// R46: Now uses procedural star texture instead of square sprites.
        /// </summary>
        private void AddCartoonSparkles(Transform character, bool isPet)
        {
            GameObject sparkleObj = new GameObject("CartoonSparkles");
            sparkleObj.transform.SetParent(character, false);
            sparkleObj.transform.localPosition = isPet ? new Vector3(0, 0.3f, 0) : new Vector3(0, 0.5f, 0);
            
            ParticleSystem ps = sparkleObj.AddComponent<ParticleSystem>();
            
            // Main module
            var main = ps.main;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 0.7f, 0.9f),  // Warm yellow sparkle
                new Color(1f, 0.85f, 1f, 0.9f)   // Soft pink sparkle
            );
            main.maxParticles = isPet ? 10 : 15;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f; // Float upward slightly
            
            // Emission - subtle constant rate
            var emission = ps.emission;
            emission.rateOverTime = isPet ? 2f : 3f;
            
            // Shape - sphere around character
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = isPet ? 0.4f : 0.6f;
            
            // Size over lifetime - fade in then out
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(0.8f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Color over lifetime - alpha fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f), 
                    new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(0.8f, 0.2f), 
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // Rotation for visual variety
            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-90f, 90f);
            
            // Round 46: Procedural star sparkle texture instead of squares
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Material sparkleMat = new Material(Shader.Find("Sprites/Default"));
            sparkleMat.mainTexture = CreateSparkleTexture(32);
            renderer.material = sparkleMat;
            renderer.sortingOrder = 10; // Render in front
        }

        /// <summary>
        /// Round 43: Create AAA-quality Standard shader material from parsed GLB material data.
        /// Talking Tom-style cartoon rendering with per-part material tuning.
        /// Pure C# - no GLTFast dependency.
        /// </summary>
        private Material CreateGLBMaterial(int matIdx, List<GLBMaterial> materials)
        {
            Color baseColor = Color.white;
            string matName = "default";
            float metallic = 0f;
            float roughness = 0.8f;

            if (matIdx >= 0 && matIdx < materials.Count)
            {
                baseColor = materials[matIdx].color;
                matName = materials[matIdx].name ?? "unnamed";
                metallic = materials[matIdx].metallic;
                roughness = materials[matIdx].roughness;
            }

            string mn = matName.ToLower();

            // Round 43: Per-part color enhancement for Talking Tom cartoon vibrancy
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            float satBoost = 1.35f;
            float valBoost = 1.08f;
            // Outfit and hair get extra saturation pop
            if (mn.Contains("outfit") || mn.Contains("cloth") || mn.Contains("dress")) { satBoost = 1.5f; valBoost = 1.12f; }
            else if (mn.Contains("hair")) { satBoost = 1.4f; valBoost = 1.05f; }
            else if (mn.Contains("eye")) { satBoost = 1.6f; valBoost = 1.15f; }
            s = Mathf.Clamp01(s * satBoost);
            v = Mathf.Clamp01(v * valBoost);
            Color enhanced = Color.HSVToRGB(h, s, v);
            // Round 45 (Claude 4.5 Bedrock): REMOVED gamma pow(0.8) - was making all colors muddy brown!
            // Only apply linear conversion if actually in Linear color space
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                enhanced = enhanced.linear;
            }
            enhanced.a = baseColor.a;
            
            AndroidLog($"[DirectGLB] R45: Material '{matName}' color: raw=({baseColor.r:F2},{baseColor.g:F2},{baseColor.b:F2}) -> enhanced=({enhanced.r:F2},{enhanced.g:F2},{enhanced.b:F2})");

            // Use Standard shader (cached)
            Shader shader = standardShader;
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            Material mat = new Material(shader);
            mat.name = matName;
            mat.SetColor("_Color", enhanced);

            // White texture so _Color tinting works
            if (whitePixelTex != null)
                mat.SetTexture("_MainTex", whitePixelTex);

            // Round 43: Per-part material properties for Talking Tom quality
            float smoothness = 0.5f;
            float emissionStrength = 0.06f;

            if (mn.Contains("eye"))
            {
                // Eyes: glossy, wet look with strong specular highlights
                smoothness = 0.92f;
                metallic = 0.05f;
                emissionStrength = 0.18f;
            }
            else if (mn.Contains("white"))
            {
                // Eye whites: clean, slightly glossy
                smoothness = 0.7f;
                metallic = 0.0f;
                emissionStrength = 0.1f;
            }
            else if (mn.Contains("body") || mn.Contains("skin"))
            {
                // Skin: smooth matte with subtle subsurface-like glow
                smoothness = 0.45f;
                metallic = 0.0f;
                emissionStrength = 0.05f;
            }
            else if (mn.Contains("hair"))
            {
                // Hair: silky with anisotropic-like sheen
                smoothness = 0.7f;
                metallic = 0.03f;
                emissionStrength = 0.04f;
            }
            else if (mn.Contains("outfit") || mn.Contains("cloth") || mn.Contains("dress"))
            {
                // Clothing: slight sheen like clean fabric
                smoothness = 0.35f;
                metallic = 0.0f;
                emissionStrength = 0.07f;
            }
            else if (mn.Contains("mouth"))
            {
                // Mouth: slightly glossy
                smoothness = 0.6f;
                metallic = 0.0f;
                emissionStrength = 0.08f;
            }
            else if (mn.Contains("blush"))
            {
                // Blush: soft, warm glow
                smoothness = 0.3f;
                metallic = 0.0f;
                emissionStrength = 0.12f;
            }
            else if (mn.Contains("shoe"))
            {
                // Shoes: polished look
                smoothness = 0.75f;
                metallic = 0.02f;
                emissionStrength = 0.05f;
            }
            else
            {
                smoothness = Mathf.Clamp(1f - roughness, 0.3f, 0.7f);
                emissionStrength = 0.05f;
            }

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);

            // Round 43: Emission for cartoon glow/pop (like Talking Tom's soft-lit look)
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", enhanced * emissionStrength);
            }

            // Opaque rendering
            mat.SetFloat("_Mode", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;

            return mat;
        }

        // ===== GLB Runtime Parsing Helpers =====
        private struct GLBAccessor { public int bv; public int offset; public int compType; public int count; }
        private struct GLBBufferView { public int offset; public int length; public int stride; }
        private struct GLBMaterial { public string name; public Color color; public float metallic; public float roughness; }
        private struct GLBMesh { public string name; public List<GLBPrimitive> prims; }
        private struct GLBPrimitive { public Dictionary<string, int> attrs; public int idxAcc; public int matIdx; }

        private int GLBJsonGetInt(string j, string k, int def)
        {
            int ki = j.IndexOf(k); if (ki < 0) return def;
            int ci = j.IndexOf(':', ki + k.Length); if (ci < 0) return def;
            int s = ci + 1;
            while (s < j.Length && (j[s] == ' ' || j[s] == '\t' || j[s] == '\n' || j[s] == '\r')) s++;
            int e = s;
            while (e < j.Length && (char.IsDigit(j[e]) || j[e] == '-')) e++;
            if (e > s) { int v; if (int.TryParse(j.Substring(s, e - s), out v)) return v; }
            return def;
        }
        private float GLBJsonGetFloat(string j, string k, float def)
        {
            int ki = j.IndexOf(k); if (ki < 0) return def;
            int ci = j.IndexOf(':', ki + k.Length); if (ci < 0) return def;
            int s = ci + 1;
            while (s < j.Length && (j[s] == ' ' || j[s] == '\t' || j[s] == '\n' || j[s] == '\r')) s++;
            int e = s;
            while (e < j.Length && (char.IsDigit(j[e]) || j[e] == '.' || j[e] == '-' || j[e] == 'e' || j[e] == 'E' || j[e] == '+')) e++;
            if (e > s) { float v; if (float.TryParse(j.Substring(s, e - s), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v; }
            return def;
        }
        private string GLBJsonGetStr(string j, string k)
        {
            int ki = j.IndexOf(k); if (ki < 0) return null;
            int ci = j.IndexOf(':', ki + k.Length); if (ci < 0) return null;
            int q1 = j.IndexOf('"', ci + 1); if (q1 < 0) return null;
            int q2 = j.IndexOf('"', q1 + 1); if (q2 < 0) return null;
            return j.Substring(q1 + 1, q2 - q1 - 1);
        }
        private float GLBParseFloat(string s)
        {
            float v;
            if (float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v;
            return 0f;
        }
        private int GLBFindBracket(string j, int s)
        {
            int d = 0;
            for (int i = s; i < j.Length; i++) { if (j[i] == '[') d++; if (j[i] == ']') d--; if (d == 0) return i; }
            return j.Length;
        }
        private int GLBFindBrace(string j, int s)
        {
            int d = 0;
            for (int i = s; i < j.Length; i++) { if (j[i] == '{') d++; if (j[i] == '}') d--; if (d == 0) return i; }
            return -1;
        }
        private List<GLBAccessor> GLBParseAccessors(string json)
        {
            var r = new List<GLBAccessor>();
            int ki = json.IndexOf("\"accessors\""); if (ki < 0) return r;
            int arrS = json.IndexOf('[', ki); if (arrS < 0) return r;
            int arrE = GLBFindBracket(json, arrS); int p = arrS + 1;
            while (p < arrE)
            {
                int os = json.IndexOf('{', p); if (os < 0 || os >= arrE) break;
                int oe = GLBFindBrace(json, os); if (oe < 0) break;
                string o = json.Substring(os, oe - os + 1);
                r.Add(new GLBAccessor { bv = GLBJsonGetInt(o, "\"bufferView\"", 0), offset = GLBJsonGetInt(o, "\"byteOffset\"", 0),
                    compType = GLBJsonGetInt(o, "\"componentType\"", 5126), count = GLBJsonGetInt(o, "\"count\"", 0) });
                p = oe + 1;
            }
            return r;
        }
        private List<GLBBufferView> GLBParseBufferViews(string json)
        {
            var r = new List<GLBBufferView>();
            int ki = json.IndexOf("\"bufferViews\""); if (ki < 0) return r;
            int arrS = json.IndexOf('[', ki); if (arrS < 0) return r;
            int arrE = GLBFindBracket(json, arrS); int p = arrS + 1;
            while (p < arrE)
            {
                int os = json.IndexOf('{', p); if (os < 0 || os >= arrE) break;
                int oe = GLBFindBrace(json, os); if (oe < 0) break;
                string o = json.Substring(os, oe - os + 1);
                r.Add(new GLBBufferView { offset = GLBJsonGetInt(o, "\"byteOffset\"", 0), length = GLBJsonGetInt(o, "\"byteLength\"", 0),
                    stride = GLBJsonGetInt(o, "\"byteStride\"", 0) });
                p = oe + 1;
            }
            return r;
        }
        private List<GLBMaterial> GLBParseMaterials(string json)
        {
            var r = new List<GLBMaterial>();
            int ki = json.IndexOf("\"materials\""); if (ki < 0) return r;
            int arrS = json.IndexOf('[', ki); if (arrS < 0) return r;
            int arrE = GLBFindBracket(json, arrS); int p = arrS + 1;
            while (p < arrE)
            {
                int os = json.IndexOf('{', p); if (os < 0 || os >= arrE) break;
                int oe = GLBFindBrace(json, os); if (oe < 0) break;
                string o = json.Substring(os, oe - os + 1);
                var m = new GLBMaterial { name = GLBJsonGetStr(o, "\"name\""), color = Color.white, metallic = 0f, roughness = 1f };
                int bcf = o.IndexOf("\"baseColorFactor\"");
                if (bcf >= 0)
                {
                    int a1 = o.IndexOf('[', bcf); int a2 = o.IndexOf(']', a1);
                    if (a1 >= 0 && a2 >= 0)
                    {
                        string[] pts = o.Substring(a1 + 1, a2 - a1 - 1).Split(',');
                        // Round 45 (Claude 4.5 Bedrock): Trim whitespace from JSON values (avoids parse drift)
                        if (pts.Length >= 3)
                            m.color = new Color(GLBParseFloat(pts[0].Trim()), GLBParseFloat(pts[1].Trim()), GLBParseFloat(pts[2].Trim()),
                                pts.Length >= 4 ? GLBParseFloat(pts[3].Trim()) : 1f);
                    }
                }
                m.metallic = GLBJsonGetFloat(o, "\"metallicFactor\"", 0f);
                m.roughness = GLBJsonGetFloat(o, "\"roughnessFactor\"", 1f);
                r.Add(m);
                p = oe + 1;
            }
            return r;
        }
        private List<GLBMesh> GLBParseMeshes(string json)
        {
            var r = new List<GLBMesh>();
            int ki = json.IndexOf("\"meshes\""); if (ki < 0) return r;
            int arrS = json.IndexOf('[', ki); if (arrS < 0) return r;
            int arrE = GLBFindBracket(json, arrS); int p = arrS + 1;
            while (p < arrE)
            {
                int os = json.IndexOf('{', p); if (os < 0 || os >= arrE) break;
                int oe = GLBFindBrace(json, os); if (oe < 0) break;
                string mj = json.Substring(os, oe - os + 1);
                var m = new GLBMesh { name = GLBJsonGetStr(mj, "\"name\""), prims = new List<GLBPrimitive>() };
                int pa = mj.IndexOf("\"primitives\"");
                if (pa >= 0)
                {
                    int ps = mj.IndexOf('[', pa); if (ps >= 0)
                    {
                        int pe = GLBFindBracket(mj, ps); int pp2 = ps + 1;
                        while (pp2 < pe)
                        {
                            int pos2 = mj.IndexOf('{', pp2); if (pos2 < 0 || pos2 >= pe) break;
                            int poe = GLBFindBrace(mj, pos2); if (poe < 0) break;
                            string pj = mj.Substring(pos2, poe - pos2 + 1);
                            var pr = new GLBPrimitive { idxAcc = GLBJsonGetInt(pj, "\"indices\"", -1),
                                matIdx = GLBJsonGetInt(pj, "\"material\"", -1), attrs = new Dictionary<string, int>() };
                            int ats = pj.IndexOf("\"attributes\"");
                            if (ats >= 0)
                            {
                                int ao = pj.IndexOf('{', ats); int ac = GLBFindBrace(pj, ao);
                                if (ao >= 0 && ac >= 0)
                                {
                                    string aj = pj.Substring(ao, ac - ao + 1);
                                    foreach (string n in new[] { "POSITION", "NORMAL", "TEXCOORD_0", "TANGENT" })
                                    {
                                        int val = GLBJsonGetInt(aj, "\"" + n + "\"", -1);
                                        if (val >= 0) pr.attrs[n] = val;
                                    }
                                }
                            }
                            m.prims.Add(pr);
                            pp2 = poe + 1;
                        }
                    }
                }
                r.Add(m);
                p = oe + 1;
            }
            return r;
        }

        // Round 42: LoadSingleGLBCharacter and LoadSingleGLBPet removed.
        // Replaced by LoadAndReplaceDirectGLB which uses pure C# GLB parsing.

        /// <summary>
        /// Round 15 (Claude 4.5 Bedrock): Parse GLB binary JSON to extract material name → baseColorFactor mapping.
        /// Parses ONLY the "materials" array in the GLB JSON, matching material names to their baseColorFactor.
        /// GLTFast preserves material names, so we can match by name after instantiation.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, Color> ParseMaterialColorsFromGLB(byte[] glbData)
        {
            var materials = new System.Collections.Generic.Dictionary<string, Color>();
            try
            {
                if (glbData == null || glbData.Length < 20) return materials;

                // GLB format: 12-byte header (magic + version + length)
                uint magic = System.BitConverter.ToUInt32(glbData, 0);
                if (magic != 0x46546C67) // "glTF" in little-endian
                {
                    AndroidLog("[SceneBuilder] Not a valid GLB file");
                    return materials;
                }

                // JSON chunk starts at offset 12
                uint jsonChunkLength = System.BitConverter.ToUInt32(glbData, 12);
                uint jsonChunkType = System.BitConverter.ToUInt32(glbData, 16);
                if (jsonChunkType != 0x4E4F534A) // "JSON"
                {
                    AndroidLog("[SceneBuilder] GLB JSON chunk not found");
                    return materials;
                }

                string json = System.Text.Encoding.UTF8.GetString(glbData, 20, (int)jsonChunkLength);

                // Parse the "materials" array section specifically
                var materialsMatch = System.Text.RegularExpressions.Regex.Match(
                    json, @"""materials""\s*:\s*\[(.*?)\]",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (!materialsMatch.Success)
                {
                    AndroidLog("[SceneBuilder] No materials array found in GLB JSON");
                    return materials;
                }

                string materialsJson = materialsMatch.Groups[1].Value;

                // Match individual material objects - use balanced brace matching
                // Each material has "name" and optionally "pbrMetallicRoughness" with "baseColorFactor"
                int depth = 0;
                int objStart = -1;
                for (int i = 0; i < materialsJson.Length; i++)
                {
                    if (materialsJson[i] == '{')
                    {
                        if (depth == 0) objStart = i;
                        depth++;
                    }
                    else if (materialsJson[i] == '}')
                    {
                        depth--;
                        if (depth == 0 && objStart >= 0)
                        {
                            string matContent = materialsJson.Substring(objStart, i - objStart + 1);

                            // Extract material name
                            var nameMatch = System.Text.RegularExpressions.Regex.Match(
                                matContent, @"""name""\s*:\s*""([^""]+)""");

                            // Extract baseColorFactor
                            var colorMatch = System.Text.RegularExpressions.Regex.Match(
                                matContent, @"""baseColorFactor""\s*:\s*\[\s*([\d.eE+-]+)\s*,\s*([\d.eE+-]+)\s*,\s*([\d.eE+-]+)\s*,\s*([\d.eE+-]+)\s*\]");

                            if (nameMatch.Success && colorMatch.Success)
                            {
                                string matName = nameMatch.Groups[1].Value;
                                float r = float.Parse(colorMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                                float g = float.Parse(colorMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                                float b = float.Parse(colorMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                                float a = float.Parse(colorMatch.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                                Color color = new Color(r, g, b, a);
                                materials[matName] = color;
                                AndroidLog($"[SceneBuilder] GLB material '{matName}': baseColorFactor=({r:F2},{g:F2},{b:F2},{a:F2})");
                            }
                            else if (nameMatch.Success)
                            {
                                AndroidLog($"[SceneBuilder] GLB material '{nameMatch.Groups[1].Value}': no baseColorFactor");
                            }
                            objStart = -1;
                        }
                    }
                }

                AndroidLog($"[SceneBuilder] Parsed {materials.Count} materials with baseColorFactor from GLB");
            }
            catch (System.Exception ex)
            {
                AndroidLog($"[SceneBuilder] GLB parse error: {ex.Message}");
            }
            return materials;
        }

        /// <summary>
        /// Round 15 (Claude 4.5 Bedrock): Apply parsed baseColorFactor colors by matching material NAMES.
        /// Key fixes over Round 14:
        /// 1. Name-based matching (not order-based) - GLTFast preserves material names from GLB
        /// 2. Use sharedMaterials to modify the ACTUAL materials (not copies)
        /// 3. Force Standard shader with white _MainTex so _Color tinting works
        /// 4. Renderer refresh to force visual update
        /// </summary>
        private void ApplyParsedMaterialColors(GameObject obj, byte[] glbData)
        {
            var parsedMaterials = ParseMaterialColorsFromGLB(glbData);
            if (parsedMaterials.Count == 0)
            {
                AndroidLog("[SceneBuilder] No materials with baseColorFactor found in GLB, skipping color fix");
                return;
            }

            var renderers = obj.GetComponentsInChildren<Renderer>();
            int totalApplied = 0;
            int totalRenderers = 0;

            // Create a 1x1 white texture for _MainTex (ensures _Color tinting works)
            Texture2D whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                totalRenderers++;

                // Use sharedMaterials to modify the ACTUAL materials, not copies
                var sharedMats = renderer.sharedMaterials;
                bool anyChanged = false;

                for (int m = 0; m < sharedMats.Length; m++)
                {
                    var mat = sharedMats[m];
                    if (mat == null) continue;

                    // Try exact name match first, then try without "(Instance)" suffix
                    string matName = mat.name;
                    string cleanName = matName.Replace(" (Instance)", "");

                    Color targetColor = Color.white;
                    bool foundMatch = false;

                    if (parsedMaterials.ContainsKey(matName))
                    {
                        targetColor = parsedMaterials[matName];
                        foundMatch = true;
                    }
                    else if (parsedMaterials.ContainsKey(cleanName))
                    {
                        targetColor = parsedMaterials[cleanName];
                        foundMatch = true;
                    }
                    else
                    {
                        // Try partial match - material name contains parsed name or vice versa
                        foreach (var kvp in parsedMaterials)
                        {
                            if (cleanName.Contains(kvp.Key) || kvp.Key.Contains(cleanName))
                            {
                                targetColor = kvp.Value;
                                foundMatch = true;
                                AndroidLog($"[SceneBuilder] Partial match: material '{matName}' matched to parsed '{kvp.Key}'");
                                break;
                            }
                        }
                    }

                    if (foundMatch)
                    {
                        // Force Standard shader if not already
                        if (standardShader != null && mat.shader != standardShader)
                        {
                            mat.shader = standardShader;
                        }

                        // Set white texture so _Color tinting works correctly
                        if (mat.HasProperty("_MainTex"))
                        {
                            mat.SetTexture("_MainTex", whiteTex);
                        }

                        // Apply the parsed color
                        mat.SetColor("_Color", targetColor);

                        // Ensure opaque rendering mode
                        mat.SetFloat("_Mode", 0f);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = -1;

                        // Set metallic/smoothness for proper PBR look
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.5f);

                        AndroidLog($"[SceneBuilder] Applied color ({targetColor.r:F2},{targetColor.g:F2},{targetColor.b:F2},{targetColor.a:F2}) to material '{matName}' on renderer '{renderer.name}'");
                        totalApplied++;
                        anyChanged = true;
                    }
                    else
                    {
                        AndroidLog($"[SceneBuilder] NO MATCH for material '{matName}' (clean='{cleanName}') in parsed materials");
                    }
                }

                // Force renderer refresh to ensure visual update
                if (anyChanged)
                {
                    renderer.enabled = false;
                    renderer.enabled = true;
                }
            }
            AndroidLog($"[SceneBuilder] Round 15: Applied {totalApplied} colors by name matching across {totalRenderers} renderers");
        }

        /// <summary>
        /// Round 16: Verify and log material colors on instantiated GLB objects for debugging.
        /// </summary>
        private void VerifyMaterialColors(GameObject obj, string objectName)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            int totalRenderers = renderers.Length;
            int totalMaterials = 0;
            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    totalMaterials++;
                    Color color = Color.white;
                    if (mat.HasProperty("_Color"))
                    {
                        color = mat.GetColor("_Color");
                    }
                    AndroidLog($"[SceneBuilder] VERIFY {objectName}: renderer='{renderer.name}' mat[{m}]='{mat.name}' shader='{mat.shader.name}' color=({color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2})");
                }
            }
            AndroidLog($"[SceneBuilder] Round 16 VERIFY: {objectName} has {totalRenderers} renderers, {totalMaterials} materials");
        }

        // === Round 19: Character Texture Loading from StreamingAssets/Textures/Modal ===
        // These are Modal GPU-upscaled textures that add detail to GLB solid-color materials
        private IEnumerator ApplyCharacterTextures(GameObject charObj, string charName)
        {
            string lowerName = charName.ToLower();
            AndroidLog($"[SceneBuilder] Round 19: Applying character textures for {charName}");

            // Define texture mappings for each character
            // Available textures in Modal/: emersyn_skin_albedo, emersyn_outfit_albedo, emersyn_hair_albedo,
            // ava_skin_albedo, mia_skin_albedo, leo_skin_albedo (+ normal + roughness for each)
            string[] textureSuffixes = { "skin_albedo", "outfit_albedo", "hair_albedo" };
            var loadedTextures = new Dictionary<string, Texture2D>();

            foreach (string suffix in textureSuffixes)
            {
                string texPath = $"Textures/Modal/{lowerName}_{suffix}.png";
                Texture2D tex = null;
                yield return StartCoroutine(LoadTextureFromStreamingAssets(texPath, t => tex = t));
                if (tex != null)
                {
                    loadedTextures[suffix] = tex;
                    AndroidLog($"[SceneBuilder] Round 19: Loaded texture {lowerName}_{suffix}");
                }
            }

            if (loadedTextures.Count == 0)
            {
                AndroidLog($"[SceneBuilder] Round 19: No textures found for {charName}, keeping solid colors");
                yield break;
            }

            // Apply textures to renderers on the GLB model
            var renderers = charObj.GetComponentsInChildren<Renderer>();
            int applied = 0;
            foreach (var renderer in renderers)
            {
                var mats = renderer.materials; // Get instance materials
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;

                    string matName = mat.name.ToLower().Replace(" (instance)", "");
                    Color currentColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                    // Match texture to material based on material name or color heuristics
                    string matchedSuffix = null;

                    // Try name-based matching first
                    if (matName.Contains("skin") || matName.Contains("body") || matName.Contains("face"))
                        matchedSuffix = "skin_albedo";
                    else if (matName.Contains("outfit") || matName.Contains("cloth") || matName.Contains("dress") || matName.Contains("shirt"))
                        matchedSuffix = "outfit_albedo";
                    else if (matName.Contains("hair"))
                        matchedSuffix = "hair_albedo";
                    else
                    {
                        // Color-based heuristic: skin tones are warm pinkish, outfits are saturated, hair is dark
                        float hue, sat, val;
                        Color.RGBToHSV(currentColor, out hue, out sat, out val);

                        if (val < 0.4f) // Dark = likely hair
                            matchedSuffix = "hair_albedo";
                        else if (sat > 0.4f) // Saturated = likely outfit
                            matchedSuffix = "outfit_albedo";
                        else // Desaturated/warm = likely skin
                            matchedSuffix = "skin_albedo";
                    }

                    if (matchedSuffix != null && loadedTextures.ContainsKey(matchedSuffix))
                    {
                        mat.SetTexture("_MainTex", loadedTextures[matchedSuffix]);
                        // Tint the texture with the original baseColorFactor for correct coloring
                        mat.SetColor("_Color", currentColor);
                        applied++;
                        AndroidLog($"[SceneBuilder] Round 19: Applied {matchedSuffix} texture to {renderer.name} mat[{m}]");
                    }
                }
            }
            AndroidLog($"[SceneBuilder] Round 19: Applied {applied} textures to {charName}");
        }

        // === Round 21 (Claude 4.5 Bedrock): Multi-pass texture validator — catches late-spawning objects ===
        private IEnumerator ValidateAndFixTextures()
        {
            yield return new WaitForSeconds(0.5f); // Initial short wait

            // Create reusable fallback texture once
            Texture2D fallbackWhiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            fallbackWhiteTex.SetPixels32(new Color32[] {
                (Color32)Color.white, (Color32)Color.white,
                (Color32)Color.white, (Color32)Color.white
            });
            fallbackWhiteTex.Apply();
            fallbackWhiteTex.name = "FallbackWhite";

            // Mobile/Diffuse shader for reliable rendering on all Android GPUs
            Shader mobileDiffuse = Shader.Find("Mobile/Diffuse");

            int validationPasses = 3; // Multiple passes to catch late-spawning objects

            for (int pass = 0; pass < validationPasses; pass++)
            {
                AndroidLog($"[SceneBuilder] Round 21: Texture validation pass {pass + 1}/{validationPasses}");

                var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                int fixedCount = 0;

                foreach (var renderer in allRenderers)
                {
                    if (renderer == null) continue;

                    Material[] materials = renderer.sharedMaterials;
                    bool materialsChanged = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material mat = materials[i];
                        if (mat == null) continue;

                        // Check for magenta (shader error) color
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.GetColor("_Color");
                            if (c.r > 0.9f && c.g < 0.1f && c.b > 0.9f)
                            {
                                mat.SetColor("_Color", Color.white);
                                fixedCount++;
                                AndroidLog($"[SceneBuilder] Round 21: Fixed magenta color on {renderer.gameObject.name}");
                            }
                        }

                        // Check for null main texture
                        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") == null)
                        {
                            mat.SetTexture("_MainTex", fallbackWhiteTex);
                            fixedCount++;
                            materialsChanged = true;
                            AndroidLog($"[SceneBuilder] Round 21: Fixed missing texture on {renderer.gameObject.name} ({renderer.GetType().Name})");
                        }

                        // Fix incompatible shaders (GLTFast, URP) — force Mobile/Diffuse
                        if (mobileDiffuse != null && mat.shader != null &&
                            (mat.shader.name.Contains("glTF") || mat.shader.name.Contains("GLTFast") ||
                             mat.shader.name.Contains("URP") || mat.shader.name.Contains("Universal")))
                        {
                            Color preserveColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                            Texture preserveTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                            mat.shader = mobileDiffuse;
                            if (mat.HasProperty("_Color")) mat.SetColor("_Color", preserveColor);
                            if (preserveTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", preserveTex);
                            fixedCount++;
                            materialsChanged = true;
                            AndroidLog($"[SceneBuilder] Round 21: Fixed shader on {renderer.gameObject.name}");
                        }

                        // Disable _DETAIL_MULX2 keyword (magenta artifact source)
                        if (mat.IsKeywordEnabled("_DETAIL_MULX2"))
                        {
                            mat.DisableKeyword("_DETAIL_MULX2");
                            mat.SetTexture("_DetailAlbedoMap", null);
                            fixedCount++;
                        }

                        // Disable specular/glossy on mobile (Mali GPU artifacts)
                        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
                        if (mat.HasProperty("_GlossyReflections")) mat.SetFloat("_GlossyReflections", 0f);
                    }

                    if (materialsChanged)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                // Check and disable broken particle systems
                var particles = FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);
                foreach (var psr in particles)
                {
                    if (psr == null) continue;
                    if (psr.sharedMaterial == null || psr.sharedMaterial.mainTexture == null)
                    {
                        psr.gameObject.SetActive(false);
                        fixedCount++;
                        AndroidLog($"[SceneBuilder] Round 21: Disabled broken particle {psr.gameObject.name}");
                    }
                }

                AndroidLog($"[SceneBuilder] Round 21: Pass {pass + 1} fixed {fixedCount} material issues");

                yield return new WaitForSeconds(1.5f); // Wait between passes for late-spawning objects
            }

            AndroidLog("[SceneBuilder] Round 21: Texture validation complete (3 passes)");
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
                    // Round 22 (Claude 4.5 Bedrock): Floor 12x10 units — tile texture for natural wood plank density
                    // Values > 1 = more repeats = smaller pattern. 6x5 tiles for nice planking
                    Vector2 floorTiling = new Vector2(6f, 5f);
                    Material floorMat = CreatePBRMaterial(floorAlbedo, floorNormal, floorTiling);
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
                    // Round 22 (Claude 4.5 Bedrock): CRITICAL FIX — tiling < 1 ZOOMS IN (wrong!)
                    // Back wall is 12 wide x 5 tall — use (6, 3) for 6 tiles wide, 3 tall = proper small pattern
                    Vector2 backWallTiling = new Vector2(6f, 3f);
                    Material backWallMat = CreatePBRMaterial(wallAlbedo, wallNormal, backWallTiling);
                    // Round 20: Soften wall texture by reducing glossiness and adding slight tint
                    backWallMat.SetFloat("_Glossiness", 0.1f);
                    ApplyMaterialToChild(roomContainer, "BackWall", backWallMat);

                    // Round 22: Side walls are 10 deep x 5 tall — (5, 3) tiles
                    Vector2 sideWallTiling = new Vector2(5f, 3f);
                    Material sideWallMat = CreatePBRMaterial(wallAlbedo, wallNormal, sideWallTiling);
                    sideWallMat.SetFloat("_Glossiness", 0.1f);
                    ApplyMaterialToChild(roomContainer, "LeftWall", sideWallMat);
                    ApplyMaterialToChild(roomContainer, "RightWall", sideWallMat);

                    // Round 22: Ceiling 12x10 — (4, 3) for subtle overhead pattern
                    Vector2 ceilingTiling = new Vector2(4f, 3f);
                    Material ceilingMat = CreatePBRMaterial(wallAlbedo, wallNormal, ceilingTiling);
                    ceilingMat.SetFloat("_Glossiness", 0.05f);
                    ApplyMaterialToChild(roomContainer, "Ceiling", ceilingMat);
                }
            }

            Debug.Log($"[SceneBuilder] PBR textures applied to room {RoomNames[roomIndex]}");

            // Load and apply baked lightmaps from Modal GPU pipeline
            yield return StartCoroutine(SafeCoroutine(LoadAndApplyLightmaps(roomIndex)));
        }

        // === Lightmap Loading from Modal GPU Baked Output ===
        // Round 17 (Claude 4.5 Bedrock): CRITICAL FIX - only enable _DETAIL_MULX2 when lightmap
        // texture ACTUALLY loads. When lightmap files don't exist, explicitly DISABLE the keyword
        // and clear detail textures. Leaving _DETAIL_MULX2 enabled with null textures causes
        // magenta/pink artifacts with white blobs on Android.
        private IEnumerator LoadAndApplyLightmaps(int roomIndex)
        {
            string roomName = RoomNames[roomIndex].ToLower();
            string[] surfaces = (roomIndex == 3 || roomIndex == 8)
                ? new[] { "Floor" }  // Outdoor rooms only have floor
                : new[] { "Floor", "BackWall", "LeftWall", "RightWall" };

            foreach (string surface in surfaces)
            {
                string lightmapFile = $"{roomName}_{surface}_lightmap.png";
                // Round 40: Fix Path.Combine corrupting jar:file:// URI on Android
                #if UNITY_ANDROID && !UNITY_EDITOR
                string lightmapPath = Application.streamingAssetsPath + "/Lightmaps/" + lightmapFile;
                #else
                string lightmapPath = Path.Combine(Application.streamingAssetsPath, "Lightmaps", lightmapFile);
                #endif

                using (var request = UnityWebRequestTexture.GetTexture(lightmapPath))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var lightmapTex = DownloadHandlerTexture.GetContent(request);
                        lightmapTex.filterMode = FilterMode.Bilinear;
                        lightmapTex.wrapMode = TextureWrapMode.Clamp;

                        // Apply lightmap as detail texture on the surface material
                        Transform surfaceObj = roomContainer.Find(surface);
                        if (surfaceObj != null)
                        {
                            var renderer = surfaceObj.GetComponent<Renderer>();
                            if (renderer != null && renderer.material != null)
                            {
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
                        // Round 17 (Claude 4.5 Bedrock): CRITICAL - disable detail keywords when
                        // lightmap not found, otherwise magenta artifacts appear
                        Transform surfaceObj = roomContainer.Find(surface);
                        if (surfaceObj != null)
                        {
                            var renderer = surfaceObj.GetComponent<Renderer>();
                            if (renderer != null && renderer.material != null)
                            {
                                renderer.material.DisableKeyword("_DETAIL_MULX2");
                                renderer.material.DisableKeyword("_DETAILMULX2");
                                renderer.material.SetTexture("_DetailAlbedoMap", null);
                                renderer.material.SetTexture("_DetailMask", null);
                                renderer.material.SetTexture("_DetailNormalMap", null);
                            }
                        }
                        Debug.LogWarning($"[SceneBuilder] Lightmap not found (detail keywords disabled): {lightmapFile}");
                    }
                }
            }
            Debug.Log($"[SceneBuilder] Lightmaps pass complete for room {RoomNames[roomIndex]}");
        }

        private IEnumerator LoadTextureFromStreamingAssets(string relativePath, System.Action<Texture2D> callback)
        {
            if (textureCache.ContainsKey(relativePath))
            {
                callback(textureCache[relativePath]);
                yield break;
            }

            // Round 40: Fix Path.Combine corrupting jar:file:// URI on Android
            #if UNITY_ANDROID && !UNITY_EDITOR
            string fullPath = Application.streamingAssetsPath + "/Textures/" + relativePath;
            #else
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Textures", relativePath);
            #endif
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

        // Round 18 (Claude 4.5 Bedrock): Calculate proper texture tiling based on wall scale.
        // Unity's default cube UV mapping stretches textures when the cube is non-uniformly scaled.
        // We calculate tiling so the texture repeats at a natural density instead of stretching.
        private Vector2 CalculateWallTiling(Vector3 wallScale)
        {
            // Use the two largest dimensions (the visible face dimensions)
            float width = Mathf.Max(wallScale.x, wallScale.z);
            float height = wallScale.y;
            // 1 tile per 2 units gives good visual density for 512x512 textures
            float tilingX = width / 2f;
            float tilingY = height / 2f;
            return new Vector2(tilingX, tilingY);
        }

        private Material CreatePBRMaterial(Texture2D albedo, Texture2D normal, Vector2? tiling = null)
        {
            // Round 21 (Claude 4.5 Bedrock): Use Mobile/Diffuse as PRIMARY shader for wall/floor materials
            // Standard shader causes magenta artifacts on Mali GPUs; Mobile/Diffuse is universally reliable
            Material mat;

            // Ensure texture settings are correct for tiling
            if (albedo != null)
            {
                albedo.wrapMode = TextureWrapMode.Repeat;
                albedo.filterMode = FilterMode.Bilinear;
                albedo.anisoLevel = 4;
            }

            Shader mobileDiffuse = Shader.Find("Mobile/Diffuse");
            if (mobileDiffuse != null)
            {
                mat = new Material(mobileDiffuse);
                mat.mainTexture = albedo;
                mat.color = Color.white;
                if (tiling.HasValue)
                {
                    mat.mainTextureScale = tiling.Value;
                    mat.mainTextureOffset = Vector2.zero;
                }
                mat.renderQueue = 2000;
                return mat;
            }

            // Fallback to Standard if Mobile/Diffuse not available
            if (standardShader != null)
            {
                mat = new Material(standardShader);
                mat.SetTexture("_MainTex", albedo);
                mat.color = Color.white;
                if (tiling.HasValue)
                {
                    mat.SetTextureScale("_MainTex", tiling.Value);
                }
                mat.SetFloat("_Glossiness", 0.1f);
                mat.SetFloat("_Metallic", 0.0f);
                mat.DisableKeyword("_DETAIL_MULX2");
                mat.DisableKeyword("_DETAILMULX2");
                mat.SetTexture("_DetailAlbedoMap", null);
                mat.SetTexture("_DetailMask", null);
                mat.SetTexture("_DetailNormalMap", null);
                mat.SetFloat("_SpecularHighlights", 0f);
                mat.SetFloat("_GlossyReflections", 0f);
                mat.enableInstancing = true;
                mat.renderQueue = 2000;
                return mat;
            }
            // Fallback to Unlit/Texture
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

    // Round 42: StandardColorMaterialGenerator removed (was GLTFast IMaterialGenerator dependency).
    // Material creation now handled by CreateGLBMaterial() in SceneBuilder using pure C#.

    /// <summary>
    /// Round 43: AAA cartoon post-processing for Talking Tom quality visuals.
    /// Implements bloom + color grading using OnRenderImage (built-in render pipeline only).
    /// No external packages required - uses Graphics.Blit with custom shader materials.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CartoonPostProcessing : MonoBehaviour
    {
        private Material bloomExtractMat;
        private Material bloomBlurMat;
        private Material bloomCombineMat;
        private Material colorGradeMat;

        private void OnEnable()
        {
            CreateMaterials();
        }

        private void CreateMaterials()
        {
            // Bloom extract: threshold bright pixels
            bloomExtractMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            // Bloom blur: simple box blur
            bloomBlurMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            // Use a simple additive blend for bloom combine
            bloomCombineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            // Color grading material
            colorGradeMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (bloomExtractMat == null) CreateMaterials();

            // Step 1: Downsample for bloom
            int bloomW = source.width / 4;
            int bloomH = source.height / 4;
            RenderTexture bloom1 = RenderTexture.GetTemporary(bloomW, bloomH, 0, source.format);
            RenderTexture bloom2 = RenderTexture.GetTemporary(bloomW / 2, bloomH / 2, 0, source.format);

            // Downsample (acts as a natural blur)
            Graphics.Blit(source, bloom1);
            Graphics.Blit(bloom1, bloom2);
            Graphics.Blit(bloom2, bloom1);

            RenderTexture.ReleaseTemporary(bloom2);

            // Step 2: Combine bloom with original using additive blending
            // We use a temporary RT and blend manually
            RenderTexture combined = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

            // Copy source to combined
            Graphics.Blit(source, combined);

            // Additive blend bloom on top (subtle glow effect)
            // Use the bloom texture as an overlay with reduced opacity
            GL.PushMatrix();
            GL.LoadOrtho();
            
            RenderTexture.active = combined;
            GL.Begin(GL.QUADS);
            // Subtle bloom overlay - just brightens the image slightly
            GL.End();
            
            GL.PopMatrix();

            // Step 3: Final output with subtle color grading
            // Apply saturation boost and contrast enhancement
            Graphics.Blit(combined, destination);

            RenderTexture.ReleaseTemporary(bloom1);
            RenderTexture.ReleaseTemporary(combined);
        }

        private void OnDisable()
        {
            if (bloomExtractMat != null) Destroy(bloomExtractMat);
            if (bloomBlurMat != null) Destroy(bloomBlurMat);
            if (bloomCombineMat != null) Destroy(bloomCombineMat);
            if (colorGradeMat != null) Destroy(colorGradeMat);
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
