using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerResource : MonoBehaviour
{
    [Header("Battery")] 
    [SerializeField] private float currentBatteryLevel;
    [SerializeField] private float maxBatteryLevel;
    
    [Header("Energy")] 
    [SerializeField] private bool drainEnergy = true;
    [SerializeField] private float currentEnergy;
    [SerializeField] private float maxEnergy;
    [Tooltip("How long in seconds to fully deplete energy bar")]
    [SerializeField] private float secondsToDepleteEnergy;
    private float baseMaxEnergy;
    private float temporaryMaxEnergyPenalty;
    
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    public float CurrentBatteryLevel => currentBatteryLevel;
    public float MaxBatteryLevel => maxBatteryLevel;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    public event Action OnBatteryLevelChanged;
    public event Action OnEnergyChanged;

    private void Awake()
    {
        baseMaxEnergy = maxEnergy;

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted += ResetEnergyForNewDay;
            dayManager.OnNightStarted += ResetEnergy;
        }
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted -= ResetEnergyForNewDay;
            dayManager.OnNightStarted -= ResetEnergy;
        }
    }

    private void Update()
    {
        if (dayManager == null) return;
        if (!dayManager.IsTimeRunning) return;
        if (drainEnergy)
        {
            ReduceEnergy((maxEnergy / secondsToDepleteEnergy) * Time.deltaTime);
            
            if (currentEnergy <= 0)
            {
                if (dayManager.IsNightPhase)
                    playerHealth.TakeDamageFromEnergyDepletion();
                dayManager.ForceEndCurrentPhase();
            }
        }
    }
    

    
    public void ResetResources()
    {
        ResetBattery();
        ResetEnergy();
    }

    private void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke();
    }

    private void ResetEnergyForNewDay()
    {
        ClearTemporaryMaxEnergyPenalty();
        ResetEnergy();
    }

    private void ResetBattery()
    {
        currentBatteryLevel = maxBatteryLevel;
        OnBatteryLevelChanged?.Invoke();
    }
    
   
    
    #region BatteryFunc
    
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

    public void SetBatteryLevel(float value)
    {
        currentBatteryLevel = Mathf.Clamp(value, 0, maxBatteryLevel);
        OnBatteryLevelChanged?.Invoke();
    }
    
    public void SetMaxBatteryLevel(float value)
    {
        maxBatteryLevel = value;
        OnBatteryLevelChanged?.Invoke();
    }
    #endregion

    #region EnergyFunc
    public void ReduceEnergy(float value)
    {
        currentEnergy = Mathf.Max(currentEnergy - value, 0);
        OnEnergyChanged?.Invoke();
    }

    public void AddEnergy(float value)
    {
        currentEnergy = Mathf.Min(currentEnergy + value, maxEnergy);
        OnEnergyChanged?.Invoke();
    }

    public void SetEnergy(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0, maxEnergy);
        OnEnergyChanged?.Invoke();
    }

    public void SetMaxEnergy(float value)
    {
        baseMaxEnergy = Mathf.Max(0f, value);
        RecalculateMaxEnergy();
    }

    public void AddTemporaryMaxEnergyPenalty(float value)
    {
        temporaryMaxEnergyPenalty = Mathf.Max(temporaryMaxEnergyPenalty + value, 0f);
        RecalculateMaxEnergy();
    }

    public void ClearTemporaryMaxEnergyPenalty()
    {
        temporaryMaxEnergyPenalty = 0f;
        RecalculateMaxEnergy();
    }

    private void RecalculateMaxEnergy()
    {
        maxEnergy = Mathf.Max(1f, baseMaxEnergy - temporaryMaxEnergyPenalty);
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        OnEnergyChanged?.Invoke();
    }

    public void SetDrainEnergy(bool drain)
    {
        drainEnergy = drain;
    }

    #endregion
    
    
}
