using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        transform.position = target.position + offset;
        transform.forward = cam.transform.forward;
    }

    public void SetValue(float value)
    {
        fillImage.fillAmount = value;
    }
}