using UnityEngine;

public class ChaseState : IState
{
    private CameraEnemyAI enemy;

    public ChaseState(CameraEnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        FadeController fade = Object.FindFirstObjectByType<FadeController>();

        if (fade != null)
        {
            fade.FadeAndReload();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    public void Update() { }

    public void Exit() { }
}