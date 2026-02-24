
public interface IConsumable
{
    bool CanAfford(float amount);
    void Spend(float amount);
    void SetDrainMultiplier(float multiplier);
    void Recover();
}