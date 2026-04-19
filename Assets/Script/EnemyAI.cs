using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private IState currentState;
    public EnemyAudio Audio;
    public VisionSystem Vision;

    void Start()
    {
        ChangeState(new PatrolState(this));
    }

    void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Patrol()
    {
        transform.Rotate(Vector3.up * 20f * Time.deltaTime);
    }
}