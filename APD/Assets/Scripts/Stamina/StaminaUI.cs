using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class StaminaUI : MonoBehaviour
{
    [SerializeField] private StaminaComponent staminaComponent;
    [SerializeField] private string textValue;
    [SerializeField] private TMP_Text text;

    void Awake()
    {
        staminaComponent.OnStaminaChanged += UpdateUI;
        staminaComponent.OnStaminaEmpty += RecolorEmptyUI;
        staminaComponent.OnStaminaFullyRecovered += RecolorFullUI;
    }

     void UpdateUI(float newStaminaValue)
    {
        text.color = Color.white;
        textValue = newStaminaValue.ToString("00.00");
        text.text = textValue;    
    }

    void RecolorEmptyUI()
    {
        text.color = Color.red;
        text.text = "Exhausted";
    }

    void RecolorFullUI()
    {
        text.color = Color.green;
    }
}