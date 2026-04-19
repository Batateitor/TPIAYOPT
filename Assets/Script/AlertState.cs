using UnityEngine;

public class AlertState : IState
{
    private EnemyAI enemy;
    private float timer;
    private float alertDuration = 2f;

    public AlertState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = alertDuration;

        if (enemy.Audio != null)
            enemy.Audio.PlayAlert();
    }

    public void Update()
    {
        if (enemy == null || enemy.Vision == null) return;

        if (!enemy.Vision.CanSeeTarget())
        {
            enemy.ChangeState(new PatrolState(enemy));
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            enemy.ChangeState(new ChaseState(enemy));
        }
    }

    public void Exit() { }
}