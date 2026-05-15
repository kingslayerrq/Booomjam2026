using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [SerializeField] private PlayerResource playerResource;
    
    [Header("Battery Segments (Left)")]
    [SerializeField] private Image[] batterySegments;

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
        if (playerResource == null || batterySegments == null || batterySegments.Length == 0) return;

        // Loop through all assigned segment images
        for (int i = 0; i < batterySegments.Length; i++)
        {
            batterySegments[i].enabled = (i < playerResource.CurrentBatteryLevel);
        }
    }

    private void UpdateEnergyUI()
    {
        if (playerResource == null) return;
        if (energyImageFill == null) return;
        energyImageFill.fillAmount = Mathf.Clamp01(playerResource.CurrentEnergy / playerResource.MaxEnergy);
    }
}
