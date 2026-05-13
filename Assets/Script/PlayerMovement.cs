using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public int speed = 5;
    public float jumpForce = 5f;
    private CharacterController controller;

    [SerializeField] private PlayerStamina stamina;

    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float fatigueSpeed = 3f;

    [SerializeField] private WorldSpaceBar staminaBar;



    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);

        stamina.isRunning = wantsToRun && move.magnitude > 0 && !stamina.isFatigued;

        float speed;

        if (stamina.isFatigued)
            speed = fatigueSpeed;
        else if (stamina.isRunning)
            speed = runSpeed;
        else
            speed = walkSpeed;

        controller.Move(move * speed * Time.deltaTime);
        transform.Translate(move.normalized * speed * Time.deltaTime);
    }
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {

        HandleMovement();

        staminaBar.SetValue(stamina.GetStaminaNormalized());
    }
}