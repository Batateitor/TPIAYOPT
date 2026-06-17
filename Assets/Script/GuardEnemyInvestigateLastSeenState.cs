using UnityEngine;

public class GuardEnemyInvestigateLastSeenState : IGuardEnemyState
{
    private readonly GuardEnemyAI guardEnemy;
    private float waitTimer;
    private bool reachedLastSeenPosition;

    public GuardEnemyInvestigateLastSeenState(GuardEnemyAI guardEnemy)
    {
        this.guardEnemy = guardEnemy;
    }

    public void Enter()
    {
        waitTimer = 0f;
        reachedLastSeenPosition = false;
        guardEnemy.MoveToLastSeenPlayerPosition(guardEnemy.ChaseSpeed);
    }

    public void Update()
    {
        if (guardEnemy.CanSeePlayer())
        {
            guardEnemy.ChangeState(new GuardEnemyChaseState(guardEnemy));
            return;
        }

        if (!reachedLastSeenPosition)
        {
            if (!guardEnemy.HasReachedDestination())
            {
                return;
            }

            reachedLastSeenPosition = true;
            guardEnemy.StopMoving();
        }

        waitTimer += Time.deltaTime;

        if (waitTimer >= guardEnemy.InvestigationWaitTime)
        {
            guardEnemy.ChangeState(new GuardEnemyReturnToNodeState(guardEnemy));
        }
    }

    public void Exit() { }
}
