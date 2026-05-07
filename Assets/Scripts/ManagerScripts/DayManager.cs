using System;
using UnityEngine;
using UnityEngine.Serialization;

public class DayManager : MonoBehaviour
{
    [Header("Time Settings")] 
    [SerializeField] private int currentDay;
    [SerializeField] private int totalDays;
    [SerializeField] private float halfDayDurationInSeconds;
    [SerializeField] private int startHour = 0;
    [SerializeField] private int endHour = 24;
    
    private float elapsedTimeInSeconds;
    private bool isDayRunning;
    
    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int StartHour => startHour;
    public int EndHour => endHour;
    public bool IsMorning => isDayRunning && RemainingSeconds > halfDayDurationInSeconds;
    public float DayDurationInSeconds => halfDayDurationInSeconds * 2;
    public float RemainingSeconds => Mathf.Max(0f, DayDurationInSeconds - elapsedTimeInSeconds);
    public float NormalizedTime => (DayDurationInSeconds) <= 0f ? 1f : elapsedTimeInSeconds / (DayDurationInSeconds);

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
        
        bool wasMorning = elapsedTimeInSeconds < halfDayDurationInSeconds;
        
        elapsedTimeInSeconds += Time.deltaTime;
        OnTimeChanged?.Invoke();

        if (wasMorning && elapsedTimeInSeconds >= halfDayDurationInSeconds)
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
        
        currentDay = day;
        isDayRunning = true;
        elapsedTimeInSeconds = fromMorning ? 0 : halfDayDurationInSeconds;

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
        bool wasMorning = RemainingSeconds > halfDayDurationInSeconds;
        
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
}
