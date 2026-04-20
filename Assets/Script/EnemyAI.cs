using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private IState currentState;
    public EnemyAudio Audio;
    public VisionSystem Vision;
    [SerializeField] private float rotationSpeed = 35f;

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
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}