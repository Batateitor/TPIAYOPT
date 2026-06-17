public class GuardEnemyPatrolState : IGuardEnemyState
{
    private readonly GuardEnemyAI guardEnemy;
    private bool destinationSet;

    public GuardEnemyPatrolState(GuardEnemyAI guardEnemy)
    {
        this.guardEnemy = guardEnemy;
    }

    public void Enter()
    {
        destinationSet = guardEnemy.MoveToCurrentPatrolNode();
    }

    public void Update()
    {
        if (guardEnemy.CanSeePlayer())
        {
            guardEnemy.ChangeState(new GuardEnemyChaseState(guardEnemy));
            return;
        }

        if (!guardEnemy.HasPatrolNodes)
        {
            guardEnemy.StopMoving();
            return;
        }

        if (!destinationSet)
        {
            guardEnemy.AdvanceToNextPatrolNode();
            destinationSet = guardEnemy.MoveToCurrentPatrolNode();
            return;
        }

        if (guardEnemy.HasReachedDestination())
        {
            guardEnemy.MarkCurrentNodeVisitedAndAdvance();
            destinationSet = guardEnemy.MoveToCurrentPatrolNode();
        }
    }

    public void Exit() { }
}
