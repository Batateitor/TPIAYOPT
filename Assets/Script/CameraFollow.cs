using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [SerializeField] public float fixedY = 12f;
    [SerializeField] public float offsetZ = -6f;
    public Vector3 rotation = new Vector3(55f, 0f, 0f);

    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x,
            fixedY,
            target.position.z + offsetZ
        );

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(rotation);
    }
}