using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoor : MonoBehaviour
{
    public FadeController fadeController;
    public AudioSource audioSource;
    public AudioClip victoryClip;

    private bool activated = false;

    private void Awake()
    {
        FindFadeControllerIfNeeded();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject touchedObject)
    {
        if (activated || touchedObject == null)
            return;

        Transform touchedTransform = touchedObject.transform;
        bool isPlayer = touchedObject.CompareTag("Player") ||
            (touchedTransform.root != null && touchedTransform.root.CompareTag("Player"));

        if (!isPlayer)
            return;

        activated = true;

        if (audioSource != null && victoryClip != null)
        {
            audioSource.PlayOneShot(victoryClip);
        }

        FindFadeControllerIfNeeded();

        if (fadeController != null)
        {
            fadeController.FadeAndLoadNextScene();
            return;
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings && Application.CanStreamedLevelBeLoaded(nextSceneIndex))
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void FindFadeControllerIfNeeded()
    {
        if (fadeController == null)
        {
            fadeController = Object.FindAnyObjectByType<FadeController>();
        }
    }
}
