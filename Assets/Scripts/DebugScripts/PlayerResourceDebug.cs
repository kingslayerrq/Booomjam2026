using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
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
        if (Keyboard.current[toggleDebugPanelKey].wasPressedThisFrame)
        {
            if (resourceDebugPanel != null)
                resourceDebugPanel.SetActive(!resourceDebugPanel.activeSelf);
        }
    }

    public void AddBattery()
    {
        playerResource.AddBatteryLevel(float.TryParse(addBatteryAmountInput.text, out float amount) ? amount : 0);
    }

    public void ReduceBattery()
    {
        playerResource.ReduceBatteryLevel(float.TryParse(reduceBatteryAmountInput.text, out float amount) ? amount : 0);
    }

    public void SetMaxBattery()
    {
        playerResource.SetMaxBatteryLevel(float.Parse(maxBatteryAmountInput.text));
    }

    public void AddEnergy()
    {
        playerResource.AddEnergy(float.TryParse(addEnergyAmountInput.text,  out float amount) ? amount : 0);
    }

    public void ReduceEnergy()
    {
        playerResource.ReduceEnergy(float.TryParse(reduceEnergyAmountInput.text,  out float amount) ? amount : 0);
    }

    
    public void SetMaxEnergy()
    {
        playerResource.SetMaxEnergy(float.Parse(maxEnergyAmountInput.text));
    }

    public void SetCurrentEnergy()
    {
        playerResource.SetEnergy(float.Parse(energyAmountInput.text));
    }

    public void SetEnergyDrain()
    {
        playerResource.SetDrainEnergy(energyDrainToggle.isOn);
    }
}
