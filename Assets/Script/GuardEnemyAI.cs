using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardEnemyAI : MonoBehaviour
{
    [Header("Patrol Nodes")]
    [SerializeField] private List<GuardEnemyNode> patrolNodes = new List<GuardEnemyNode>();
    [SerializeField] private float nodeReachedDistance = 0.35f;

    [Header("Vision")]
    [SerializeField] private Transform player;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField, Range(1f, 180f)] private float viewAngle = 75f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float returnSpeed = 2.5f;
    [SerializeField] private float investigationWaitTime = 3f;

    private IGuardEnemyState currentState;
    private NavMeshAgent agent;
    private int currentNodeIndex;
    private int lastVisitedNodeIndex = -1;
    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPlayerPosition;

    public NavMeshAgent Agent => agent;
    public float ChaseSpeed => chaseSpeed;
    public float ReturnSpeed => returnSpeed;
    public float InvestigationWaitTime => investigationWaitTime;
    public bool HasPatrolNodes => patrolNodes != null && patrolNodes.Count > 0;
    public bool HasLastSeenPlayerPosition => hasLastSeenPlayerPosition;
    public Vector3 LastSeenPlayerPosition => lastSeenPlayerPosition;

    private void Reset()
    {
        gameObject.name = "guardEnemy";
        obstacleMask = LayerMask.GetMask("Obstacle");
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Obstacle");
        }
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        ChangeState(new GuardEnemyPatrolState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(IGuardEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public bool CanSeePlayer()
    {
        FindPlayerIfNeeded();

        if (player == null)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + eyeOffset;
        Vector3 targetPosition = player.position + Vector3.up;
        Vector3 directionToPlayer = targetPosition - eyePosition;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        directionToPlayer.Normalize();

        if (Vector3.Angle(transform.forward, directionToPlayer) > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(eyePosition, directionToPlayer, distanceToPlayer, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        lastSeenPlayerPosition = player.position;
        hasLastSeenPlayerPosition = true;
        return true;
    }

    public bool MoveToCurrentPatrolNode()
    {
        GuardEnemyNode node = GetCurrentPatrolNode();

        if (node == null)
        {
            return false;
        }

        MoveToPosition(node.transform.position, patrolSpeed);
        return true;
    }

    public bool MoveToLastSeenPlayerPosition(float speed)
    {
        if (!hasLastSeenPlayerPosition)
        {
            return false;
        }

        MoveToPosition(lastSeenPlayerPosition, speed);
        return true;
    }

    public bool MoveToLastVisitedNode()
    {
        GuardEnemyNode node = GetLastVisitedNode();

        if (node == null)
        {
            return false;
        }

        MoveToPosition(node.transform.position, returnSpeed);
        return true;
    }

    public void MoveToPosition(Vector3 position, float speed)
    {
        if (!IsAgentReady())
        {
            return;
        }

        agent.speed = speed;
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public void StopMoving()
    {
        if (!IsAgentReady())
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (!IsAgentReady())
        {
            return true;
        }

        if (agent.pathPending)
        {
            return false;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return true;
        }

        float reachDistance = Mathf.Max(agent.stoppingDistance, nodeReachedDistance);

        if (agent.remainingDistance > reachDistance)
        {
            return false;
        }

        return !agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f;
    }

    public void MarkCurrentNodeVisitedAndAdvance()
    {
        if (!HasPatrolNodes)
        {
            return;
        }

        lastVisitedNodeIndex = currentNodeIndex;
        currentNodeIndex = GetNextValidNodeIndex(currentNodeIndex);
    }

    public void AdvanceToNextPatrolNode()
    {
        if (!HasPatrolNodes)
        {
            return;
        }

        currentNodeIndex = GetNextValidNodeIndex(currentNodeIndex);
    }

    public void ResumePatrolAfterReturn()
    {
        if (!HasPatrolNodes || lastVisitedNodeIndex < 0)
        {
            return;
        }

        currentNodeIndex = GetNextValidNodeIndex(lastVisitedNodeIndex);
    }

    private GuardEnemyNode GetCurrentPatrolNode()
    {
        if (!HasPatrolNodes)
        {
            return null;
        }

        currentNodeIndex = Mathf.Clamp(currentNodeIndex, 0, patrolNodes.Count - 1);
        return patrolNodes[currentNodeIndex];
    }

    private GuardEnemyNode GetLastVisitedNode()
    {
        if (IsValidNodeIndex(lastVisitedNodeIndex))
        {
            return patrolNodes[lastVisitedNodeIndex];
        }

        return GetCurrentPatrolNode();
    }

    private int GetNextValidNodeIndex(int startIndex)
    {
        if (!HasPatrolNodes)
        {
            return 0;
        }

        for (int offset = 1; offset <= patrolNodes.Count; offset++)
        {
            int candidate = (startIndex + offset) % patrolNodes.Count;

            if (patrolNodes[candidate] != null)
            {
                return candidate;
            }
        }

        return startIndex;
    }

    private bool IsValidNodeIndex(int index)
    {
        return patrolNodes != null && index >= 0 && index < patrolNodes.Count && patrolNodes[index] != null;
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePosition = transform.position + eyeOffset;
        Vector3 leftLimit = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightLimit = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, viewDistance);
        Gizmos.DrawRay(eyePosition, leftLimit * viewDistance);
        Gizmos.DrawRay(eyePosition, rightLimit * viewDistance);

        Gizmos.color = Color.cyan;

        for (int i = 0; patrolNodes != null && i < patrolNodes.Count; i++)
        {
            if (patrolNodes[i] == null)
            {
                continue;
            }

            Gizmos.DrawLine(transform.position, patrolNodes[i].transform.position);
        }
    }
}
