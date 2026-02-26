using UnityEngine;
using UnityEngine.InputSystem;

public class DummyStaminaInput : MonoBehaviour
{
    private IConsumable stamina;
    private DummyInputActions inputActions;
    public float spendAmount = 1f;
    public float multiplier = 1f;

    void Awake()
    {
        inputActions = new DummyInputActions();
        inputActions.Player.Spend.performed += OnSpendPerformed;
        inputActions.Player.Consume.started += OnConsumedStarted;
        inputActions.Player.Consume.canceled += OnConsumedCanceled;

        stamina = GetComponentInParent<IConsumable>();
    }

    void Start()
    {
        stamina.SetDrainMultiplier(multiplier);
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

    void OnConsumedStarted(InputAction.CallbackContext context)
    {
        if (stamina != null)
        {
            stamina.ToggleConsume();
        }
    }

    void OnConsumedCanceled(InputAction.CallbackContext context)
    {
        if(stamina != null)
        {
             stamina.ToggleConsume();
        }
    }
}