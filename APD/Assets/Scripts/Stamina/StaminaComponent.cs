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

    public void SetDrainMultiplier(float newDrainMultiplier)
    {
        drainMultiplier = newDrainMultiplier;
    }

    public void Recover()
    {
        
    }

    void Awake()
    {
        currentStamina =  maxStamina;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    void Update()
    {
        if (!isExhausted)
        {
            currentStamina = Mathf.Clamp(regenerationRate * Time.deltaTime, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina);
        }
    }
}
