using System;
using UnityEngine;
using UnityEngine.Serialization;

public class DayManager : MonoBehaviour
{
    [Header("Time Settings")] 
    [SerializeField] private int currentDay;
    [SerializeField] private int totalDays;
    [SerializeField] private DayTimeConfig timeConfig;
    [SerializeField] private float noonHour;
    
    private float elapsedTimeInSeconds;
    private bool isDayRunning;
    private float _totalRealDuration;
    private float _realSecondsToNoon;
    
    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public float StartHour => timeConfig.segments[0].gameStartHour;
    public float EndHour => timeConfig.segments[timeConfig.segments.Count - 1].gameEndHour;
    public bool IsMorning => isDayRunning && elapsedTimeInSeconds < _realSecondsToNoon;
    public float DayDurationInSeconds => _totalRealDuration;
    public float RemainingSeconds => Mathf.Max(0f, _totalRealDuration - elapsedTimeInSeconds);
    public float NormalizedTime => _totalRealDuration <= 0f ? 1f : elapsedTimeInSeconds / _totalRealDuration;
    public float CurrentHour => timeConfig.GameHourAtRealSeconds(elapsedTimeInSeconds);
    public bool IsDayRunning => isDayRunning;

    public event Action OnDayInitialized;       // Setup, UIs
    public event Action OnMorningStarted;       // Gameplay logic events
    public event Action OnAfternoonStarted;
    public event Action OnHalfDayPassed;
    public event Action OnDayEnded;
    public event Action OnTimeChanged;
    

    private void Update()
    {
        if (!isDayRunning) return;
        
        bool wasMorning = elapsedTimeInSeconds < _realSecondsToNoon;
        
        elapsedTimeInSeconds += Time.deltaTime;
        OnTimeChanged?.Invoke();

        if (wasMorning && elapsedTimeInSeconds >= _realSecondsToNoon)
        {
            // TODO: half day events?
            EndHalfDay();
        }

        if (elapsedTimeInSeconds >= DayDurationInSeconds)
        {
            EndDay();
        }
    }

    public void StartDay(int day, bool fromMorning = true)
    {
        if (day > totalDays) return;
        noonHour = timeConfig.dayFormat == DayFormat.TwentyFourHour ? 12 : 6;
        _totalRealDuration = timeConfig.TotalRealDuration();
        _realSecondsToNoon = timeConfig.RealSecondsAtGameHour(noonHour);
        
        currentDay = day;
        isDayRunning = true;
        elapsedTimeInSeconds = fromMorning ? 0 : _realSecondsToNoon;

        OnDayInitialized?.Invoke();

        if (fromMorning)
        {
            OnMorningStarted?.Invoke();
        }
        else
        {
            OnAfternoonStarted?.Invoke();
        }

        Debug.Log(fromMorning ? $"Day {currentDay} started." : $"Day {currentDay} continued from noon.");
    }
    
    /// <summary>
    /// Force the current half day to be passed (eg: energy reaches 0)
    /// </summary>
    public void ForceEndHalfDay()
    {
        bool wasMorning = RemainingSeconds > _realSecondsToNoon;
        
        if (wasMorning)
        {
            EndHalfDay();
        }
        else
        {
            EndDay();
        }
    }
    
    private void EndHalfDay()
    {
        isDayRunning = false;
        OnHalfDayPassed?.Invoke();
        Debug.Log($"Day {currentDay} passed noon.");
    }

    private void EndDay()
    {
        isDayRunning = false;
        OnDayEnded?.Invoke();
        
        Debug.Log($"Day {currentDay} ended.");
    }

    /// <summary>
    /// Stops time
    /// </summary>
    public void StopDay()
    {
        isDayRunning = false;
        
        Debug.Log($"Day {currentDay} stopped.");
    }
}
