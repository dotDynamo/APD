using UnityEngine;

[CreateAssetMenu(fileName = "SOSquirrel", menuName = "Scriptable Objects/SOSquirrel")]
public class SOSquirrel : ScriptableObject
{
    public string squirrelType;
    public int maxStamina;
    public float climbDrainRate;
    public bool isInvertedDrainRate;
    public float invertedDrainRate;
    public float sprintDrainRate;
    public int dashCost;
    public float regenRate;
}
