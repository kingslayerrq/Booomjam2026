using System;
using TMPro;
using UnityEngine;

public class PlayerResourceUI : MonoBehaviour
{
    [SerializeField] private PlayerResource playerResource;
    
    [Header("UI")]
    [SerializeField] private TMP_Text batteryText;

    private void OnEnable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged += UpdateBatteryLevelUI;
        }
        UpdateBatteryLevelUI();
    }

    private void OnDisable()
    {
        if (playerResource != null)
        {
            playerResource.OnBatteryLevelChanged -= UpdateBatteryLevelUI;
        }
    }

    private void UpdateBatteryLevelUI()
    {
        if (playerResource == null) return;
        if (batteryText == null) return;
        batteryText.text = $"Batter level: {playerResource.CurrentBatteryLevel}/{playerResource.MaxBatteryLevel}";
    }
}
