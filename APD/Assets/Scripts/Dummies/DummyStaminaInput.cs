using UnityEngine;

public class DummyStaminaInput : MonoBehaviour
{
    public StaminaComponent stamina;
    public float spendAmount = 10f;
    public float multiplier = 1f;

    void Start()
    {
        stamina.setDrainMultiplier(multiplier);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (stamina != null)
            {
                if (stamina.CanAfford(spendAmount))
                {
                    stamina.Spend(spendAmount);
                }
            } 
        }
    }
}