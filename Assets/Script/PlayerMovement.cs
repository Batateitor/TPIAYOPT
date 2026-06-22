using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string MoveActionName = "Move";
    private const string SprintActionName = "Sprint";

    public int speed = 5;
    public float jumpForce = 5f;
    private CharacterController controller;

    [SerializeField] private PlayerStamina stamina;
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float fatigueSpeed = 3f;

    [SerializeField] private WorldSpaceBar staminaBar;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputActionMap runtimeActionMap;
    private bool usingRuntimeActions;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (stamina == null)
            stamina = GetComponent<PlayerStamina>();

        SetUpInputActions();
    }

    private void OnEnable()
    {
        if (moveAction == null || sprintAction == null)
            SetUpInputActions();

        moveAction?.Enable();
        sprintAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();

        if (stamina != null)
            stamina.isRunning = false;
    }

    private void OnDestroy()
    {
        if (usingRuntimeActions)
            runtimeActionMap?.Dispose();
    }

    private void HandleMovement()
    {
        if (controller == null || stamina == null)
            return;

        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 move = new Vector3(input.x, 0, input.y);
        move = Vector3.ClampMagnitude(move, 1f);

        bool wantsToRun = sprintAction != null && sprintAction.IsPressed();

        stamina.isRunning = wantsToRun && move.sqrMagnitude > 0.001f && !stamina.isFatigued;

        float speed;

        if (stamina.isFatigued)
            speed = fatigueSpeed;
        else if (stamina.isRunning)
            speed = runSpeed;
        else
            speed = walkSpeed;

        controller.Move(move * speed * Time.deltaTime);
    }

    private void SetUpInputActions()
    {
        if (inputActions != null)
        {
            InputActionMap playerMap = inputActions.FindActionMap(PlayerActionMapName, false);

            if (playerMap != null)
            {
                moveAction = playerMap.FindAction(MoveActionName, false);
                sprintAction = playerMap.FindAction(SprintActionName, false);
            }
        }

        if (moveAction != null && sprintAction != null)
            return;

        CreateRuntimeInputActions();
    }

    private void CreateRuntimeInputActions()
    {
        runtimeActionMap?.Dispose();
        runtimeActionMap = new InputActionMap(PlayerActionMapName);
        usingRuntimeActions = true;

        moveAction = runtimeActionMap.AddAction(MoveActionName, InputActionType.Value, expectedControlLayout: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Joystick>/stick");

        sprintAction = runtimeActionMap.AddAction(SprintActionName, InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");
    }

    void Update()
    {

        HandleMovement();

        if (staminaBar != null && stamina != null)
            staminaBar.SetValue(stamina.GetStaminaNormalized());
    }
}
