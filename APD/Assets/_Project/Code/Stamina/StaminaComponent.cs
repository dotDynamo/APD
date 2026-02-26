using System;
using UnityEngine;
using System.Collections;

public class StaminaComponent : MonoBehaviour, IConsumable
{
    [SerializeField] private float maxStamina;
    [SerializeField] private float regenerationRate;
    [SerializeField] private float drainMultiplier = 1f;
    private float currentStamina;
    private float staminaSpendBuffer = 1;
    private float recoveryWaitTime = 3f;
    private  bool isExhausted;
    private bool isConsuming;

    public event Action OnStaminaEmpty;
    public event Action OnStaminaFullyRecovered;
    public event Action<float> OnStaminaChanged;

    public bool getIsExhausted(){ return isExhausted;}

    public bool CanAfford(float amount)
    {
        if (isExhausted) { return false; }
        return amount  < currentStamina + staminaSpendBuffer;
    }

    public void Spend(float amount)
    {
        currentStamina -= amount;
        OnStaminaChanged?.Invoke(currentStamina);
        checkExhaustion();
    }

    public void ToggleConsume()
    {
        isConsuming = !isConsuming;
    }

    public void SetDrainMultiplier(float newDrainMultiplier)
    {
        drainMultiplier = newDrainMultiplier;
    }

    public void Recover()
    {
        currentStamina = Mathf.Clamp(currentStamina + regenerationRate * Time.deltaTime, 0f, maxStamina);
        if (currentStamina == maxStamina) 
        {
            OnStaminaFullyRecovered?.Invoke();
            return;
        }
        OnStaminaChanged?.Invoke(currentStamina);
    }

    private void checkExhaustion()
    {
        if (currentStamina > 0) return;

        OnStaminaEmpty?.Invoke();
        currentStamina = 0;
        StartCoroutine(RecoveryDelay());
    }

     IEnumerator RecoveryDelay()
    {
        isExhausted = true;
        yield return new WaitForSeconds(recoveryWaitTime);
        isExhausted = false;
    }
    void Awake()
    {
        currentStamina =  maxStamina;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    void Update()
    {
        if (isConsuming)
        {
            Spend(drainMultiplier  * Time.deltaTime);
        }
        else if (!isExhausted && currentStamina < maxStamina)
        {
            Recover();
        }
    }
}
