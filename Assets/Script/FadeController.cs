using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeController : MonoBehaviour
{
    [SerializeField] public Image fadeImage;
    [SerializeField] private string lastLevelFallbackScene = "MainMenu";
    public float fadeDuration = 1f;

    private bool isFading;

    public void FadeAndReload()
    {
        StartFade(SceneManager.GetActiveScene().buildIndex);
    }

    public void FadeAndLoadNextScene()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings && Application.CanStreamedLevelBeLoaded(nextSceneIndex))
        {
            StartFade(nextSceneIndex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(lastLevelFallbackScene) && Application.CanStreamedLevelBeLoaded(lastLevelFallbackScene))
        {
            StartCoroutine(FadeOut(lastLevelFallbackScene));
            return;
        }

        Debug.LogWarning("No hay una escena siguiente configurada para cargar.");
    }

    private void StartFade(int sceneBuildIndex)
    {
        if (isFading)
            return;

        StartCoroutine(FadeOut(sceneBuildIndex));
    }

    private IEnumerator FadeOut(int sceneBuildIndex)
    {
        yield return FadeToBlack();
        yield return LoadSceneAsync(sceneBuildIndex);
    }

    private IEnumerator FadeOut(string sceneName)
    {
        if (isFading)
            yield break;

        yield return FadeToBlack();
        yield return LoadSceneAsync(sceneName);
    }

    private IEnumerator FadeToBlack()
    {
        isFading = true;
        float t = 0;

        Color c = fadeImage != null ? fadeImage.color : Color.black;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);

            if (fadeImage != null)
                fadeImage.color = c;

            yield return null;
        }
    }

    private static IEnumerator LoadSceneAsync(int sceneBuildIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneBuildIndex);

        if (operation == null)
        {
            SceneManager.LoadScene(sceneBuildIndex);
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }

    private static IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }
}
