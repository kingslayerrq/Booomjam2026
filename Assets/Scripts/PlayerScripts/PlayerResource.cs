using System;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [Header("Battery")] 
    [SerializeField] private float currentBatteryLevel;
    [SerializeField] private float maxBatteryLevel;
    
    public float CurrentBatteryLevel => currentBatteryLevel;
    public float MaxBatteryLevel => maxBatteryLevel;

    public event Action OnBatteryLevelChanged;
    
    public void ResetResources()
    {
        currentBatteryLevel = maxBatteryLevel;
        OnBatteryLevelChanged?.Invoke();
    }
    
    public void SetResources(float batteryLevel)
    {
        currentBatteryLevel = batteryLevel;
        OnBatteryLevelChanged?.Invoke();
    }
    
    #region Debug
    
    public void AddBatteryLevel(float value)
    {
        currentBatteryLevel =  Mathf.Min(currentBatteryLevel + value, maxBatteryLevel);
        OnBatteryLevelChanged?.Invoke();
    }

    public void ReduceBatteryLevel(float value)
    {
        currentBatteryLevel = Mathf.Max(currentBatteryLevel - value, 0);
        OnBatteryLevelChanged?.Invoke();
    }

    public void SetMaxBatteryLevel(float value)
    {
        maxBatteryLevel = value;
        OnBatteryLevelChanged?.Invoke();
    }

    #endregion
    
}
