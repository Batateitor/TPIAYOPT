public class GuardEnemyReturnToNodeState : IGuardEnemyState
{
    private readonly GuardEnemyAI guardEnemy;
    private bool destinationSet;

    public GuardEnemyReturnToNodeState(GuardEnemyAI guardEnemy)
    {
        this.guardEnemy = guardEnemy;
    }

    public void Enter()
    {
        destinationSet = guardEnemy.MoveToLastVisitedNode();
    }

    public void Update()
    {
        if (guardEnemy.CanSeePlayer())
        {
            guardEnemy.ChangeState(new GuardEnemyChaseState(guardEnemy));
            return;
        }

        if (!destinationSet)
        {
            guardEnemy.ChangeState(new GuardEnemyPatrolState(guardEnemy));
            return;
        }

        if (guardEnemy.HasReachedDestination())
        {
            guardEnemy.ResumePatrolAfterReturn();
            guardEnemy.ChangeState(new GuardEnemyPatrolState(guardEnemy));
        }
    }

    public void Exit() { }
}
