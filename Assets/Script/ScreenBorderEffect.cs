using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ScreenBorderEffect : MonoBehaviour
{
    private const string EffectObjectName = "Screen Border Effect";
    private const int TextureWidth = 512;
    private const int TextureHeight = 288;
    private const int SortingOrder = -20;

    private static ScreenBorderEffect activeInstance;

    private Texture2D borderTexture;
    private Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        UpdateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateForScene(scene);
    }

    private static void UpdateForScene(Scene scene)
    {
        bool shouldShow = scene.IsValid() && scene.name != "MainMenu";

        if (!shouldShow)
        {
            if (activeInstance != null)
                activeInstance.SetVisible(false);

            return;
        }

        if (activeInstance == null)
        {
            GameObject effectObject = new GameObject(EffectObjectName);
            activeInstance = effectObject.AddComponent<ScreenBorderEffect>();
        }

        activeInstance.SetVisible(true);
    }

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        activeInstance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;

        if (borderTexture != null)
            Destroy(borderTexture);
    }

    private void SetVisible(bool isVisible)
    {
        if (canvas != null)
            canvas.enabled = isVisible;
    }

    private void CreateOverlay()
    {
        gameObject.layer = LayerMask.NameToLayer("UI");

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvas.pixelPerfect = false;

        GameObject imageObject = new GameObject("Border Texture", typeof(RectTransform), typeof(CanvasRenderer));
        imageObject.layer = gameObject.layer;
        imageObject.transform.SetParent(transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        RawImage rawImage = imageObject.AddComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.texture = CreateBorderTexture();
        rawImage.color = Color.white;
    }

    private Texture2D CreateBorderTexture()
    {
        borderTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true);
        borderTexture.name = "Runtime Screen Border Texture";
        borderTexture.wrapMode = TextureWrapMode.Clamp;
        borderTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[TextureWidth * TextureHeight];

        for (int y = 0; y < TextureHeight; y++)
        {
            float v = y / (TextureHeight - 1f);

            for (int x = 0; x < TextureWidth; x++)
            {
                float u = x / (TextureWidth - 1f);
                float side = Mathf.Max(EdgeFade(u, 0.24f), EdgeFade(1f - u, 0.24f));
                float vertical = Mathf.Max(EdgeFade(v, 0.18f), EdgeFade(1f - v, 0.18f));
                float corner = Mathf.Pow(side * vertical, 0.45f);
                float alpha = Mathf.Clamp01(side * 0.42f + vertical * 0.25f + corner * 0.36f);

                float noise = Hash01(x, y);
                alpha *= Mathf.Lerp(0.9f, 1.08f, noise);

                Color pixel = new Color(0f, 0f, 0f, alpha);

                if (alpha > 0.06f && noise > 0.985f)
                {
                    float speck = Mathf.Lerp(0.08f, 0.18f, Hash01(x + 37, y + 91));
                    float tint = Mathf.Lerp(0.48f, 0.78f, Hash01(x + 13, y + 53));
                    pixel = new Color(tint, tint, tint, speck * Mathf.Clamp01(alpha * 1.5f));
                }

                pixels[y * TextureWidth + x] = pixel;
            }
        }

        borderTexture.SetPixels(pixels);
        borderTexture.Apply(false, true);
        return borderTexture;
    }

    private static float EdgeFade(float distanceFromEdge, float width)
    {
        float value = Mathf.Clamp01((width - distanceFromEdge) / width);
        return value * value * (3f - 2f * value);
    }

    private static float Hash01(int x, int y)
    {
        uint hash = (uint)(x * 374761393 + y * 668265263);
        hash = (hash ^ (hash >> 13)) * 1274126177u;
        return (hash ^ (hash >> 16)) / 4294967295f;
    }
}
