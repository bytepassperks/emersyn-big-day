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

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("[BUILD] BuildAndroid called - waiting for editor to stabilize...");
        _frameCount = 0;
        _buildStarted = false;
        EditorApplication.update += WaitAndBuild;
    }

    private static void WaitAndBuild()
    {
        _frameCount++;
        if (_frameCount >= 30 && !_buildStarted)
        {
            _buildStarted = true;
            EditorApplication.update -= WaitAndBuild;
            Debug.Log($"[BUILD] Editor stabilized after {_frameCount} frames, starting build...");
            ExecuteBuild();
        }
        if (_frameCount > 600 && !_buildStarted)
        {
            EditorApplication.update -= WaitAndBuild;
            Debug.LogError("[BUILD] Timeout waiting for editor to stabilize");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Unity 6 headless batch mode workaround: ensure the ScriptAssemblies
    /// output directory is set before BuildPipeline runs, preventing the
    /// segfault in BuildPipeline::AppendTargetAssembliesFromManagedAssemblies.
    /// </summary>
    private static void EnsureCompileScriptsOutputDirectory()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir = Path.Combine(projectRoot, "Library", "ScriptAssemblies");
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        try
        {
            // Try to call EditorCompilationInterface.SetCompileScriptsOutputDirectory via reflection
            Type editorCompType = typeof(Editor).Assembly.GetType(
                "UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
            if (editorCompType == null)
            {
                Debug.Log("[BUILD] EditorCompilationInterface not found, skipping workaround");
                return;
            }

            MethodInfo setDirMethod = editorCompType.GetMethod(
                "SetCompileScriptsOutputDirectory",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (setDirMethod != null)
            {
                setDirMethod.Invoke(null, new object[] { outputDir });
                Debug.Log($"[BUILD] SetCompileScriptsOutputDirectory({outputDir}) called successfully");
                return;
            }

            // Fallback: try instance method
            PropertyInfo instanceProp = editorCompType.GetProperty(
                "Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instanceProp != null)
            {
                object instance = instanceProp.GetValue(null);
                if (instance != null)
                {
                    MethodInfo instMethod = instance.GetType().GetMethod(
                        "SetCompileScriptsOutputDirectory",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (instMethod != null)
                    {
                        instMethod.Invoke(instance, new object[] { outputDir });
                        Debug.Log($"[BUILD] Instance SetCompileScriptsOutputDirectory called");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BUILD] EnsureCompileScriptsOutputDirectory warning: {ex.Message}");
        }
    }

    private static void ExecuteBuild()
    {
        var startTime = DateTime.Now;

        // Phase 1: Apply Unity 6 headless workaround
        EnsureCompileScriptsOutputDirectory();

        // Phase 1b: Force asset reimport after pipeline switch (per Claude expert guidance)
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[BUILD] AssetDatabase.Refresh(ForceUpdate) completed");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string buildDir = Path.Combine(projectRoot, "Builds");
        string buildPath = Path.Combine(buildDir, "EmersynsBigDay.apk");
        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        // Discover scenes
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            string[] candidates = {
                "Assets/Scenes/MainScene.unity",
                "Assets/Scenes/LoadingScene.unity"
            };
            scenes = candidates.Where(s =>
                File.Exists(Path.Combine(projectRoot, s))).ToArray();
        }
        if (scenes.Length == 0)
        {
            Debug.LogError("[BUILD] No scenes found! Aborting.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log($"[BUILD] Scenes: {string.Join(", ", scenes)}");

        // Configure Android settings
        PlayerSettings.SetApplicationIdentifier(
            UnityEditor.Build.NamedBuildTarget.Android, "com.bytepassperks.emersynsbigday");
        PlayerSettings.companyName = "BytePassPerks";
        PlayerSettings.productName = "Emersyn's Big Day";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

        // Use IL2CPP backend per Claude guidance - Mono has known 'script class layout incompatible' bug
        // IL2CPP is required for Google Play 64-bit, better performance, fixes all serialization issues
        PlayerSettings.SetScriptingBackend(
            UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        // ARM64 only (ARMv7 deprecated, IL2CPP + ARM64 is Google Play standard)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Enable unsafe code (required for IL2CPP with pointer fields in InputSystem/TMP)
        PlayerSettings.allowUnsafeCode = true;

        Debug.Log($"[BUILD] Backend: IL2CPP, Target arch: ARM64, allowUnsafeCode: true");

        // Suppress stack traces during build to avoid massive log overhead from URP shader warnings
        var prevWarningTrace = Application.GetStackTraceLogType(LogType.Warning);
        var prevLogTrace = Application.GetStackTraceLogType(LogType.Log);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

        try
        {
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
                // Unity 6 IL2CPP workaround: disable determinism consistency checks
                // that produce false positives for Nullable<T>, void*, HashSet<T> fields
                extraScriptingDefines = new[] { "IL2CPP_DISABLE_CONSISTENCY_CHECKS" }
            };

            Debug.Log("[BUILD] Calling BuildPipeline.BuildPlayer...");
            BuildReport report = BuildPipeline.BuildPlayer(opts);

            // Restore stack trace settings
            Application.SetStackTraceLogType(LogType.Warning, prevWarningTrace);
            Application.SetStackTraceLogType(LogType.Log, prevLogTrace);

            BuildSummary summary = report.summary;
            double minutes = (DateTime.Now - startTime).TotalMinutes;

            Debug.Log($"[BUILD] Result: {summary.result}");
            Debug.Log($"[BUILD] Size: {summary.totalSize / (1024 * 1024)} MB");
            Debug.Log($"[BUILD] Errors: {summary.totalErrors}");
            Debug.Log($"[BUILD] Time: {minutes:F1} min");

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BUILD] SUCCESS - APK at: {buildPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BUILD] FAILED with {summary.totalErrors} errors");
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error)
                            Debug.LogError($"  {msg.content}");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Application.SetStackTraceLogType(LogType.Warning, prevWarningTrace);
            Application.SetStackTraceLogType(LogType.Log, prevLogTrace);
            Debug.LogError($"[BUILD] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}
