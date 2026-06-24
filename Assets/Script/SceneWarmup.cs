using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneWarmup : MonoBehaviour
{
    private const string RunnerName = "Scene Warmup Runner";
    private const int OverlaySortingOrder = 6000;
    private const int RendererBatchSize = 24;
    private const float FadeOutDuration = 0.18f;

    private static SceneWarmup runner;
    private static bool shadersWarmed;

    private Canvas overlayCanvas;
    private Image overlayImage;
    private Text loadingText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!ShouldWarmup(scene))
            return;

        EnsureRunner().StartCoroutine(runner.WarmupScene());
    }

    private static bool ShouldWarmup(Scene scene)
    {
        return scene.IsValid() && scene.name != "MainMenu";
    }

    private static SceneWarmup EnsureRunner()
    {
        if (runner != null)
            return runner;

        GameObject runnerObject = new GameObject(RunnerName);
        runner = runnerObject.AddComponent<SceneWarmup>();
        DontDestroyOnLoad(runnerObject);
        return runner;
    }

    private void Awake()
    {
        if (runner != null && runner != this)
        {
            Destroy(gameObject);
            return;
        }

        runner = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator WarmupScene()
    {
        EnsureOverlay();
        SetOverlayAlpha(1f);

        Time.timeScale = 0f;

        yield return null;
        yield return new WaitForEndOfFrame();

        if (!shadersWarmed)
        {
            Shader.WarmupAllShaders();
            shadersWarmed = true;
            yield return null;
        }

        yield return WarmupRenderers();
        yield return WarmupAnimators();
        yield return WarmupAudio();

        yield return new WaitForEndOfFrame();
        yield return null;

        Time.timeScale = 1f;
        yield return FadeOverlayOut();
        SetOverlayVisible(false);
    }

    private IEnumerator WarmupRenderers()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer current = renderers[i];

            if (current == null)
                continue;

            Material[] materials = current.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                WarmupMaterial(materials[materialIndex]);
            }

            if (current is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
            {
                skinnedMeshRenderer.updateWhenOffscreen = false;
            }

            if (i % RendererBatchSize == 0)
                yield return null;
        }
    }

    private IEnumerator WarmupAnimators()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].isActiveAndEnabled)
            {
                animators[i].Update(0f);
            }

            if (i % RendererBatchSize == 0)
                yield return null;
        }
    }

    private IEnumerator WarmupAudio()
    {
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioClip clip = audioSources[i] != null ? audioSources[i].clip : null;

            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            if (i % RendererBatchSize == 0)
                yield return null;
        }
    }

    private static void WarmupMaterial(Material material)
    {
        if (material == null)
            return;

        _ = material.shader;

        if (material.HasProperty("_BaseMap"))
            WarmupTexture(material.GetTexture("_BaseMap"));

        if (material.HasProperty("_MainTex"))
            WarmupTexture(material.GetTexture("_MainTex"));

        if (material.HasProperty("_BumpMap"))
            WarmupTexture(material.GetTexture("_BumpMap"));

        if (material.HasProperty("_OcclusionMap"))
            WarmupTexture(material.GetTexture("_OcclusionMap"));
    }

    private static void WarmupTexture(Texture texture)
    {
        if (texture == null)
            return;

        _ = texture.width;
        _ = texture.height;
    }

    private void EnsureOverlay()
    {
        if (overlayCanvas != null)
        {
            SetOverlayVisible(true);
            return;
        }

        GameObject canvasObject = new GameObject("Scene Warmup Overlay", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        overlayImage = imageObject.AddComponent<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.color = Color.black;

        GameObject textObject = new GameObject("Loading Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 42f);
        textRect.sizeDelta = new Vector2(0f, 64f);

        loadingText = textObject.AddComponent<Text>();
        loadingText.raycastTarget = false;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.fontSize = 24;
        loadingText.fontStyle = FontStyle.Bold;
        loadingText.color = new Color(1f, 1f, 1f, 0.75f);
        loadingText.text = "CARGANDO...";
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (loadingText.font == null)
            loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private IEnumerator FadeOverlayOut()
    {
        float elapsed = 0f;

        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / FadeOutDuration);
            SetOverlayAlpha(alpha);
            yield return null;
        }

        SetOverlayAlpha(0f);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvas != null)
            overlayCanvas.enabled = visible;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlayImage != null)
            overlayImage.color = new Color(0f, 0f, 0f, alpha);

        if (loadingText != null)
            loadingText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 0.75f));
    }
}
