using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    public float viewDistance = 10f;
    public float viewAngle = 45f;
    public LayerMask obstacleMask;
    public Transform target;

    public bool CanSeeTarget()
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle < viewAngle / 2f)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= viewDistance)
            {
                if (!Physics.Raycast(transform.position, directionToTarget, distance, obstacleMask))
                {
                    return true;
                }
            }
        }

        return false;
    }
}