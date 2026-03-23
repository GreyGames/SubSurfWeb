using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ApplyWebGLSizePreset
{
    [MenuItem("Tools/Build/Apply Sub-10MB WebGL Preset")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    public static void ApplyFromBatchmode()
    {
        Apply();
    }

    private static void Apply()
    {
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.gcIncremental = false;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#pragma warning disable 618
        PlayerSettings.WebGL.debugSymbols = false;
#pragma warning restore 618
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.threadsSupport = false;

        SetOptionalWebGLBool("wasm2023", true);

        AssetDatabase.SaveAssets();
        Debug.Log("[ApplyWebGLSizePreset] Applied WebGL min-size settings.");
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
