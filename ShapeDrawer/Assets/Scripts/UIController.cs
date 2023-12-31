using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] MouseController mouseController;
    [SerializeField] TMP_InputField MassInput;
    [SerializeField] TMP_InputField BreakForceInput;
    [SerializeField] TMP_InputField BreakTorqueInput;
    [SerializeField] Toggle SolidToggle;

    private void Start()
    {
        
        MassInput.onValueChanged.AddListener(delegate { UpdateValue("mass", MassInput.text); });
        BreakForceInput.onValueChanged.AddListener(delegate { UpdateValue("breakForce", BreakForceInput.text); });
        BreakTorqueInput.onValueChanged.AddListener(delegate { UpdateValue("breakTorque", BreakTorqueInput.text); });
        SolidToggle.onValueChanged.AddListener(delegate { UpdateToggleValue("solid", SolidToggle.isOn); });
    }

    private void UpdateValue(string name, string value)
    {
        decimal.TryParse(value, out decimal decimalValue);
        mouseController.HandleValueChange(name, decimalValue);
    }

    private void UpdateToggleValue(string name, bool state)
    {
        mouseController.ChangeToggleState(name, state);
    }
}
