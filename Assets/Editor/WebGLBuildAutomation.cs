using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildAutomation
{
    private const string OutputPath = "Build/WebGL";

    [MenuItem("Tools/Build/WebGL/Build Local Dev")]
    public static void BuildLocalDevFromMenu()
    {
        BuildLocalDev();
    }

    [MenuItem("Tools/Build/WebGL/Build Release")]
    public static void BuildReleaseFromMenu()
    {
        BuildRelease();
    }

    public static void BuildLocalDev()
    {
        ApplyWebGLSizePreset.ApplyLocalDevFromBatchmode();
        Build("LOCAL_DEV");
    }

    public static void BuildRelease()
    {
        ApplyWebGLSizePreset.ApplyReleaseFromBatchmode();
        Build("RELEASE");
    }

    private static void Build(string profileName)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in Build Settings.");
        }

        Directory.CreateDirectory(OutputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            target = BuildTarget.WebGL,
            locationPathName = OutputPath,
            options = BuildOptions.None
        };

        Debug.Log($"[WebGLBuildAutomation] Building {profileName} to: {Path.GetFullPath(OutputPath)}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception(
                $"WebGL build failed ({profileName}). Result={summary.result}, Errors={summary.totalErrors}, Warnings={summary.totalWarnings}");
        }

        Debug.Log(
            $"[WebGLBuildAutomation] Build succeeded ({profileName}). Size={summary.totalSize} bytes, Output={summary.outputPath}");
    }
}
