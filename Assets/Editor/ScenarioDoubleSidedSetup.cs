using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class ScenarioDoubleSidedSetup
{
    private const string MaterialFolder = "Assets/Materials/ScenarioDoubleSided";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level2.unity"
    };

    private static readonly string[] ScenarioModelPaths =
    {
        "Assets/Scenarios/level1-2.fbx",
        "Assets/Scenarios/level2-2 1.fbx"
    };

    [MenuItem("Tools/Scenario/Apply Double-Sided Materials")]
    public static void ApplyFromMenu()
    {
        ApplyToScenes();
    }

    public static void RunBatch()
    {
        try
        {
            ApplyToScenes();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ApplyToScenes()
    {
        EnsureMaterialFolder();

        HashSet<string> scenarioModels = new HashSet<string>(ScenarioModelPaths);
        Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();
        int changedRenderers = 0;

        foreach (string scenePath in ScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException($"Scene not found: {scenePath}");
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int sceneChanges = ApplyToOpenScene(scenarioModels, materialCache);

            if (sceneChanges > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changedRenderers += sceneChanges;
            }

            Debug.Log($"Double-sided scenario material pass for {scenePath}: {sceneChanges} renderer(s) updated.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Double-sided scenario material pass complete. Updated {changedRenderers} renderer(s).");
    }

    private static int ApplyToOpenScene(HashSet<string> scenarioModels, Dictionary<Material, Material> materialCache)
    {
        int changedRenderers = 0;
        MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;

            if (mesh == null || !scenarioModels.Contains(AssetDatabase.GetAssetPath(mesh)))
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];

                if (source == null)
                {
                    continue;
                }

                Material doubleSided = GetOrCreateDoubleSidedMaterial(source, materialCache);

                if (doubleSided != source)
                {
                    materials[i] = doubleSided;
                    changed = true;
                }
            }

            if (!changed)
            {
                continue;
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            EditorUtility.SetDirty(renderer);
            changedRenderers++;
        }

        return changedRenderers;
    }

    private static Material GetOrCreateDoubleSidedMaterial(Material source, Dictionary<Material, Material> materialCache)
    {
        if (materialCache.TryGetValue(source, out Material cached))
        {
            return cached;
        }

        string sourcePath = AssetDatabase.GetAssetPath(source);

        if (sourcePath.StartsWith(MaterialFolder, StringComparison.OrdinalIgnoreCase))
        {
            ConfigureDoubleSided(source);
            EditorUtility.SetDirty(source);
            materialCache[source] = source;
            return source;
        }

        string targetPath = GetDoubleSidedMaterialPath(source);
        Material doubleSided = AssetDatabase.LoadAssetAtPath<Material>(targetPath);

        if (doubleSided == null)
        {
            doubleSided = new Material(source)
            {
                name = Path.GetFileNameWithoutExtension(targetPath)
            };
            AssetDatabase.CreateAsset(doubleSided, targetPath);
        }
        else
        {
            doubleSided.CopyPropertiesFromMaterial(source);
        }

        ConfigureDoubleSided(doubleSided);
        EditorUtility.SetDirty(doubleSided);
        materialCache[source] = doubleSided;
        return doubleSided;
    }

    private static void ConfigureDoubleSided(Material material)
    {
        material.doubleSidedGI = true;

        SetCullModeOff(material, "_Cull");
        SetCullModeOff(material, "_CullMode");
        SetCullModeOff(material, "_CullModeForward");

        if (material.HasProperty("_DoubleSidedGI"))
        {
            material.SetFloat("_DoubleSidedGI", 1f);
        }
    }

    private static void SetCullModeOff(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, (float)CullMode.Off);
        }
    }

    private static string GetDoubleSidedMaterialPath(Material source)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string sourceAssetName = string.IsNullOrEmpty(sourcePath) ? "Runtime" : Path.GetFileNameWithoutExtension(sourcePath);
        string materialName = string.IsNullOrEmpty(source.name) ? "Material" : source.name;
        string fileName = $"{SanitizeFileName(sourceAssetName)}_{SanitizeFileName(materialName)}_DoubleSided.mat";

        return $"{MaterialFolder}/{fileName}";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "ScenarioDoubleSided");
        }
    }
}
