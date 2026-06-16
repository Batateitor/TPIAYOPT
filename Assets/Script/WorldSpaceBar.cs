using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform target;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    public void SetValue(float value)
    {
        fillImage.fillAmount = value;
    }
}