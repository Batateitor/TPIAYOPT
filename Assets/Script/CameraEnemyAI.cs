using System.Runtime.CompilerServices;
using UnityEngine;

public class CameraEnemyAI : MonoBehaviour
{
    private IState currentState;
    public EnemyAudio Audio;
    public VisionSystem Vision;
    public WorldSpaceBar DetectionBar;
    [SerializeField] private float rotationSpeed = 35f;
    [SerializeField] private float rotationFollowSpeed = 3.5f;
    public float RotationSpeed => rotationFollowSpeed;

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