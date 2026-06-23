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

    [Header("Capture")]
    [SerializeField] private float captureDistance = 1.1f;
    [SerializeField] private float verticalCaptureDistance = 2.25f;
    [SerializeField] private AudioSource captureAlarmSource;
    [SerializeField] private AudioClip captureAlarmClip;
    [SerializeField, Range(0f, 1f)] private float captureAlarmVolume = 1f;

    private IGuardEnemyState currentState;
    private NavMeshAgent agent;
    private int currentNodeIndex;
    private int lastVisitedNodeIndex = -1;
    private Vector3 lastSeenPlayerPosition;
    private bool hasLastSeenPlayerPosition;
    private bool hasCapturedPlayer;

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
        ConfigureCaptureAudio();

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
        if (TryCapturePlayerByDistance())
        {
            return;
        }

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

    private bool TryCapturePlayerByDistance()
    {
        if (hasCapturedPlayer)
        {
            return true;
        }

        FindPlayerIfNeeded();

        if (player == null)
        {
            return false;
        }

        if (Mathf.Abs(player.position.y - transform.position.y) > verticalCaptureDistance)
        {
            return false;
        }

        Vector3 enemyPosition = transform.position;
        Vector3 playerPosition = player.position;
        enemyPosition.y = 0f;
        playerPosition.y = 0f;

        if (Vector3.Distance(enemyPosition, playerPosition) > captureDistance)
        {
            return false;
        }

        CapturePlayer();
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCapturePlayerFromObject(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCapturePlayerFromObject(other.gameObject);
    }

    private void TryCapturePlayerFromObject(GameObject touchedObject)
    {
        if (hasCapturedPlayer || touchedObject == null)
        {
            return;
        }

        Transform touchedTransform = touchedObject.transform;

        if (!touchedObject.CompareTag("Player") && (touchedTransform.root == null || !touchedTransform.root.CompareTag("Player")))
        {
            return;
        }

        CapturePlayer();
    }

    private void CapturePlayer()
    {
        if (hasCapturedPlayer)
        {
            return;
        }

        hasCapturedPlayer = true;
        StopMoving();
        PlayCaptureAlarm();

        FadeController fade = Object.FindAnyObjectByType<FadeController>();

        if (fade != null)
        {
            fade.FadeAndReload();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void ConfigureCaptureAudio()
    {
        if (captureAlarmSource == null)
        {
            captureAlarmSource = GetComponent<AudioSource>();
        }

        if (captureAlarmSource == null && captureAlarmClip != null)
        {
            captureAlarmSource = gameObject.AddComponent<AudioSource>();
            captureAlarmSource.playOnAwake = false;
            captureAlarmSource.spatialBlend = 1f;
            captureAlarmSource.rolloffMode = AudioRolloffMode.Linear;
            captureAlarmSource.maxDistance = 20f;
        }
    }

    private void PlayCaptureAlarm()
    {
        if (captureAlarmSource == null || captureAlarmClip == null)
        {
            return;
        }

        captureAlarmSource.PlayOneShot(captureAlarmClip, captureAlarmVolume);
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureDistance);
    }
}
