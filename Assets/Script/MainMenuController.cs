using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelSelectorPanel;
    [SerializeField] private Button mainDefaultButton;
    [SerializeField] private Button selectorDefaultButton;

    public void Configure(
        GameObject main,
        GameObject selector,
        Button mainDefault,
        Button selectorDefault)
    {
        mainPanel = main;
        levelSelectorPanel = selector;
        mainDefaultButton = mainDefault;
        selectorDefaultButton = selectorDefault;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetPanel(mainPanel, true);
        SetPanel(levelSelectorPanel, false);
        Select(mainDefaultButton);
    }

    public void ShowLevelSelector()
    {
        SetPanel(mainPanel, false);
        SetPanel(levelSelectorPanel, true);
        Select(selectorDefaultButton);
    }

    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no esta incluida en Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
