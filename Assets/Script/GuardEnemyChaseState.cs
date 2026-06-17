public class GuardEnemyChaseState : IGuardEnemyState
{
    private readonly GuardEnemyAI guardEnemy;

    public GuardEnemyChaseState(GuardEnemyAI guardEnemy)
    {
        this.guardEnemy = guardEnemy;
    }

    public void Enter()
    {
        guardEnemy.MoveToLastSeenPlayerPosition(guardEnemy.ChaseSpeed);
    }

    public void Update()
    {
        if (guardEnemy.CanSeePlayer())
        {
            guardEnemy.MoveToLastSeenPlayerPosition(guardEnemy.ChaseSpeed);
            return;
        }

        if (!guardEnemy.MoveToLastSeenPlayerPosition(guardEnemy.ChaseSpeed))
        {
            guardEnemy.ChangeState(new GuardEnemyReturnToNodeState(guardEnemy));
            return;
        }

        if (guardEnemy.HasReachedDestination())
        {
            guardEnemy.ChangeState(new GuardEnemyInvestigateLastSeenState(guardEnemy));
        }
    }

    public void Exit() { }
}
