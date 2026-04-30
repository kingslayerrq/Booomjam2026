using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerResourceDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerResource playerResource;
    [SerializeField] private GameObject resourceDebugPanel;
    
    [Header("UI References")]
    [SerializeField] private TMP_InputField addAmountInput;
    [SerializeField] private TMP_InputField reduceAmountInput;
    [SerializeField] private TMP_InputField maxBatteryAmountInput;

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
        playerResource.AddBatteryLevel(float.TryParse(addAmountInput.text, out float amount) ? amount : 0);
    }

    public void ReduceBattery()
    {
        playerResource.ReduceBatteryLevel(float.TryParse(reduceAmountInput.text, out float amount) ? amount : 0);
    }

    public void SetMaxBattery()
    {
        playerResource.SetMaxBatteryLevel(float.Parse(maxBatteryAmountInput.text));
    }
}
