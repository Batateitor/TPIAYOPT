using UnityEngine;

public class AlertState : IState
{
    private EnemyAI enemy;
    private float timer = 2f;

    public AlertState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter() { }

    public void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            enemy.ChangeState(new ChaseState(enemy));
        }
    }

    public void Exit() { }
}