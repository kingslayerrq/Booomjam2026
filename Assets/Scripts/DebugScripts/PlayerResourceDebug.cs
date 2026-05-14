using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerResourceDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerResource playerResource;
    [SerializeField] private GameObject resourceDebugPanel;
    

    [Header("UI References")]
    [SerializeField] private TMP_InputField addBatteryAmountInput;
    [SerializeField] private TMP_InputField reduceBatteryAmountInput;
    [SerializeField] private TMP_InputField maxBatteryAmountInput;
    [SerializeField] private TMP_InputField addEnergyAmountInput;
    [SerializeField] private TMP_InputField reduceEnergyAmountInput;
    [SerializeField] private TMP_InputField energyAmountInput;
    [SerializeField] private TMP_InputField maxEnergyAmountInput;
    [SerializeField] private Toggle energyDrainToggle;

    [Header("Settings")] 
    [SerializeField] private Key toggleDebugPanelKey;

    private void Awake()
    {
        if (resourceDebugPanel != null) 
        {
            resourceDebugPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || toggleDebugPanelKey == Key.None)
            return;

        if (Keyboard.current[toggleDebugPanelKey].wasPressedThisFrame)
        {
            ToggleDebugPanel();
        }
    }

    public void ToggleDebugPanel()
    {
        if (resourceDebugPanel == null)
            return;

        resourceDebugPanel.SetActive(!resourceDebugPanel.activeSelf);
    }

    public void AddBattery()
    {
        if (!CanModifyResource() || !TryReadValue(addBatteryAmountInput, out float amount))
            return;

        playerResource.AddBatteryLevel(amount);
    }

    public void ReduceBattery()
    {
        if (!CanModifyResource() || !TryReadValue(reduceBatteryAmountInput, out float amount))
            return;

        playerResource.ReduceBatteryLevel(amount);
    }

    public void SetMaxBattery()
    {
        if (!CanModifyResource() || !TryReadValue(maxBatteryAmountInput, out float amount))
            return;

        playerResource.SetMaxBatteryLevel(amount);
    }

    public void AddEnergy()
    {
        if (!CanModifyResource() || !TryReadValue(addEnergyAmountInput, out float amount))
            return;

        playerResource.AddEnergy(amount);
    }

    public void ReduceEnergy()
    {
        if (!CanModifyResource() || !TryReadValue(reduceEnergyAmountInput, out float amount))
            return;

        playerResource.ReduceEnergy(amount);
    }

    
    public void SetMaxEnergy()
    {
        if (!CanModifyResource() || !TryReadValue(maxEnergyAmountInput, out float amount))
            return;

        playerResource.SetMaxEnergy(amount);
    }

    public void SetCurrentEnergy()
    {
        if (!CanModifyResource() || !TryReadValue(energyAmountInput, out float amount))
            return;

        playerResource.SetEnergy(amount);
    }

    public void SetEnergyDrain()
    {
        if (!CanModifyResource() || energyDrainToggle == null)
            return;

        playerResource.SetDrainEnergy(energyDrainToggle.isOn);
    }

    private bool CanModifyResource()
    {
        if (playerResource != null)
            return true;

        Debug.LogWarning("[PlayerResourceDebug] PlayerResource is not assigned.", this);
        return false;
    }

    private bool TryReadValue(TMP_InputField inputField, out float value)
    {
        value = 0f;

        if (inputField == null)
            return false;

        if (float.TryParse(inputField.text, out value))
            return true;

        Debug.LogWarning($"[PlayerResourceDebug] Invalid number: '{inputField.text}'.", inputField);
        return false;
    }
}
