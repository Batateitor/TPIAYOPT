using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

public static class BodyguardModelSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/guardEnemy.prefab";
    private const string PlayerModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";
    private const string EnemyModelPath = "Assets/BodyGuards/Meshes/SkelMesh_Bodyguard_03.fbx";
    private const string PlayerWalkClipPath = "Assets/Animations/Crouched Walking.fbx";
    private const string PlayerRunClipPath = "Assets/Animations/Fast Run.fbx";
    private const string EnemyWalkClipPath = "Assets/Animations/Walking.fbx";
    private const string EnemyRunClipPath = "Assets/Animations/Standard Run.fbx";
    private const string PlayerControllerPath = "Assets/Animations/PlayerBodyguard.controller";
    private const string EnemyControllerPath = "Assets/Animations/GuardBodyguard.controller";
    private const string VisualRootName = "BodyguardVisual";
    private const string ModelInstanceName = "BodyguardModel";
    private const float VisualSizeMultiplier = 1.12f;
    private const string IsMovingParameter = "IsMoving";
    private const string IsRunningParameter = "IsRunning";

    [MenuItem("Tools/Apply Bodyguard Models")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    public static void RunBatch()
    {
        try
        {
            Apply();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Apply()
    {
        ConfigureCharacterModel(PlayerModelPath);
        ConfigureCharacterModel(EnemyModelPath);
        ConfigureAnimationClip(PlayerWalkClipPath);
        ConfigureAnimationClip(PlayerRunClipPath);
        ConfigureAnimationClip(EnemyWalkClipPath);
        ConfigureAnimationClip(EnemyRunClipPath);

        AnimationClip playerWalk = LoadClip(PlayerWalkClipPath);
        AnimationClip playerRun = LoadClip(PlayerRunClipPath);
        AnimationClip enemyWalk = LoadClip(EnemyWalkClipPath);
        AnimationClip enemyRun = LoadClip(EnemyRunClipPath);

        AnimatorController playerController = BuildController(
            PlayerControllerPath,
            "Crouched Walking",
            playerWalk,
            "Fast Run",
            playerRun);

        AnimatorController enemyController = BuildController(
            EnemyControllerPath,
            "Walking",
            enemyWalk,
            "Standard Run",
            enemyRun);

        ApplyPlayerPrefab(playerController);
        ApplyEnemyPrefab(enemyController);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bodyguard models and animations were applied to Player and guardEnemy prefabs.");
    }

    private static void ConfigureCharacterModel(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            throw new InvalidOperationException($"Model not found: {path}");
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureAnimationClip(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            throw new InvalidOperationException($"Animation not found: {path}");
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;

        ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;

        for (int i = 0; i < clipAnimations.Length; i++)
        {
            clipAnimations[i].loopTime = true;
            clipAnimations[i].loopPose = true;
            clipAnimations[i].lockRootRotation = true;
            clipAnimations[i].lockRootHeightY = true;
            clipAnimations[i].lockRootPositionXZ = true;
            clipAnimations[i].keepOriginalOrientation = true;
            clipAnimations[i].keepOriginalPositionY = true;
            clipAnimations[i].keepOriginalPositionXZ = true;
        }

        importer.clipAnimations = clipAnimations;
        importer.SaveAndReimport();
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(asset => !asset.name.StartsWith("__preview__", StringComparison.Ordinal));

        if (clip == null)
        {
            throw new InvalidOperationException($"No animation clip found in {path}");
        }

        return clip;
    }

    private static AnimatorController BuildController(
        string path,
        string walkStateName,
        AnimationClip walkClip,
        string runStateName,
        AnimationClip runClip)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter(IsMovingParameter, AnimatorControllerParameterType.Bool);
        controller.AddParameter(IsRunningParameter, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState walkState = stateMachine.AddState(walkStateName, new Vector3(240f, 120f, 0f));
        walkState.motion = walkClip;
        stateMachine.defaultState = walkState;

        AnimatorState runState = stateMachine.AddState(runStateName, new Vector3(520f, 120f, 0f));
        runState.motion = runClip;

        AnimatorStateTransition runTransition = walkState.AddTransition(runState);
        runTransition.hasExitTime = false;
        runTransition.duration = 0.1f;
        runTransition.canTransitionToSelf = false;
        runTransition.AddCondition(AnimatorConditionMode.If, 0f, IsRunningParameter);

        AnimatorStateTransition walkTransition = runState.AddTransition(walkState);
        walkTransition.hasExitTime = false;
        walkTransition.duration = 0.1f;
        walkTransition.canTransitionToSelf = false;
        walkTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, IsRunningParameter);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ApplyPlayerPrefab(RuntimeAnimatorController controller)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        try
        {
            VisualSetup visualSetup = ApplyVisual(prefabRoot, PlayerModelPath, controller);
            PlayerBodyguardAnimator driver = prefabRoot.GetComponent<PlayerBodyguardAnimator>();

            if (driver == null)
            {
                driver = prefabRoot.AddComponent<PlayerBodyguardAnimator>();
            }

            SerializedObject serializedDriver = new SerializedObject(driver);
            serializedDriver.FindProperty("animator").objectReferenceValue = visualSetup.Animator;
            serializedDriver.FindProperty("stamina").objectReferenceValue = prefabRoot.GetComponent<PlayerStamina>();
            serializedDriver.FindProperty("visualRoot").objectReferenceValue = visualSetup.Pivot;
            serializedDriver.FindProperty("minimumMoveSpeed").floatValue = 0.05f;
            serializedDriver.FindProperty("walkReferenceSpeed").floatValue = 5f;
            serializedDriver.FindProperty("runReferenceSpeed").floatValue = 8f;
            serializedDriver.FindProperty("turnSpeed").floatValue = 18f;
            serializedDriver.FindProperty("lockAnimatorRoot").boolValue = true;
            serializedDriver.FindProperty("stoppedStateName").stringValue = "Crouched Walking";
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ApplyEnemyPrefab(RuntimeAnimatorController controller)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

        try
        {
            VisualSetup visualSetup = ApplyVisual(prefabRoot, EnemyModelPath, controller);
            GuardBodyguardAnimator driver = prefabRoot.GetComponent<GuardBodyguardAnimator>();

            if (driver == null)
            {
                driver = prefabRoot.AddComponent<GuardBodyguardAnimator>();
            }

            SerializedObject serializedDriver = new SerializedObject(driver);
            serializedDriver.FindProperty("animator").objectReferenceValue = visualSetup.Animator;
            serializedDriver.FindProperty("visualRoot").objectReferenceValue = visualSetup.Pivot;
            serializedDriver.FindProperty("minimumMoveSpeed").floatValue = 0.05f;
            serializedDriver.FindProperty("runThreshold").floatValue = 3f;
            serializedDriver.FindProperty("walkReferenceSpeed").floatValue = 2f;
            serializedDriver.FindProperty("runReferenceSpeed").floatValue = 4f;
            serializedDriver.FindProperty("turnSpeed").floatValue = 14f;
            serializedDriver.FindProperty("lockAnimatorRoot").boolValue = true;
            serializedDriver.FindProperty("useAgentRotation").boolValue = true;
            serializedDriver.FindProperty("stoppedStateName").stringValue = "Walking";
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(prefabRoot, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static VisualSetup ApplyVisual(GameObject prefabRoot, string modelPath, RuntimeAnimatorController controller)
    {
        MeshRenderer capsuleRenderer = prefabRoot.GetComponent<MeshRenderer>();

        if (capsuleRenderer != null)
        {
            capsuleRenderer.enabled = false;
        }

        Transform previousVisual = prefabRoot.transform.Find(VisualRootName);

        if (previousVisual != null)
        {
            UnityEngine.Object.DestroyImmediate(previousVisual.gameObject);
        }

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        if (modelPrefab == null)
        {
            throw new InvalidOperationException($"Model prefab not found: {modelPath}");
        }

        GameObject visualPivot = new GameObject(VisualRootName);
        visualPivot.transform.SetParent(prefabRoot.transform, false);
        visualPivot.transform.localPosition = Vector3.zero;
        visualPivot.transform.localRotation = Quaternion.identity;
        visualPivot.transform.localScale = Vector3.one;

        GameObject visual = PrefabUtility.InstantiatePrefab(modelPrefab, visualPivot.transform) as GameObject;

        if (visual == null)
        {
            throw new InvalidOperationException($"Could not instantiate model: {modelPath}");
        }

        visual.name = ModelInstanceName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        RemovePhysicsFromVisual(visual);
        OptimizeVisualRenderers(visual);
        FitVisualToCapsule(prefabRoot, visual);

        Animator animator = visual.GetComponent<Animator>();

        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        return new VisualSetup(animator, visualPivot.transform);
    }

    private static void RemovePhysicsFromVisual(GameObject visual)
    {
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            UnityEngine.Object.DestroyImmediate(rigidbody);
        }
    }

    private static void OptimizeVisualRenderers(GameObject visual)
    {
        foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            renderer.updateWhenOffscreen = false;
            renderer.skinnedMotionVectors = false;
        }
    }

    private static void FitVisualToCapsule(GameObject prefabRoot, GameObject visual)
    {
        GetCapsuleSpace(prefabRoot, out float bottom, out float height);

        Bounds bounds = CalculateLocalBounds(prefabRoot.transform, visual);
        float targetHeight = height * VisualSizeMultiplier;
        float scale = targetHeight / Mathf.Max(bounds.size.y, 0.001f);
        visual.transform.localScale = Vector3.one * scale;

        bounds = CalculateLocalBounds(prefabRoot.transform, visual);
        Vector3 localPosition = visual.transform.localPosition;
        localPosition.y += bottom - bounds.min.y;
        visual.transform.localPosition = localPosition;

        bounds = CalculateLocalBounds(prefabRoot.transform, visual);
        localPosition = visual.transform.localPosition;
        localPosition.x -= bounds.center.x;
        localPosition.z -= bounds.center.z;
        visual.transform.localPosition = localPosition;
    }

    private static void GetCapsuleSpace(GameObject prefabRoot, out float bottom, out float height)
    {
        CharacterController characterController = prefabRoot.GetComponent<CharacterController>();

        if (characterController != null)
        {
            height = characterController.height;
            bottom = characterController.center.y - height * 0.5f;
            return;
        }

        CapsuleCollider capsuleCollider = prefabRoot.GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            height = capsuleCollider.height;
            bottom = capsuleCollider.center.y - height * 0.5f;
            return;
        }

        NavMeshAgent navMeshAgent = prefabRoot.GetComponent<NavMeshAgent>();

        if (navMeshAgent != null)
        {
            height = navMeshAgent.height;
            bottom = navMeshAgent.baseOffset - height;
            return;
        }

        height = 2f;
        bottom = -1f;
    }

    private static Bounds CalculateLocalBounds(Transform space, GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = space.InverseTransformPoint(corner);

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                    continue;
                }

                localBounds.Encapsulate(localCorner);
            }
        }

        return hasBounds ? localBounds : new Bounds(Vector3.zero, Vector3.one);
    }

    private static void SavePrefab(GameObject prefabRoot, string path)
    {
        bool saved = false;
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path, out saved);

        if (!saved)
        {
            throw new InvalidOperationException($"Could not save prefab: {path}");
        }
    }

    private struct VisualSetup
    {
        public readonly Animator Animator;
        public readonly Transform Pivot;

        public VisualSetup(Animator animator, Transform pivot)
        {
            Animator = animator;
            Pivot = pivot;
        }
    }
}
