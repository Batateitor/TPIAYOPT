using UnityEngine;

public class AlertState : IState
{
    private CameraEnemyAI enemy;

    private Transform player;

    private float detectionTime = 3f;
    private float currentDetection;

    public AlertState(CameraEnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        currentDetection = 0f;

        if (enemy.Audio != null)
            enemy.Audio.PlayAlert();

        if (enemy.DetectionBar != null)
            enemy.DetectionBar.gameObject.SetActive(true);
    }

    public void Update()
    {
        if (enemy == null || enemy.Vision == null || player == null) return;

        bool canSee = enemy.Vision.CanSeeTarget();

        if (canSee)
        {
            currentDetection += Time.deltaTime;
        }
        else
        {
            currentDetection -= Time.deltaTime;
        }

        currentDetection = Mathf.Clamp(currentDetection, 0, detectionTime);

        if (enemy.DetectionBar != null)
        {
            enemy.DetectionBar.SetValue(currentDetection / detectionTime);
        }

        if (canSee)
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

        if (!canSee && currentDetection <= 0)
        {
            enemy.ChangeState(new PatrolState(enemy));
            return;
        }

        if (currentDetection >= detectionTime)
        {
            enemy.ChangeState(new ChaseState(enemy));
        }
    }

    public void Exit()
    {
        if (enemy.DetectionBar != null)
            enemy.DetectionBar.gameObject.SetActive(false);
    }
}