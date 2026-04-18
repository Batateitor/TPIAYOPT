using UnityEngine.SceneManagement;

public class ChaseState : IState
{
    private EnemyAI enemy;

    public ChaseState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Update() { }

    public void Exit() { }
}