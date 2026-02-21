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
        staminaComponent.OnStaminaEmpty += RecolorUI;
    }

     void UpdateUI(float newStaminaValue)
    {
        textValue = newStaminaValue.ToString();
        text.text = textValue;    
    }

    void RecolorUI()
    {
        text.color = Color.red;
    }
}