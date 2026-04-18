public class PatrolState : IState
{
    private EnemyAI enemy;

    public PatrolState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter() { }

    public void Update()
    {
        enemy.Patrol();

        if (enemy.Vision.CanSeeTarget())
        {
            enemy.ChangeState(new AlertState(enemy));
        }
    }

    public void Exit() { }
}
