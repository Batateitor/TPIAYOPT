using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class GamePerformanceSettings
{
    private const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= ApplyAfterSceneLoad;
        SceneManager.sceneLoaded += ApplyAfterSceneLoad;
        Apply();
    }

    private static void ApplyAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
        OnDemandRendering.renderFrameInterval = 1;
    }
}
