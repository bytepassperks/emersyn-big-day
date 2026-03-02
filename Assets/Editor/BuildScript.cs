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
        // CRITICAL: Call the internal SetCompileScriptsOutputDirectory to prevent
        // segfault in BuildPipeline::GetCompatibleTargetAssemblies
        EnsureCompileScriptsOutputDirectory();
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDir = Path.Combine(projectRoot, "Builds");
        string buildPath = Path.Combine(buildDir, "EmersynsBigDay.apk");

        Debug.Log($"Project root: {projectRoot}");
        Debug.Log($"Build output: {buildPath}");

        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
            Debug.Log($"Created build directory: {buildDir}");
        }

        // Get all scenes from EditorBuildSettings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // If no scenes in build settings, use our known scenes
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
            Debug.LogError("No scenes found to build! Aborting.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"Building Android APK with {scenes.Length} scenes:");
        foreach (string scene in scenes)
        {
            Debug.Log($"  Scene: {scene}");
        }

        // WORKAROUND: Force-initialize the EditorOnlyPlayerSettings scripting backend map
        // by calling SetScriptingBackend BEFORE BuildPlayer. The native map at offset 0x24d0
        // is uninitialized in batch mode, causing a segfault in GetPlatformScriptingBackend.
        // Calling Set first may lazily initialize the map via a different native code path.
        try
        {
            Debug.Log("Pre-initializing PlayerSettings for Android...");
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("SetScriptingBackend succeeded.");
            
            // Also pre-read to test if the map is now accessible
            var backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android);
            Debug.Log($"GetScriptingBackend returned: {backend}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PlayerSettings pre-init failed: {ex.Message}");
        }

        // Configure Android settings
        PlayerSettings.SetApplicationIdentifier(
            UnityEditor.Build.NamedBuildTarget.Android, "com.bytepassperks.emersynsbigday");
        PlayerSettings.companyName = "BytePassPerks";
        PlayerSettings.productName = "Emersyn's Big Day";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

        // CRITICAL: Do NOT set targetGroup. In Unity 6, BuildTargetGroup is deprecated.
        // Setting it triggers the broken EditorOnlyPlayerSettings code path in batch mode,
        // causing a segfault in GetPlatformScriptingBackend (addr:0x24d0).
        // game-ci/unity-builder also omits targetGroup — Unity infers it from target.
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

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, output: {buildPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"Build failed with {summary.totalErrors} errors");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError($"  [{step.name}] {msg.content}");
                        }
                    }
                }
                EditorApplication.Exit(1);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Build threw exception: {ex.Message}\n{ex.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}
