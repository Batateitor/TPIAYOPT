using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightmapBakeRunner
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level2.unity"
    };

    private static readonly string[] Level2Only =
    {
        "Assets/Scenes/Level2.unity"
    };

    [MenuItem("Tools/Bake Level Lightmaps")]
    public static void BakeFromMenu()
    {
        BakeScenes();
    }

    public static void RunBatch()
    {
        try
        {
            BakeScenes();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void RunLevel2Batch()
    {
        try
        {
            BakeSceneSet(Level2Only);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void BakeScenes()
    {
        BakeSceneSet(ScenePaths);
    }

    private static void BakeSceneSet(string[] scenePaths)
    {
        foreach (string scenePath in scenePaths)
        {
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException($"Scene not found for lightmap bake: {scenePath}");
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"Baking lightmap for {scenePath}...");

            ConfigureActiveSceneLightsForBake();
            Lightmapping.Clear();

            if (!Lightmapping.Bake())
            {
                throw new InvalidOperationException($"Unity could not bake lightmap for {scenePath}");
            }

            WaitForBakeToFinish(scenePath);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Finished lightmap bake for {scenePath}.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureActiveSceneLightsForBake()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        int changed = 0;

        foreach (Light light in lights)
        {
            if (light == null || !light.enabled || light.intensity <= 0f)
            {
                continue;
            }

            if (light.lightmapBakeType != LightmapBakeType.Mixed)
            {
                light.lightmapBakeType = LightmapBakeType.Mixed;
                changed++;
            }

            if (light.shadows == LightShadows.None && light.type != LightType.Directional)
            {
                light.shadows = LightShadows.Soft;
                changed++;
            }

            EditorUtility.SetDirty(light);
        }

        if (changed > 0)
        {
            Debug.Log($"Prepared {changed} light setting(s) for mixed lighting.");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static void WaitForBakeToFinish(string scenePath)
    {
        DateTime startedAt = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(30);

        while (Lightmapping.isRunning)
        {
            if (DateTime.UtcNow - startedAt > timeout)
            {
                Lightmapping.Cancel();
                throw new TimeoutException($"Lightmap bake timed out for {scenePath}");
            }

            Thread.Sleep(1000);
        }
    }
}
