using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerBodyguardAnimator : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private const string VisualRootName = "BodyguardVisual";

    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStamina stamina;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float minimumMoveSpeed = 0.05f;
    [SerializeField] private float walkReferenceSpeed = 5f;
    [SerializeField] private float runReferenceSpeed = 8f;
    [SerializeField] private float turnSpeed = 18f;
    [SerializeField] private bool lockAnimatorRoot = true;
    [SerializeField] private string stoppedStateName = "Crouched Walking";

    private CharacterController controller;
    private Transform animatorRoot;
    private Vector3 lockedAnimatorLocalPosition;
    private Quaternion lockedAnimatorLocalRotation;
    private Vector3 smoothedDirection;
    private int stoppedStateHash;
    private bool hasSmoothedDirection;
    private bool wasMoving;

    private void Reset()
    {
        stamina = GetComponent<PlayerStamina>();
        ResolveAnimatorReferences();
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (stamina == null)
        {
            stamina = GetComponent<PlayerStamina>();
        }

        ResolveAnimatorReferences();
        CacheAnimatorRoot();
        stoppedStateHash = Animator.StringToHash(stoppedStateName);
    }

    private void OnValidate()
    {
        minimumMoveSpeed = Mathf.Max(0.01f, minimumMoveSpeed);
        walkReferenceSpeed = Mathf.Max(0.01f, walkReferenceSpeed);
        runReferenceSpeed = Mathf.Max(0.01f, runReferenceSpeed);
        turnSpeed = Mathf.Max(0f, turnSpeed);
        stoppedStateHash = Animator.StringToHash(stoppedStateName);
        ResolveAnimatorReferences();
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    private void LateUpdate()
    {
        KeepAnimatorRootStable();
    }

    private void Update()
    {
        if (animator == null || controller == null)
        {
            return;
        }

        Vector3 planarVelocity = controller.velocity;
        planarVelocity.y = 0f;

        float currentSpeed = planarVelocity.magnitude;
        bool isMoving = currentSpeed > minimumMoveSpeed;
        bool isRunning = isMoving && stamina != null && stamina.isRunning;

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(IsRunningHash, isRunning);

        if (!isMoving)
        {
            FreezeOnStoppedPose();
            hasSmoothedDirection = false;
            wasMoving = false;
            return;
        }

        float referenceSpeed = isRunning ? runReferenceSpeed : walkReferenceSpeed;
        animator.speed = Mathf.Clamp(currentSpeed / referenceSpeed, 0.75f, 1.35f);
        RotateVisualToward(planarVelocity);
        wasMoving = true;
    }

    private void FreezeOnStoppedPose()
    {
        if (wasMoving)
        {
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(stoppedStateHash, 0.08f);
        }

        animator.speed = 0f;
    }

    private void RotateVisualToward(Vector3 planarVelocity)
    {
        if (visualRoot == null || planarVelocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
        Vector3 targetDirection = targetRotation * Vector3.forward;

        if (!hasSmoothedDirection)
        {
            smoothedDirection = targetDirection;
            hasSmoothedDirection = true;
        }
        else
        {
            float blend = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            smoothedDirection = Vector3.Slerp(smoothedDirection, targetDirection, blend).normalized;
        }

        float yaw = Quaternion.LookRotation(smoothedDirection, Vector3.up).eulerAngles.y;
        visualRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void ResolveAnimatorReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (visualRoot == null && animator != null)
        {
            visualRoot = transform.Find(VisualRootName);

            if (visualRoot == null)
            {
                visualRoot = animator.transform;
            }
        }

        animatorRoot = animator != null ? animator.transform : null;
    }

    private void CacheAnimatorRoot()
    {
        if (animatorRoot == null)
        {
            return;
        }

        lockedAnimatorLocalPosition = animatorRoot.localPosition;
        lockedAnimatorLocalRotation = animatorRoot.localRotation;
    }

    private void KeepAnimatorRootStable()
    {
        if (!lockAnimatorRoot || animatorRoot == null)
        {
            return;
        }

        animatorRoot.localPosition = lockedAnimatorLocalPosition;
        animatorRoot.localRotation = lockedAnimatorLocalRotation;
    }
}
