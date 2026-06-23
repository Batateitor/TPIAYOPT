using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";

    private void Update()
    {
        bool keyboardBack = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadBack = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (keyboardBack || gamepadBack)
            LoadMainMenu();
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene);
    }
}
