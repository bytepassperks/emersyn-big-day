using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
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
        Debug.Log("[BUILD] BuildAndroid called - forcing script recompilation first...");
        
        // Claude Bedrock Round 7: Force recompile ALL assemblies to avoid stale cache
        CompilationPipeline.RequestScriptCompilation();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[BUILD] Script recompilation requested and AssetDatabase refreshed");
        
        ExecuteBuild();
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

        // Phase 0: Round 36 - Skip editor-time GLB conversion entirely.
        // GLB files are parsed at RUNTIME on Android via direct binary parser in SceneBuilder.
        // This avoids all batch mode AssetDatabase/PrefabUtility issues that failed in rounds 31-35.
        Debug.Log("[BUILD] Phase 0: GLB conversion skipped (Round 36: runtime parsing on Android)");
        Debug.Log("[BUILD] GLB files in StreamingAssets/Characters/ will be parsed at runtime");

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

        // Use IL2CPP backend - Google Play standard for 64-bit
        // TMP built-in source patched to replace HashSet<uint> with List<uint> per Claude 6th analysis
        PlayerSettings.SetScriptingBackend(
            UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        // ARM64 only (IL2CPP standard)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Disable managed stripping to prevent code stripping issues
        PlayerSettings.SetManagedStrippingLevel(
            UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Disabled);
        PlayerSettings.stripEngineCode = false;

        Debug.Log($"[BUILD] Backend: IL2CPP, Target arch: ARM64, Stripping: Disabled");

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
                options = BuildOptions.None
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
