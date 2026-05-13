public class PatrolState : IState
{
    private CameraEnemyAI enemy;

    public PatrolState(CameraEnemyAI enemy)
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
