using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [SerializeField] private PlayerResource playerResource;
    
    [Header("UI")]
    [SerializeField] private TMP_Text batteryText;

    [SerializeField] private Image energyImageFill;

    private void OnEnable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged += UpdateBatteryLevelUI;
            playerResource.OnEnergyChanged += UpdateEnergyUI;
        }
        UpdateBatteryLevelUI();
        UpdateEnergyUI();
    }

    private void OnDisable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged -= UpdateBatteryLevelUI;
            playerResource.OnEnergyChanged -= UpdateEnergyUI;
        }
    }

    private void UpdateBatteryLevelUI()
    {
        if (playerResource == null) return;
        if (batteryText == null) return;
        batteryText.text = $"Battery level: {playerResource.CurrentBatteryLevel}/{playerResource.MaxBatteryLevel}";
    }

    private void UpdateEnergyUI()
    {
        if (playerResource == null) return;
        if (energyImageFill == null) return;
        energyImageFill.fillAmount = Mathf.Clamp01(playerResource.CurrentEnergy / playerResource.MaxEnergy);
    }
}
