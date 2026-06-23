using UnityEngine;
using UnityEngine.UI;

public class FpsCounter : MonoBehaviour
{
    [SerializeField] private Text fpsLabel;
    [SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;

    private float elapsedTime;
    private int frameCount;

    public void Configure(Text label)
    {
        fpsLabel = label;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        frameCount = 0;

        if (fpsLabel != null)
            fpsLabel.text = "FPS --";
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime < refreshInterval)
            return;

        int fps = Mathf.RoundToInt(frameCount / elapsedTime);

        if (fpsLabel != null)
            fpsLabel.text = $"FPS {fps}";

        elapsedTime = 0f;
        frameCount = 0;
    }
}
