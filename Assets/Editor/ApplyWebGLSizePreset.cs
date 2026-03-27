using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ApplyWebGLSizePreset
{
    [MenuItem("Tools/Build/WebGL Preset/Release (Sub-10MB)")]
    public static void ApplyReleaseFromMenu()
    {
        ApplyRelease();
    }

    [MenuItem("Tools/Build/WebGL Preset/Local Dev (Chrome)")]
    public static void ApplyLocalDevFromMenu()
    {
        ApplyLocalDev();
    }

    // Kept for CI/batchmode (backward compatible entrypoint).
    public static void ApplyFromBatchmode()
    {
        ApplyRelease();
    }

    public static void ApplyReleaseFromBatchmode()
    {
        ApplyRelease();
    }

    public static void ApplyLocalDevFromBatchmode()
    {
        ApplyLocalDev();
    }

    private static void ApplyCommon()
    {
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.gcIncremental = false;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
        SetOptionalWebGLBool("wasm2023", true);
    }

    private static void ApplyRelease()
    {
        ApplyCommon();

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#pragma warning disable 618
        PlayerSettings.WebGL.debugSymbols = false;
#pragma warning restore 618
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;

        AssetDatabase.SaveAssets();
        Debug.Log("[ApplyWebGLSizePreset] Applied RELEASE WebGL preset (Brotli with decompression fallback).");
    }

    private static void ApplyLocalDev()
    {
        ApplyCommon();

        // Local-friendly settings so simple localhost servers work in Chrome.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
#pragma warning disable 618
        PlayerSettings.WebGL.debugSymbols = true;
#pragma warning restore 618
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;

        AssetDatabase.SaveAssets();
        Debug.Log("[ApplyWebGLSizePreset] Applied LOCAL DEV WebGL preset (no compression).");
    }

    private static void SetOptionalWebGLBool(string propertyName, bool value)
    {
        var webGlSettings = typeof(PlayerSettings).GetNestedType("WebGL", BindingFlags.Public);
        if (webGlSettings == null)
        {
            return;
        }

        var prop = webGlSettings.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (prop == null || prop.PropertyType != typeof(bool) || !prop.CanWrite)
        {
            return;
        }

        prop.SetValue(null, value);
    }
}
