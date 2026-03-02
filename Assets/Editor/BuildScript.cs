using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string buildPath = "Builds/EmersynsBigDay.apk";
        string buildDir = Path.GetDirectoryName(buildPath);
        
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        // Get all scenes from EditorBuildSettings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // If no scenes in build settings, use our known scenes
        if (scenes.Length == 0)
        {
            scenes = new string[]
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
        }

        Debug.Log($"Building Android APK with {scenes.Length} scenes...");

        // Configure Android settings
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.bytepassperks.emersynsbigday");
        PlayerSettings.companyName = "BytePassPerks";
        PlayerSettings.productName = "Emersyn's Big Day";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

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
            EditorApplication.Exit(1);
        }
    }
}
