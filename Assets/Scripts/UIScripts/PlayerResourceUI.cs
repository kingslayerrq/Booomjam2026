using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [SerializeField] private PlayerResource playerResource;
    
    [Header("Battery Segments")]
    [SerializeField] private Image[] batterySegments;
    
    [Header("Energy Image")]
    [SerializeField] private Image energyImageFill;

    [Header("Lockup")] 
    [SerializeField] private TextMeshProUGUI lockupLabel;

    private void OnEnable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged += UpdateBatteryLevelUI;
            playerResource.OnEnergyChanged += UpdateEnergyUI;
            playerResource.OnLockupNumberChanged += UpdateLockupLabel;
        }
        UpdateBatteryLevelUI();
        UpdateEnergyUI();
        UpdateLockupLabel();
    }

    private void OnDisable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged -= UpdateBatteryLevelUI;
            playerResource.OnEnergyChanged -= UpdateEnergyUI;
            playerResource.OnLockupNumberChanged -= UpdateLockupLabel;
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

    private void UpdateLockupLabel()
    {
        if (playerResource == null) return;
        if (lockupLabel == null) return;
        lockupLabel.text = $"x{playerResource.CurrentLockupNumber}";
    }
}
