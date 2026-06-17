using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild
{
    public static void BuildApk()
    {
        PlayerSettings.applicationIdentifier = "com.promptbox.downfall";
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        Directory.CreateDirectory(outputDirectory);

        var options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/CombatScene.unity",
                "Assets/Scenes/DialogueScene.unity",
                "Assets/Scenes/GearUpScene.unity"
            },
            locationPathName = Path.Combine(outputDirectory, "Match3Adventure.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Android APK build failed: {report.summary.result}");
        }

        Debug.Log($"Android APK build succeeded: {options.locationPathName}");
    }
}
