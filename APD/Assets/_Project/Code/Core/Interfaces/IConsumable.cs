
public interface IConsumable
{
    bool CanAfford(float amount);
    void Spend(float amount);
    void ToggleConsume();
    void SetDrainMultiplier(float multiplier);
    void Recover();
}