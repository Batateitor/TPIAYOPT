using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("Rates")]
    public float drainRate = 20f;
    public float regenRate = 10f;
    public float fatigueRegenRate = 5f;

    [Header("State")]
    public bool isRunning;
    public bool isFatigued;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleStamina();
    }

    private void HandleStamina()
    {
        if (isRunning && !isFatigued)
        {
            currentStamina -= drainRate * Time.deltaTime;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isFatigued = true;
            }
        }
        else
        {
            float regen = isFatigued ? fatigueRegenRate : regenRate;
            currentStamina += regen * Time.deltaTime;

            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                isFatigued = false;
            }
        }
    }

    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }
}