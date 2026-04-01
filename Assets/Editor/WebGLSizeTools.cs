using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WebGLSizeTools
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/SubSurfWeb/WebGL/Optimize Asset Importers (Aggressive)")]
    public static void OptimizeAssetImporters()
    {
        OptimizeTextures();
        OptimizeAudio();
        OptimizeModels();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WebGLSizeTools] Optimization pass complete.");
    }

    [MenuItem("Tools/SubSurfWeb/WebGL/Report Scene Dependency Sizes")]
    public static void ReportSceneDependencySizes()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning($"[WebGLSizeTools] Scene not found: {ScenePath}");
            return;
        }

        string[] deps = AssetDatabase.GetDependencies(ScenePath, true);
        var rows = deps
            .Where(p => !p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Where(File.Exists)
            .Select(p => new FileInfo(p))
            .OrderByDescending(fi => fi.Length)
            .Select(fi => $"{(fi.Length / 1048576f):0.00} MB  {fi.FullName.Replace('\\', '/')}")
            .ToArray();

        string reportDir = "Assets/BuildReports";
        if (!AssetDatabase.IsValidFolder(reportDir))
        {
            AssetDatabase.CreateFolder("Assets", "BuildReports");
        }

        string reportPath = Path.Combine(reportDir, "SceneDependencySizes.txt");
        File.WriteAllLines(reportPath, rows);

        AssetDatabase.ImportAsset(reportPath);
        Debug.Log($"[WebGLSizeTools] Wrote report: {reportPath}");
    }

    private static void OptimizeTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.StartsWith("Assets/Editor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            var settings = importer.GetPlatformTextureSettings("WebGL");
            settings.overridden = true;
            settings.maxTextureSize = 512;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 50;

            bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
            settings.format = hasAlpha
                ? TextureImporterFormat.ETC2_RGBA8
                : TextureImporterFormat.ETC2_RGB4;

            importer.SetPlatformTextureSettings(settings);

            // Keep mipmaps for non‑sprite textures; disable for sprites to save size.
            importer.mipmapEnabled = importer.textureType != TextureImporterType.Sprite;

            importer.SaveAndReimport();
        }
    }

    private static void OptimizeAudio()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.StartsWith("Assets/Editor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            var webgl = importer.GetOverrideSampleSettings("WebGL");
            webgl.compressionFormat = AudioCompressionFormat.Vorbis;
            webgl.quality = 0.35f;

            long bytes = new FileInfo(path).Length;
            webgl.loadType = bytes > (1024 * 1024)
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.CompressedInMemory;

            importer.SetOverrideSampleSettings("WebGL", webgl);
            importer.SaveAndReimport();
        }
    }

    private static void OptimizeModels()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.StartsWith("Assets/Editor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.optimizeMesh = true;

            // Keep optimizeGameObjects off to avoid breaking animation hierarchies.
            importer.optimizeGameObjects = false;

            importer.SaveAndReimport();
        }
    }
}
