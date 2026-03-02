using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class BuildScript
{
    private static int _frameCount = 0;
    private static bool _buildStarted = false;

    // Wait this many editor frames before building to let Unity fully initialize.
    // The initial bee_backend compilation completes during project open.
    // We must NOT request recompilation via CompilationPipeline as that triggers
    // the Unity 6 SetCompileScriptsOutputDirectory bug in headless batch mode.
    private const int FRAMES_BEFORE_BUILD = 30;

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("BuildScript.BuildAndroid called - waiting for editor stabilization...");

        _frameCount = 0;
        _buildStarted = false;

        // Register update callback - fires every editor frame
        EditorApplication.update += OnEditorUpdate;
    }

    /// <summary>
    /// Direct build method for -quit batch mode (no frame waiting).
    /// Use: -executeMethod BuildScript.BuildDirect
    /// </summary>
    public static void BuildDirect()
    {
        Debug.Log("BuildScript.BuildDirect called - building immediately...");
        DoBuild();
    }

    private static void OnEditorUpdate()
    {
        _frameCount++;

        if (_frameCount == FRAMES_BEFORE_BUILD && !_buildStarted)
        {
            Debug.Log($"Frame {_frameCount}: Editor stabilized, starting build...");
            _buildStarted = true;
            EditorApplication.update -= OnEditorUpdate;
            DoBuild();
        }

        // Safety timeout
        if (_frameCount > 500 && !_buildStarted)
        {
            Debug.LogError($"Frame {_frameCount}: Timeout. Aborting.");
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Workaround for Unity 6 headless batch mode bug where
    /// SetCompileScriptsOutputDirectory is never called internally,
    /// causing a segfault in BuildPipeline::GetCompatibleTargetAssemblies.
    /// Uses reflection to call the internal method before BuildPlayer.
    /// </summary>
    private static void EnsureCompileScriptsOutputDirectory()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir = Path.Combine(projectRoot, "Library", "ScriptAssemblies");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            // Get EditorCompilationInterface type (internal class)
            Type editorCompInterfaceType = Type.GetType(
                "UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface, UnityEditor");

            if (editorCompInterfaceType == null)
            {
                Debug.LogWarning("Could not find EditorCompilationInterface type");
                return;
            }

            // Get the EditorCompilation instance via the Instance property
            PropertyInfo instanceProp = editorCompInterfaceType.GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (instanceProp == null)
            {
                // Try getting it via a field instead
                FieldInfo instanceField = editorCompInterfaceType.GetField(
                    "instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (instanceField != null)
                {
                    object instance = instanceField.GetValue(null);
                    if (instance != null)
                    {
                        CallSetOutputDir(instance, outputDir);
                        return;
                    }
                }

                // Try listing all members to find the right one
                Debug.Log("Searching EditorCompilationInterface members...");
                MemberInfo[] members = editorCompInterfaceType.GetMembers(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MemberInfo member in members)
                {
                    Debug.Log($"  Member: {member.MemberType} {member.Name}");
                }
                return;
            }

            object compInstance = instanceProp.GetValue(null);
            if (compInstance == null)
            {
                Debug.LogWarning("EditorCompilation instance is null");
                return;
            }

            CallSetOutputDir(compInstance, outputDir);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"EnsureCompileScriptsOutputDirectory reflection failed: {ex.Message}");
        }
    }

    private static void CallSetOutputDir(object editorCompilation, string outputDir)
    {
        Type compType = editorCompilation.GetType();

        // Try SetCompileScriptsOutputDirectory
        MethodInfo setDirMethod = compType.GetMethod(
            "SetCompileScriptsOutputDirectory",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (setDirMethod != null)
        {
            Debug.Log($"Calling SetCompileScriptsOutputDirectory({outputDir})...");
            setDirMethod.Invoke(editorCompilation, new object[] { outputDir });
            Debug.Log("SetCompileScriptsOutputDirectory called successfully!");
        }
        else
        {
            Debug.LogWarning("Could not find SetCompileScriptsOutputDirectory method");
            // List available methods for debugging
            MethodInfo[] methods = compType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo m in methods)
            {
                if (m.Name.Contains("Compile") || m.Name.Contains("Output") || m.Name.Contains("Directory"))
                {
                    Debug.Log($"  Available method: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                }
            }
        }
    }

    private static void DoBuild()
    {
        var buildStartTime = DateTime.Now;
        Debug.Log($"[BUILD] ========== BUILD STARTED at {buildStartTime:HH:mm:ss} ==========");

        // Phase 1: Compile scripts output directory
        Debug.Log("[BUILD] Phase 1/8: Ensuring compile scripts output directory...");
        EnsureCompileScriptsOutputDirectory();
        Debug.Log($"[BUILD] Phase 1/8 DONE ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed)");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDir = Path.Combine(projectRoot, "Builds");
        string buildPath = Path.Combine(buildDir, "EmersynsBigDay.apk");

        Debug.Log($"[BUILD] Project root: {projectRoot}");
        Debug.Log($"[BUILD] Build output: {buildPath}");

        // Log render pipeline info
        Debug.Log($"[BUILD] Current Render Pipeline: {UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name ?? "Built-in (null)"}");
        Debug.Log($"[BUILD] Default Render Pipeline: {UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline?.name ?? "Built-in (null)"}");
        Debug.Log($"[BUILD] Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");

        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
            Debug.Log($"[BUILD] Created build directory: {buildDir}");
        }

        // Phase 2: Discover scenes
        Debug.Log("[BUILD] Phase 2/8: Discovering scenes...");
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            string[] candidateScenes = new string[]
            {
                "Assets/Scenes/LoadingScene.unity",
                "Assets/Scenes/MainScene.unity",
                "Assets/Scenes/Rooms/BedroomScene.unity",
                "Assets/Scenes/Rooms/KitchenScene.unity",
                "Assets/Scenes/Rooms/BathroomScene.unity",
                "Assets/Scenes/Rooms/LivingRoomScene.unity",
                "Assets/Scenes/Rooms/GardenScene.unity",
                "Assets/Scenes/Rooms/SchoolScene.unity",
                "Assets/Scenes/Rooms/ShopScene.unity",
                "Assets/Scenes/Rooms/PlaygroundScene.unity",
                "Assets/Scenes/Rooms/ParkScene.unity",
                "Assets/Scenes/Rooms/MallScene.unity",
                "Assets/Scenes/Rooms/ArcadeScene.unity",
                "Assets/Scenes/Rooms/AmusementParkScene.unity"
            };

            scenes = candidateScenes
                .Where(s => File.Exists(Path.Combine(projectRoot, s)))
                .ToArray();
        }

        if (scenes.Length == 0)
        {
            Debug.LogError("[BUILD] FATAL: No scenes found to build! Aborting.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[BUILD] Phase 2/8 DONE - Found {scenes.Length} scenes ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed):");
        foreach (string scene in scenes)
        {
            Debug.Log($"[BUILD]   Scene: {scene}");
        }

        // Phase 3: Pre-initialize PlayerSettings
        Debug.Log("[BUILD] Phase 3/8: Pre-initializing PlayerSettings...");
        // WORKAROUND: Force-initialize the EditorOnlyPlayerSettings scripting backend map
        try
        {
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            var backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android);
            Debug.Log($"[BUILD] Phase 3/8 DONE - ScriptingBackend: {backend} ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed)");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BUILD] Phase 3/8 WARNING: PlayerSettings pre-init failed: {ex.Message}");
        }

        // Phase 4: Configure Android settings
        Debug.Log("[BUILD] Phase 4/8: Configuring Android settings...");
        PlayerSettings.SetApplicationIdentifier(
            UnityEditor.Build.NamedBuildTarget.Android, "com.bytepassperks.emersynsbigday");
        PlayerSettings.companyName = "BytePassPerks";
        PlayerSettings.productName = "Emersyn's Big Day";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        Debug.Log($"[BUILD] Phase 4/8 DONE ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed)");

        // Phase 5: Prepare build options
        Debug.Log("[BUILD] Phase 5/8: Preparing build options...");
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
#if UNITY_2021_2_OR_NEWER
            subtarget = (int)0
#endif
        };
        Debug.Log($"[BUILD] Phase 5/8 DONE ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed)");

        // Phase 6: BuildPlayer (shader compilation + IL2CPP + Gradle)
        Debug.Log("[BUILD] Phase 6/8: Starting BuildPipeline.BuildPlayer...");
        Debug.Log("[BUILD]   This phase includes: shader compilation, IL2CPP C++ compilation, Gradle APK packaging");
        Debug.Log("[BUILD]   Expected duration: 8-15 minutes");

        // CRITICAL FIX: Suppress stack traces for warnings/logs during build.
        // URP shader stripping in batch mode generates 100K+ "Failed to write file" warnings,
        // each with a full C# stack trace. This logging overhead adds 20+ min to the build.
        // Suppressing stack traces keeps the warnings but removes the expensive trace generation.
        var prevWarningTrace = Application.GetStackTraceLogType(LogType.Warning);
        var prevLogTrace = Application.GetStackTraceLogType(LogType.Log);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Debug.Log("[BUILD]   Stack trace logging suppressed for shader compilation phase");

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            // Restore stack trace logging after build
            Application.SetStackTraceLogType(LogType.Warning, prevWarningTrace);
            Application.SetStackTraceLogType(LogType.Log, prevLogTrace);
            BuildSummary summary = report.summary;
            Debug.Log($"[BUILD] Phase 6/8 DONE ({(DateTime.Now - buildStartTime).TotalSeconds:F1}s elapsed)");

            // Phase 7: Analyze build report
            Debug.Log("[BUILD] Phase 7/8: Analyzing build report...");
            Debug.Log($"[BUILD]   Result: {summary.result}");
            Debug.Log($"[BUILD]   Total size: {summary.totalSize} bytes ({summary.totalSize / (1024 * 1024)} MB)");
            Debug.Log($"[BUILD]   Total errors: {summary.totalErrors}");
            Debug.Log($"[BUILD]   Total warnings: {summary.totalWarnings}");
            Debug.Log($"[BUILD]   Build time: {summary.totalTime}");

            // Log all build steps with timing
            Debug.Log("[BUILD]   Build steps:");
            foreach (var step in report.steps)
            {
                Debug.Log($"[BUILD]     Step: {step.name} (duration: {step.duration})");
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    {
                        Debug.LogError($"[BUILD]       ERROR [{step.name}]: {msg.content}");
                    }
                }
            }

            // Phase 8: Exit
            if (summary.result == BuildResult.Succeeded)
            {
                var totalTime = DateTime.Now - buildStartTime;
                Debug.Log($"[BUILD] ========== BUILD SUCCEEDED in {totalTime.TotalMinutes:F1} minutes ==========");
                Debug.Log($"[BUILD] APK at: {buildPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BUILD] ========== BUILD FAILED with {summary.totalErrors} errors ==========");
                EditorApplication.Exit(1);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BUILD] ========== BUILD EXCEPTION ==========");
            Debug.LogError($"[BUILD] Exception: {ex.Message}");
            Debug.LogError($"[BUILD] StackTrace: {ex.StackTrace}");
            Debug.LogError($"[BUILD] Elapsed: {(DateTime.Now - buildStartTime).TotalSeconds:F1}s");
            EditorApplication.Exit(1);
        }
    }
}
