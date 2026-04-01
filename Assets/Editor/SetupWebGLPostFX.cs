using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupWebGLPostFX
{
    private const string SetupKey = "SubSurfWeb_WebGLPostFXSetup_v1";
    private const string UrpAssetPath = "Assets/Settings/URP/SubSurfWeb_WebGL_URP.asset";
    private const string UrpRendererPath = "Assets/Settings/URP/SubSurfWeb_WebGL_Renderer.asset";
    private const string VolumeProfilePath = "Assets/Settings/URP/SubSurfWeb_WebGL_VolumeProfile.asset";
    private const string SlowMoMaterialPath = "Assets/Settings/URP/SubSurfWeb_SlowMoZone_Unlit.mat";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/SubSurfWeb/Setup WebGL Post FX")]
    public static void RunSetup()
    {
        EnsureFolders();
        UniversalRenderPipelineAsset urp = EnsureUrpAsset();
        EnsureRendererData(urp);
        AssignPipeline(urp);
        EnsureGlobalVolume();
        EnsureSlowMoZoneMaterial();
        EnableCameraPostFX();

        EditorPrefs.SetBool(SetupKey, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [InitializeOnLoadMethod]
    private static void AutoSetup()
    {
        if (EditorPrefs.GetBool(SetupKey, false))
        {
            return;
        }

        // Only auto-run if the package is available.
        if (typeof(UniversalRenderPipelineAsset) == null)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(SetupKey, false))
            {
                return;
            }

            RunSetup();
        };
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Settings/URP"))
        {
            AssetDatabase.CreateFolder("Assets/Settings", "URP");
        }
    }

    private static UniversalRenderPipelineAsset EnsureUrpAsset()
    {
        UniversalRenderPipelineAsset existing =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (existing != null)
        {
            return existing;
        }

        UniversalRenderPipelineAsset created = UniversalRenderPipelineAsset.Create();
        if (created == null)
        {
            throw new System.InvalidOperationException("Failed to create URP asset.");
        }

        AssetDatabase.CreateAsset(created, UrpAssetPath);
        return created;
    }

    private static void EnsureRendererData(UniversalRenderPipelineAsset urp)
    {
        if (urp == null)
        {
            return;
        }

        ScriptableRendererData rendererData =
            AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(UrpRendererPath);
        if (rendererData == null)
        {
            // Create a default Universal Renderer Data asset.
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            if (rendererData == null)
            {
                throw new System.InvalidOperationException("Failed to create URP Renderer Data.");
            }
            AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
        }

        SerializedObject so = new SerializedObject(urp);
        SerializedProperty listProp = so.FindProperty("m_RendererDataList");
        SerializedProperty defaultIndexProp = so.FindProperty("m_DefaultRendererIndex");
        if (listProp != null)
        {
            listProp.arraySize = 1;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
        }
        if (defaultIndexProp != null)
        {
            defaultIndexProp.intValue = 0;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignPipeline(UniversalRenderPipelineAsset urp)
    {
        if (urp == null)
        {
            return;
        }

        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;
    }

    private static void EnsureSlowMoZoneMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(SlowMoMaterialPath);
        if (mat == null)
        {
            mat = new Material(shader)
            {
                name = "SubSurfWeb_SlowMoZone_Unlit"
            };
            Color c = new Color(0.2f, 0.85f, 1f, 0.5f);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", c);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", c);
            }
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c);
            }
            AssetDatabase.CreateAsset(mat, SlowMoMaterialPath);
        }

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
        if (sceneAsset == null)
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        bool dirty = false;
        TrackSegment[] segments = Object.FindObjectsOfType<TrackSegment>(true);
        for (int i = 0; i < segments.Length; i++)
        {
            SerializedObject so = new SerializedObject(segments[i]);
            SerializedProperty prop = so.FindProperty("slowMoVisualMaterial");
            if (prop != null && prop.objectReferenceValue != mat)
            {
                prop.objectReferenceValue = mat;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void EnsureGlobalVolume()
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
        if (sceneAsset == null)
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        GameObject volumeGo = GameObject.Find("Global Volume");
        if (volumeGo == null)
        {
            volumeGo = new GameObject("Global Volume");
        }

        Volume volume = volumeGo.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeGo.AddComponent<Volume>();
        }

        volume.isGlobal = true;
        volume.priority = 0f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            ColorAdjustments color;
            if (!profile.TryGet(out color))
            {
                color = profile.Add<ColorAdjustments>(true);
            }
            color.postExposure.Override(0.2f);
            color.contrast.Override(12f);
            color.saturation.Override(6f);

            Bloom bloom;
            if (!profile.TryGet(out bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(1.2f);
            bloom.scatter.Override(0.6f);

            Tonemapping tone;
            if (!profile.TryGet(out tone))
            {
                tone = profile.Add<Tonemapping>(true);
            }
            tone.mode.Override(TonemappingMode.ACES);

            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        volume.sharedProfile = profile;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnableCameraPostFX()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data == null)
        {
            data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        data.renderPostProcessing = true;
    }
}
