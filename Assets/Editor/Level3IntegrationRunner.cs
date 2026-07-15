using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Level3IntegrationRunner
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3.unity";
    private const string Level3SceneName = "Level3";
    private const string Level3ModelPath = "Assets/Scenarios/level3 (1).fbx";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string BlackSkyboxPath = "Assets/Materials/BlackSkybox.mat";
    private const string DoubleSidedMaterialFolder = "Assets/Materials/ScenarioDoubleSided";
    private const string Level3SceneDataFolder = "Assets/Scenes/Level3";

    [MenuItem("Tools/Levels/Integrate Level 3")]
    public static void IntegrateFromMenu()
    {
        IntegrateLevel3();
    }

    public static void RunBatch()
    {
        try
        {
            IntegrateLevel3();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void ValidateBatch()
    {
        try
        {
            ValidateLevel3Integration();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void IntegrateLevel3()
    {
        EnsureBuildSettings();
        PrepareLevel3ModelImporter();
        ConfigureMainMenu();
        ConfigureLevel3Scene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateLevel3Integration();

        Debug.Log("Level 3 integration completed.");
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true),
            new EditorBuildSettingsScene(Level2ScenePath, true),
            new EditorBuildSettingsScene(Level3ScenePath, true)
        };

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Build Settings updated with Level3 after Level2.");
    }

    private static void PrepareLevel3ModelImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(Level3ModelPath) as ModelImporter;

        if (importer == null)
            throw new FileNotFoundException($"Level 3 model importer not found: {Level3ModelPath}");

        bool changed = false;
        changed |= SetIfDifferent(() => importer.meshCompression, value => importer.meshCompression = value, ModelImporterMeshCompression.Off);
        changed |= SetIfDifferent(() => importer.generateSecondaryUV, value => importer.generateSecondaryUV = value, true);
        changed |= SetIfDifferent(() => importer.importCameras, value => importer.importCameras = value, false);
        changed |= SetIfDifferent(() => importer.importLights, value => importer.importLights = value, false);
        changed |= SetIfDifferent(() => importer.isReadable, value => importer.isReadable = value, false);
        changed |= SetIfDifferent(() => importer.optimizeMeshPolygons, value => importer.optimizeMeshPolygons = value, true);
        changed |= SetIfDifferent(() => importer.optimizeMeshVertices, value => importer.optimizeMeshVertices = value, true);
        changed |= SetIfDifferent(() => importer.preserveHierarchy, value => importer.preserveHierarchy = value, true);

        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log("Level 3 model importer prepared for scene use.");
        }
    }

    private static bool SetIfDifferent<T>(Func<T> getter, Action<T> setter, T value)
    {
        if (EqualityComparer<T>.Default.Equals(getter(), value))
            return false;

        setter(value);
        return true;
    }

    private static void ConfigureMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();

        if (controller == null)
            throw new InvalidOperationException("MainMenuController not found in MainMenu scene.");

        GameObject level2ButtonObject = FindObjectByName(scene, "Level 2 Button");

        if (level2ButtonObject == null)
            throw new InvalidOperationException("Level 2 Button not found in MainMenu scene.");

        GameObject level3ButtonObject = FindObjectByName(scene, "Level 3 Button");

        if (level3ButtonObject == null)
        {
            level3ButtonObject = UnityEngine.Object.Instantiate(level2ButtonObject, level2ButtonObject.transform.parent);
            level3ButtonObject.name = "Level 3 Button";
            Undo.RegisterCreatedObjectUndo(level3ButtonObject, "Create Level 3 Button");
        }

        ConfigureLevelButton(level3ButtonObject, controller, "NIVEL 03", Level3SceneName);
        RepositionLevelSelectorButtons(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main menu updated with Level 3 button.");
    }

    private static void ConfigureLevelButton(GameObject buttonObject, MainMenuController controller, string label, string sceneName)
    {
        Text labelText = null;
        Text[] texts = buttonObject.GetComponentsInChildren<Text>(true);

        foreach (Text text in texts)
        {
            if (text.gameObject.name.Equals("Label", StringComparison.OrdinalIgnoreCase))
            {
                labelText = text;
                break;
            }
        }

        if (labelText == null && texts.Length > 0)
            labelText = texts[0];

        if (labelText != null)
        {
            labelText.text = label;
            EditorUtility.SetDirty(labelText);
        }

        Button button = buttonObject.GetComponent<Button>();

        if (button == null)
            throw new InvalidOperationException($"Button component not found on {buttonObject.name}.");

        button.onClick = new Button.ButtonClickedEvent();
        UnityAction<string> action = controller.LoadScene;
        UnityEventTools.AddStringPersistentListener(button.onClick, action, sceneName);
        EditorUtility.SetDirty(button);
    }

    private static void RepositionLevelSelectorButtons(Scene scene)
    {
        SetAnchoredY(FindObjectByName(scene, "Level 1 Button"), -296f);
        SetAnchoredY(FindObjectByName(scene, "Level 2 Button"), -390f);
        SetAnchoredY(FindObjectByName(scene, "Level 3 Button"), -484f);
        SetAnchoredY(FindObjectByName(scene, "Back Button"), -594f);
    }

    private static void SetAnchoredY(GameObject gameObject, float y)
    {
        if (gameObject == null)
            return;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = y;
        rectTransform.anchoredPosition = anchoredPosition;
        EditorUtility.SetDirty(rectTransform);
    }

    private static void ConfigureLevel3Scene()
    {
        EnsureLevel3SceneDataFolder();

        Scene level3Scene = EditorSceneManager.OpenScene(Level3ScenePath, OpenSceneMode.Single);
        GameObject scenarioRoot = FindScenarioRoot(level3Scene);

        if (scenarioRoot == null)
            throw new InvalidOperationException($"Could not find scenario instance for {Level3ModelPath} in Level3 scene.");

        ConfigureSkybox();
        RemoveImportedCamerasAndLights(scenarioRoot);
        ConfigureScenarioGeometry(scenarioRoot);
        ApplyDoubleSidedMaterials(scenarioRoot);
        CopyLevelRuntimeObjectsFromLevel2(level3Scene);
        NavMeshSurface navMeshSurface = EnsureNavMesh(level3Scene);

        Vector3 spawnPosition = ResolveSpawnPosition(scenarioRoot);
        GameObject player = EnsurePlayer(level3Scene, spawnPosition);
        EnsureCamera(level3Scene, player.transform);
        EnsureExitDoor(level3Scene, scenarioRoot);

        EditorSceneManager.MarkSceneDirty(level3Scene);
        EditorSceneManager.SaveScene(level3Scene);

        if (navMeshSurface != null)
            EditorUtility.SetDirty(navMeshSurface);

        Debug.Log("Level3 scene configured.");
    }

    private static void EnsureLevel3SceneDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(Level3SceneDataFolder))
            AssetDatabase.CreateFolder("Assets/Scenes", "Level3");
    }

    private static void ConfigureSkybox()
    {
        Material blackSkybox = AssetDatabase.LoadAssetAtPath<Material>(BlackSkyboxPath);

        if (blackSkybox != null)
        {
            RenderSettings.skybox = blackSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    private static void RemoveImportedCamerasAndLights(GameObject scenarioRoot)
    {
        foreach (Camera camera in scenarioRoot.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(camera.gameObject);

        foreach (Light light in scenarioRoot.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(light.gameObject);
    }

    private static void ConfigureScenarioGeometry(GameObject scenarioRoot)
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        foreach (Transform child in scenarioRoot.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.isStatic = true;

            if (obstacleLayer >= 0)
                child.gameObject.layer = obstacleLayer;
        }

        foreach (MeshFilter meshFilter in scenarioRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            Collider existingCollider = meshFilter.GetComponent<Collider>();

            if (existingCollider != null)
                continue;

            MeshCollider collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);
        }
    }

    private static void ApplyDoubleSidedMaterials(GameObject scenarioRoot)
    {
        EnsureDoubleSidedMaterialFolder();

        Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();

        foreach (MeshRenderer renderer in scenarioRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];

                if (source == null)
                    continue;

                Material doubleSided = GetOrCreateDoubleSidedMaterial(source, materialCache);

                if (doubleSided == source)
                    continue;

                materials[i] = doubleSided;
                changed = true;
            }

            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;

            if (changed)
                renderer.sharedMaterials = materials;

            EditorUtility.SetDirty(renderer);
        }
    }

    private static void EnsureDoubleSidedMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        if (!AssetDatabase.IsValidFolder(DoubleSidedMaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "ScenarioDoubleSided");
    }

    private static Material GetOrCreateDoubleSidedMaterial(Material source, Dictionary<Material, Material> materialCache)
    {
        if (materialCache.TryGetValue(source, out Material cached))
            return cached;

        string sourcePath = AssetDatabase.GetAssetPath(source);

        if (sourcePath.StartsWith(DoubleSidedMaterialFolder, StringComparison.OrdinalIgnoreCase))
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
            material.SetFloat("_DoubleSidedGI", 1f);
    }

    private static void SetCullModeOff(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, (float)CullMode.Off);
    }

    private static string GetDoubleSidedMaterialPath(Material source)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string sourceAssetName = string.IsNullOrEmpty(sourcePath) ? "Runtime" : Path.GetFileNameWithoutExtension(sourcePath);
        string materialName = string.IsNullOrEmpty(source.name) ? "Material" : source.name;
        string fileName = $"{SanitizeFileName(sourceAssetName)}_{SanitizeFileName(materialName)}_DoubleSided.mat";

        return $"{DoubleSidedMaterialFolder}/{fileName}";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }

    private static void CopyLevelRuntimeObjectsFromLevel2(Scene level3Scene)
    {
        Scene level2Scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Additive);

        try
        {
            CopyRuntimeObjectIfMissing(level2Scene, level3Scene, "System HUD");
            CopyRuntimeObjectIfMissing(level2Scene, level3Scene, "EventSystem");
        }
        finally
        {
            EditorSceneManager.CloseScene(level2Scene, true);
            SceneManager.SetActiveScene(level3Scene);
        }
    }

    private static void CopyRuntimeObjectIfMissing(Scene sourceScene, Scene targetScene, string objectName)
    {
        if (FindObjectByName(targetScene, objectName) != null)
            return;

        GameObject source = FindObjectByName(sourceScene, objectName);

        if (source == null)
            throw new InvalidOperationException($"{objectName} not found in Level2 scene.");

        GameObject clone = UnityEngine.Object.Instantiate(source);
        clone.name = objectName;
        SceneManager.MoveGameObjectToScene(clone, targetScene);
        EditorUtility.SetDirty(clone);
    }

    private static NavMeshSurface EnsureNavMesh(Scene scene)
    {
        SceneManager.SetActiveScene(scene);

        NavMeshSurface surface = UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            GameObject surfaceObject = new GameObject("NavMesh Surface");
            SceneManager.MoveGameObjectToScene(surfaceObject, scene);
            surface = surfaceObject.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        Debug.Log("Level3 NavMesh rebuilt.");
        return surface;
    }

    private static Vector3 ResolveSpawnPosition(GameObject scenarioRoot)
    {
        Transform spawnMarker = FindChildByPartialName(scenarioRoot.transform, "spawn");

        if (spawnMarker != null && TrySampleNavMesh(spawnMarker.position, 8f, out Vector3 markerPosition))
            return markerPosition + Vector3.up * 0.08f;

        Bounds bounds = CalculateBounds(scenarioRoot);
        Vector3[] candidates =
        {
            bounds.center,
            new Vector3(bounds.min.x + bounds.size.x * 0.2f, bounds.center.y, bounds.min.z + bounds.size.z * 0.2f),
            new Vector3(bounds.min.x + bounds.size.x * 0.2f, bounds.center.y, bounds.max.z - bounds.size.z * 0.2f),
            new Vector3(bounds.max.x - bounds.size.x * 0.2f, bounds.center.y, bounds.min.z + bounds.size.z * 0.2f),
            new Vector3(bounds.max.x - bounds.size.x * 0.2f, bounds.center.y, bounds.max.z - bounds.size.z * 0.2f)
        };

        foreach (Vector3 candidate in candidates)
        {
            if (TrySampleNavMesh(candidate, Mathf.Max(bounds.extents.x, bounds.extents.z), out Vector3 sampledPosition))
                return sampledPosition + Vector3.up * 0.08f;
        }

        return new Vector3(bounds.center.x, bounds.min.y + 0.2f, bounds.center.z);
    }

    private static GameObject EnsurePlayer(Scene scene, Vector3 spawnPosition)
    {
        GameObject player = FindTaggedObjectInScene(scene, "Player");

        if (player == null)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (playerPrefab == null)
                throw new FileNotFoundException($"Player prefab not found: {PlayerPrefabPath}");

            player = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;

            if (player == null)
                throw new InvalidOperationException("Could not instantiate Player prefab.");
        }

        player.name = "Player";
        player.tag = "Player";
        player.transform.position = spawnPosition;
        player.transform.rotation = Quaternion.identity;
        EditorUtility.SetDirty(player);
        return player;
    }

    private static void EnsureCamera(Scene scene, Transform player)
    {
        GameObject cameraObject = FindObjectByName(scene, "Main Camera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
        }

        CameraFollow follow = cameraObject.GetComponent<CameraFollow>();

        if (follow == null)
            follow = cameraObject.AddComponent<CameraFollow>();

        follow.target = player;
        follow.fixedY = 12f;
        follow.offsetZ = -6f;
        follow.rotation = new Vector3(55f, 0f, 0f);
        cameraObject.transform.position = new Vector3(player.position.x, follow.fixedY, player.position.z + follow.offsetZ);
        cameraObject.transform.rotation = Quaternion.Euler(follow.rotation);
        EditorUtility.SetDirty(follow);
        EditorUtility.SetDirty(cameraObject.transform);
    }

    private static void EnsureExitDoor(Scene scene, GameObject scenarioRoot)
    {
        ExitDoor exitDoor = UnityEngine.Object.FindAnyObjectByType<ExitDoor>();

        if (exitDoor == null || exitDoor.gameObject.scene != scene)
        {
            GameObject doorObject = ResolveDoorObject(scene, scenarioRoot);
            exitDoor = doorObject.GetComponent<ExitDoor>();

            if (exitDoor == null)
                exitDoor = doorObject.AddComponent<ExitDoor>();
        }

        BoxCollider trigger = exitDoor.GetComponent<BoxCollider>();

        if (trigger == null)
            trigger = exitDoor.gameObject.AddComponent<BoxCollider>();

        trigger.isTrigger = true;

        if (trigger.size == Vector3.one)
            trigger.size = new Vector3(2.4f, 3f, 2.4f);

        exitDoor.fadeController = UnityEngine.Object.FindAnyObjectByType<FadeController>();
        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(exitDoor);
    }

    private static GameObject ResolveDoorObject(Scene scene, GameObject scenarioRoot)
    {
        Transform doorMarker = FindChildByPartialName(scenarioRoot.transform, "door") ??
            FindChildByPartialName(scenarioRoot.transform, "salida") ??
            FindChildByPartialName(scenarioRoot.transform, "exit");

        if (doorMarker != null)
            return doorMarker.gameObject;

        GameObject exitObject = FindObjectByName(scene, "Exit Door");

        if (exitObject != null)
            return exitObject;

        Bounds bounds = CalculateBounds(scenarioRoot);
        Vector3 candidate = new Vector3(bounds.max.x - bounds.size.x * 0.15f, bounds.center.y, bounds.max.z - bounds.size.z * 0.15f);

        if (TrySampleNavMesh(candidate, Mathf.Max(bounds.extents.x, bounds.extents.z), out Vector3 sampledPosition))
            candidate = sampledPosition;
        else
            candidate.y = bounds.min.y + 0.2f;

        exitObject = new GameObject("Exit Door");
        SceneManager.MoveGameObjectToScene(exitObject, scene);
        exitObject.transform.position = candidate + Vector3.up * 1.25f;
        return exitObject;
    }

    private static bool TrySampleNavMesh(Vector3 position, float maxDistance, out Vector3 sampledPosition)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            sampledPosition = hit.position;
            return true;
        }

        sampledPosition = position;
        return false;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static GameObject FindScenarioRoot(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            string prefabPath = GetPrefabAssetPath(root);

            if (prefabPath == Level3ModelPath)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (GetPrefabAssetPath(child.gameObject) == Level3ModelPath)
                    return child.gameObject;
            }
        }

        return null;
    }

    private static string GetPrefabAssetPath(GameObject gameObject)
    {
        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);

        if (source == null)
            return string.Empty;

        return AssetDatabase.GetAssetPath(source);
    }

    private static Transform FindChildByPartialName(Transform root, string partialName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
        }

        return null;
    }

    private static GameObject FindObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindTaggedObjectInScene(Scene scene, string tag)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.CompareTag(tag))
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag(tag))
                    return child.gameObject;
            }
        }

        return null;
    }

    private static void ValidateLevel3Integration()
    {
        ValidateBuildSettings();
        ValidateMainMenu();
        ValidateLevel3Scene();
        Debug.Log("Level 3 integration validation passed.");
    }

    private static void ValidateBuildSettings()
    {
        int level2Index = -1;
        int level3Index = -1;
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == Level2ScenePath)
                level2Index = i;

            if (scenes[i].path == Level3ScenePath)
                level3Index = i;
        }

        if (level3Index < 0)
            throw new InvalidOperationException("Level3 is not included in Build Settings.");

        if (level3Index != level2Index + 1)
            throw new InvalidOperationException("Level3 must be placed immediately after Level2 in Build Settings.");
    }

    private static void ValidateMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject level3Button = FindObjectByName(scene, "Level 3 Button");

        if (level3Button == null)
            throw new InvalidOperationException("MainMenu does not contain Level 3 Button.");

        Button button = level3Button.GetComponent<Button>();

        if (button == null || button.onClick.GetPersistentEventCount() == 0)
            throw new InvalidOperationException("Level 3 Button has no persistent load action.");
    }

    private static void ValidateLevel3Scene()
    {
        Scene scene = EditorSceneManager.OpenScene(Level3ScenePath, OpenSceneMode.Single);

        if (FindScenarioRoot(scene) == null)
            throw new InvalidOperationException("Level3 scenario mesh is missing.");

        if (FindTaggedObjectInScene(scene, "Player") == null)
            throw new InvalidOperationException("Level3 player is missing.");

        if (FindObjectByName(scene, "Main Camera")?.GetComponent<CameraFollow>() == null)
            throw new InvalidOperationException("Level3 camera follow is missing.");

        if (UnityEngine.Object.FindAnyObjectByType<FadeController>() == null)
            throw new InvalidOperationException("Level3 fade controller is missing.");

        if (UnityEngine.Object.FindAnyObjectByType<ExitDoor>() == null)
            throw new InvalidOperationException("Level3 exit door is missing.");

        if (UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>() == null)
            throw new InvalidOperationException("Level3 NavMeshSurface is missing.");
    }
}
