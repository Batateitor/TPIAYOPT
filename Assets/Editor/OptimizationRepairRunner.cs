using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OptimizationRepairRunner
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/guardEnemy.prefab";
    private const string PlayerModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";
    private const string EnemyModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_03.fbx";
    private const string PlayerMaterialPath = "Assets/BodyGuards/Meshes/Materials/Boduguard_01_D.mat";
    private const string EnemyMaterialPath = "Assets/BodyGuards/Meshes/Materials/Boduguard_03_D.mat";

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

    public static void RunBatch()
    {
        try
        {
            RepairBodyguardMaterialsAndAssignments();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Optimization/Repair Bodyguard Materials")]
    public static void RepairBodyguardMaterialsAndAssignments()
    {
        Material playerMaterial = ConfigureBodyguardMaterial("01", PlayerMaterialPath);
        Material enemyMaterial = ConfigureBodyguardMaterial("03", EnemyMaterialPath);

        int prefabAssignments = 0;
        prefabAssignments += AssignPrefabMaterial(PlayerPrefabPath, PlayerModelPath, playerMaterial);
        prefabAssignments += AssignPrefabMaterial(EnemyPrefabPath, EnemyModelPath, enemyMaterial);

        int sceneAssignments = 0;

        foreach (string scenePath in ScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int changes = 0;
            changes += AssignOpenSceneMaterial(PlayerModelPath, playerMaterial);
            changes += AssignOpenSceneMaterial(EnemyModelPath, enemyMaterial);

            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneAssignments += changes;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Repaired bodyguard materials. Prefab renderer assignments: {prefabAssignments}. Scene renderer assignments: {sceneAssignments}.");
    }

    public static void RepairScenarioModelImportersForLightmaps()
    {
        int changed = 0;

        foreach (string modelPath in ScenarioModelPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

            if (importer == null)
            {
                continue;
            }

            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.generateSecondaryUV = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"Prepared {changed} scenario model importer(s) for stable lightmaps.");
    }

    private static Material ConfigureBodyguardMaterial(string bodyguardNumber, string materialPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            throw new FileNotFoundException($"Bodyguard material not found: {materialPath}");
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader != null)
        {
            material.shader = litShader;
        }

        Texture2D albedo = LoadTexture($"Assets/BodyGuards/Textures/OptimizedMobile/Boduguard_{bodyguardNumber}_D_Mobile.jpg");
        Texture2D normal = LoadTexture($"Assets/BodyGuards/Textures/OptimizedMobile/Boduguard_{bodyguardNumber}_N_Mobile.png");
        Texture2D occlusion = LoadTexture($"Assets/BodyGuards/Textures/OptimizedMobile/Boduguard_{bodyguardNumber}_Ao_Mobile.jpg");
        Texture2D specular = LoadTexture($"Assets/BodyGuards/Textures/OptimizedMobile/Boduguard_{bodyguardNumber}_S_Mobile.png");

        SetTexture(material, "_BaseMap", albedo);
        SetTexture(material, "_MainTex", albedo);
        SetTexture(material, "_BumpMap", normal);
        SetTexture(material, "_OcclusionMap", occlusion);
        SetTexture(material, "_SpecGlossMap", specular);
        SetTexture(material, "_MetallicGlossMap", specular);

        SetFloat(material, "_WorkflowMode", 1f);
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_Smoothness", 0.45f);
        SetFloat(material, "_Glossiness", 0.45f);
        SetFloat(material, "_OcclusionStrength", 1f);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            throw new FileNotFoundException($"Bodyguard texture not found: {path}");
        }

        return texture;
    }

    private static int AssignPrefabMaterial(string prefabPath, string modelPath, Material material)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            int changes = AssignMaterialInHierarchy(prefabRoot, modelPath, material);

            if (changes > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }

            return changes;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int AssignOpenSceneMaterial(string modelPath, Material material)
    {
        int changes = 0;
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            changes += AssignMaterialInHierarchy(root, modelPath, material);
        }

        return changes;
    }

    private static int AssignMaterialInHierarchy(GameObject root, string modelPath, Material material)
    {
        int changes = 0;
        SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.sharedMesh == null || AssetDatabase.GetAssetPath(renderer.sharedMesh) != modelPath)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;

            if (materials.Length == 0)
            {
                materials = new[] { material };
            }

            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != material)
                {
                    materials[i] = material;
                    changed = true;
                }
            }

            if (!changed)
            {
                continue;
            }

            renderer.sharedMaterials = materials;
            renderer.updateWhenOffscreen = false;
            renderer.skinnedMotionVectors = false;
            EditorUtility.SetDirty(renderer);
            changes++;
        }

        return changes;
    }

    private static void SetTexture(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
