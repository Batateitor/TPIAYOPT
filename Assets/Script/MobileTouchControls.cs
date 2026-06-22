using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class MobileTouchControls : MonoBehaviour
{
    private const string ControlsObjectName = "Mobile Touch Controls";

    [SerializeField] private bool showInEditor = true;
    [SerializeField] private bool showOnDesktop = false;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 stickPosition = new Vector2(170f, 155f);
    [SerializeField] private Vector2 runButtonPosition = new Vector2(-170f, 155f);
    [SerializeField] private float stickBaseSize = 220f;
    [SerializeField] private float stickHandleSize = 120f;
    [SerializeField] private float stickMovementRange = 60f;
    [SerializeField] private float runButtonSize = 132f;
    [SerializeField] private string moveControlPath = "<Gamepad>/leftStick";
    [SerializeField] private string sprintControlPath = "<Gamepad>/leftStickPress";

    private static GameObject activeControls;

    private void Awake()
    {
        if (!ShouldShowControls())
            return;

        EnsureEventSystem();

        if (activeControls == null)
            activeControls = GameObject.Find(ControlsObjectName);

        if (activeControls == null)
            activeControls = CreateControlsCanvas();
    }

    private bool ShouldShowControls()
    {
#if UNITY_EDITOR
        if (showInEditor)
            return true;
#endif

        if (Application.isMobilePlatform)
            return true;

        return showOnDesktop && Touchscreen.current != null;
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputModule == null)
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        if (inputModule.actionsAsset == null)
            inputModule.AssignDefaultActions();

        BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();

        for (int i = 0; i < inputModules.Length; i++)
        {
            if (inputModules[i] != inputModule)
                inputModules[i].enabled = false;
        }
    }

    private GameObject CreateControlsCanvas()
    {
        GameObject canvasObject = new GameObject(ControlsObjectName);
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateMovementStick(canvasObject.transform);
        CreateRunButton(canvasObject.transform);

        return canvasObject;
    }

    private void CreateMovementStick(Transform parent)
    {
        GameObject baseObject = CreateUiObject("Move Stick Base", parent);
        RectTransform baseRect = baseObject.GetComponent<RectTransform>();
        baseRect.anchorMin = Vector2.zero;
        baseRect.anchorMax = Vector2.zero;
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = stickPosition;
        baseRect.sizeDelta = new Vector2(stickBaseSize, stickBaseSize);

        Image baseImage = baseObject.AddComponent<Image>();
        baseImage.sprite = CreateCircleSprite("Stick Base Sprite", 128, new Color(1f, 1f, 1f, 0.1f), new Color(1f, 1f, 1f, 0.35f), 6);
        baseImage.raycastTarget = false;

        GameObject handleObject = CreateUiObject("Move Stick Handle", baseObject.transform);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(stickHandleSize, stickHandleSize);

        Image handleImage = handleObject.AddComponent<Image>();
        handleImage.sprite = CreateCircleSprite("Stick Handle Sprite", 128, new Color(1f, 1f, 1f, 0.28f), new Color(1f, 1f, 1f, 0.58f), 5);

        OnScreenStick stick = handleObject.AddComponent<OnScreenStick>();
        stick.controlPath = moveControlPath;
        stick.movementRange = stickMovementRange;
        stick.useIsolatedInputActions = true;
    }

    private void CreateRunButton(Transform parent)
    {
        GameObject buttonObject = CreateUiObject("Run Button", parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = runButtonPosition;
        buttonRect.sizeDelta = new Vector2(runButtonSize, runButtonSize);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = CreateCircleSprite("Run Button Sprite", 128, new Color(0.14f, 0.72f, 0.94f, 0.35f), new Color(1f, 1f, 1f, 0.65f), 5);

        OnScreenButton button = buttonObject.AddComponent<OnScreenButton>();
        button.controlPath = sprintControlPath;

        GameObject labelObject = CreateUiObject("Run Button Label", buttonObject.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.text = "CORRER";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.layer = LayerMask.NameToLayer("UI");
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Sprite CreateCircleSprite(string spriteName, int size, Color fill, Color border, int borderWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName + " Texture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        float radius = (size - 1) * 0.5f;
        float innerRadius = Mathf.Max(0f, radius - borderWidth);
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance > radius)
                {
                    texture.SetPixel(x, y, clear);
                }
                else if (distance >= innerRadius)
                {
                    texture.SetPixel(x, y, border);
                }
                else
                {
                    texture.SetPixel(x, y, fill);
                }
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = spriteName;
        return sprite;
    }
}
