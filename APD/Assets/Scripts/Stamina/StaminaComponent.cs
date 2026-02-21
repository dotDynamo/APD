using System;
using UnityEngine;

public class StaminaComponent : MonoBehaviour, IConsumable
{
    [SerializeField] private float maxStamina;
    [SerializeField] private float regenerationRate;
    [SerializeField] private float drainMultiplier = 1f;
    private float currentStamina;
    private  bool isExhausted;

    public event Action OnStaminaEmpty;
    //public event Action OnStaminaFullyRecovered;
    public event Action<float> OnStaminaChanged;

    public bool getIsExhausted(){ return isExhausted;}
    public void setDrainMultiplier(float newDrainMultiplier)
    {
        drainMultiplier = newDrainMultiplier;
    }

    public bool CanAfford(float amount)
    {
        if (isExhausted) { return false; }
        return amount * drainMultiplier < currentStamina;
    }

    public void Spend(float amount)
    {
        currentStamina -= amount * drainMultiplier;
        OnStaminaChanged?.Invoke(currentStamina);
        if (currentStamina <= 0.5)
        {
            isExhausted = true;
            OnStaminaEmpty?.Invoke();
        }
    }

    void Awake()
    {
        currentStamina =  maxStamina;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    void Update()
    {
        if ( currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            return;
        }
        currentStamina += regenerationRate * Time.deltaTime;
        OnStaminaChanged?.Invoke(currentStamina);
    }
}
