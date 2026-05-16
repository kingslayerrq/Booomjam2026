using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [Serializable]
    public class EnergyDrainWindow
    {
        public DayPhase phase = DayPhase.Day;
        [Range(0f, 24f)] public float startHour;
        [Range(0f, 24f)] public float endHour;
        [Min(0f)] public float energyDrainPerSecond;
    }

    [Header("Battery")] 
    [SerializeField] private float currentBatteryLevel;
    [SerializeField] private float maxBatteryLevel;
    
    [Header("Energy")] 
    [SerializeField] private bool drainEnergy = true;
    [SerializeField] private float currentEnergy;
    [SerializeField] private float maxEnergy;
    [Tooltip("Base energy drained per real-time second during the day.")]
    [Min(0f)]
    [SerializeField] private float dayEnergyDrainPerSecond;
    [Tooltip("Base energy drained per real-time second during the night.")]
    [Min(0f)]
    [SerializeField] private float nightEnergyDrainPerSecond;
    [Tooltip("Optional phase/hour-specific drain overrides. First matching window wins.")]
    [SerializeField] private List<EnergyDrainWindow> energyDrainWindows = new List<EnergyDrainWindow>();
    private float baseMaxEnergy;
    private float temporaryMaxEnergyPenalty;

    [Header("Lockup")]
    [SerializeField] private int[] lockupNumberByDay;
    private int _currentLockupNumber;
    
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PlayerHealth playerHealth;
    
    public float CurrentBatteryLevel => currentBatteryLevel;
    public float MaxBatteryLevel => maxBatteryLevel;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public int CurrentLockupNumber => _currentLockupNumber;
    public bool HasLockupChance => _currentLockupNumber > 0;

    public event Action OnBatteryLevelChanged;
    public event Action OnEnergyChanged;
    public event Action OnEnergyDepleted;
    public event Action OnLockupNumberChanged;

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
            float drainPerSecond = GetEnergyDrainPerSecond();
            if (drainPerSecond <= 0f)
                return;

            ReduceEnergy(drainPerSecond * Time.deltaTime);
            
            if (currentEnergy <= 0)
                OnEnergyDepleted?.Invoke();
        }
    }
    

    
    public void ResetResources()
    {
        ResetBattery();
        ResetEnergy();
        ResetLockupNumber(1);
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
        ResetLockupNumber(dayManager.CurrentDay);
    }

    private void ResetBattery()
    {
        currentBatteryLevel = maxBatteryLevel;
        OnBatteryLevelChanged?.Invoke();
    }

    private float GetEnergyDrainPerSecond()
    {
        if (dayManager == null)
            return 0f;

        if (TryGetEnergyDrainWindowRate(dayManager.CurrentPhase, dayManager.CurrentHour, out float windowRate))
            return windowRate;

        return dayManager.CurrentPhase == DayPhase.Night
            ? nightEnergyDrainPerSecond
            : dayEnergyDrainPerSecond;
    }

    private bool TryGetEnergyDrainWindowRate(DayPhase phase, float hour, out float rate)
    {
        rate = 0f;

        if (energyDrainWindows == null)
            return false;

        for (int i = 0; i < energyDrainWindows.Count; i++)
        {
            EnergyDrainWindow window = energyDrainWindows[i];
            if (window == null || window.phase != phase || !IsHourInsideWindow(hour, window.startHour, window.endHour))
                continue;

            rate = Mathf.Max(0f, window.energyDrainPerSecond);
            return true;
        }

        return false;
    }

    private static bool IsHourInsideWindow(float hour, float startHour, float endHour)
    {
        if (Mathf.Approximately(startHour, endHour))
            return false;

        if (startHour < endHour)
            return hour >= startHour && hour < endHour;

        return hour >= startHour || hour < endHour;
    }

    public bool TryUseLockupChance()
    {
        if (_currentLockupNumber <= 0)
            return false;

        _currentLockupNumber--;
        OnLockupNumberChanged?.Invoke();
        return true;
    }

    public void ResetLockupNumber(int day)
    {
        int index = Mathf.Clamp(day - 1, 0, lockupNumberByDay.Length - 1);
        _currentLockupNumber = Mathf.Max(0, lockupNumberByDay[index]);
        OnLockupNumberChanged?.Invoke();
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

    public void ReduceEnergyByMaxPercent(float percent)
    {
        ReduceEnergy(maxEnergy * Mathf.Clamp01(percent));
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
