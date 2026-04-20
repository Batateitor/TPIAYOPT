using UnityEngine;

public class AlertState : IState
{
    private EnemyAI enemy;
    private float timer;
    private float alertDuration = 2f;
    private Transform player;

    public AlertState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = alertDuration;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

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

        if (player != null)
        {
            Vector3 direction = player.position - enemy.transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation,
                    targetRotation,
                    enemy.RotationSpeed * Time.deltaTime
                );
            }
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            enemy.ChangeState(new ChaseState(enemy));
        }
    }

    public void Exit() { }
}