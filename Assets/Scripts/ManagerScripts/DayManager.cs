using System;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    [Header("Time Settings")] 
    [SerializeField] private int currentDay;
    [SerializeField] private int totalDays;
    [SerializeField] private DayTimeConfig timeConfig;
    [SerializeField] private DayPhase currentPhase = DayPhase.Day;
    
    private float elapsedPhaseTimeInSeconds;
    private bool isTimeRunning;
    private float currentPhaseDurationInSeconds;
    
    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public DayPhase CurrentPhase => currentPhase;
    public float StartHour => timeConfig != null ? timeConfig.PhaseStartHour(currentPhase) : 0f;
    public float EndHour => timeConfig != null ? timeConfig.PhaseEndHour(currentPhase) : 0f;
    public bool IsDayPhase => isTimeRunning && currentPhase == DayPhase.Day;
    public bool IsNightPhase => isTimeRunning && currentPhase == DayPhase.Night;
    public float DayDurationInSeconds => timeConfig != null ? timeConfig.TotalRealDuration(DayPhase.Day) : 0f;
    public float PhaseDurationInSeconds => currentPhaseDurationInSeconds;
    public float RemainingSeconds => Mathf.Max(0f, currentPhaseDurationInSeconds - elapsedPhaseTimeInSeconds);
    public float NormalizedTime => currentPhaseDurationInSeconds <= 0f ? 1f : elapsedPhaseTimeInSeconds / currentPhaseDurationInSeconds;
    public float CurrentHour => timeConfig != null ? timeConfig.GameHourAtRealSeconds(currentPhase, elapsedPhaseTimeInSeconds) : 0f;
    public float DisplayHour => Mathf.Repeat(CurrentHour, 24f);
    public bool IsDayRunning => IsDayPhase;
    public bool IsTimeRunning => isTimeRunning;

    public event Action OnDayInitialized;       // Setup, UIs
    public event Action OnDayStarted;
    public event Action OnNightStarted;
    public event Action OnNightEnded;
    public event Action OnDayEnded;
    public event Action OnPhaseChanged;
    public event Action OnTimeChanged;
    

    private void Update()
    {
        if (!isTimeRunning) return;
        
        elapsedPhaseTimeInSeconds += Time.deltaTime;
        OnTimeChanged?.Invoke();

        if (elapsedPhaseTimeInSeconds >= currentPhaseDurationInSeconds)
        {
            CompleteCurrentPhase();
        }
    }

    public void StartDay(int day)
    {
        StartDay(day, DayPhase.Day);
    }

    public void StartDay(int day, DayPhase phase)
    {
        if (day > totalDays) return;
        
        currentDay = day;
        OnDayInitialized?.Invoke();
        StartPhase(phase);

        Debug.Log($"Day {currentDay} started in {currentPhase} phase.");
    }

    public void StartDay(int day, bool fromMorning)
    {
        StartDay(day, fromMorning ? DayPhase.Day : DayPhase.Night);
    }

    public void JumpToNight(string reason = "")
    {
        if (!isTimeRunning || currentPhase == DayPhase.Night)
            return;

        StartPhase(DayPhase.Night);

        if (string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"Day {currentDay} jumped to Night.");
        }
        else
        {
            Debug.Log($"Day {currentDay} jumped to Night: {reason}");
        }
    }
    
    /// <summary>
    /// Force the current active phase to end.
    /// </summary>
    public void ForceEndCurrentPhase()
    {
        CompleteCurrentPhase();
    }

    public void ForceEndHalfDay()
    {
        ForceEndCurrentPhase();
    }

    private void StartPhase(DayPhase phase)
    {
        currentPhase = phase;
        elapsedPhaseTimeInSeconds = 0f;
        currentPhaseDurationInSeconds = timeConfig != null ? timeConfig.TotalRealDuration(currentPhase) : 0f;
        isTimeRunning = true;

        OnPhaseChanged?.Invoke();
        OnTimeChanged?.Invoke();

        if (currentPhase == DayPhase.Day)
        {
            OnDayStarted?.Invoke();
        }
        else
        {
            OnNightStarted?.Invoke();
        }

        GameAudioManager.Instance.PlayRandomAlarm();
    }

    private void CompleteCurrentPhase()
    {
        if (currentPhase == DayPhase.Day)
        {
            StartPhase(DayPhase.Night);
            return;
        }

        EndNightAndDay();
    }

    private void EndNightAndDay()
    {
        isTimeRunning = false;
        OnNightEnded?.Invoke();
        OnDayEnded?.Invoke();

        Debug.Log($"Day {currentDay} night ended.");
    }

    public void EndDayImmediately()
    {
        isTimeRunning = false;
        OnDayEnded?.Invoke();

        Debug.Log($"Day {currentDay} ended.");
    }

    /// <summary>
    /// Stops time
    /// </summary>
    public void StopDay()
    {
        isTimeRunning = false;
        
        Debug.Log($"Day {currentDay} stopped.");
    }
}
