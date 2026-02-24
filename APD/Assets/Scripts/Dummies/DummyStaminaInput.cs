using UnityEngine;
using UnityEngine.InputSystem;

public class DummyStaminaInput : MonoBehaviour
{
    public IConsumable stamina;
    private DummyInputActions inputActions;
    public float spendAmount = 10f;
    public float multiplier = 1f;

    void Start()
    {
        stamina.SetDrainMultiplier(multiplier);
    }
    void Awake()
    {
        inputActions = new DummyInputActions();
        inputActions.Player.Spend.performed += OnSpendPerformed;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void OnSpendPerformed(InputAction.CallbackContext context)
    {
        if (stamina != null && stamina.CanAfford(spendAmount))
        {
            stamina.Spend(spendAmount);
        }
    }
}