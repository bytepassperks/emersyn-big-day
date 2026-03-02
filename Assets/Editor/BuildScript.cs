using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string buildPath = "Builds/EmersynsBigDay.apk";
        
        // Ensure build directory exists
        string dir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Get all scenes in build settings, or use MainScene
        string[] scenes = new string[] { "Assets/Scenes/MainScene.unity" };
        
        // Check if scene file exists
        if (!File.Exists(scenes[0]))
        {
            Debug.Log("[BuildScript] MainScene.unity not found, creating empty scene list");
            scenes = new string[0];
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // Set Android settings
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        Debug.Log("[BuildScript] Starting Android APK build...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build succeeded! APK at: {buildPath} ({report.summary.totalSize / (1024*1024)} MB)");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build FAILED: {report.summary.totalErrors} errors");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        Debug.LogError($"  {msg.content}");
                }
            }
            EditorApplication.Exit(1);
        }
    }
}
