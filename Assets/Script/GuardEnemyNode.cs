using UnityEngine;

public class GuardEnemyNode : MonoBehaviour
{
    [SerializeField] private float gizmoRadius = 0.3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
    }
}
