using System;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private int currentDay;
    [SerializeField] private int totalDays;
    [SerializeField] private float dayDurationInSeconds;
    [SerializeField] private int startHour;
    [SerializeField] private int endHour;

    private float elapsedTimeInSeconds;
    private bool isDayRunning;
    
    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int StartHour => startHour;
    public int EndHour => endHour;
    public float DayDurationInSeconds => dayDurationInSeconds;
    public float RemainingSeconds => Mathf.Max(0f, dayDurationInSeconds - elapsedTimeInSeconds);
    public float NormalizedTime => dayDurationInSeconds <= 0f ? 1f : elapsedTimeInSeconds / dayDurationInSeconds;

    public bool IsDayRunning => isDayRunning;

    public event Action OnDayStarted;
    public event Action OnDayEnded;
    public event Action OnTimeChanged;

    private void Update()
    {
        if (!isDayRunning) return;
        
        elapsedTimeInSeconds += Time.deltaTime;
        OnTimeChanged?.Invoke();

        if (elapsedTimeInSeconds >= dayDurationInSeconds)
        {
            EndDay();
        }
    }

    public void StartDay(int day)
    {
        if (day > totalDays) return;
        currentDay = day;
        OnDayStarted?.Invoke();
        
        elapsedTimeInSeconds = 0;
        isDayRunning = true;
        
        Debug.Log($"Day {currentDay} started.");
    }

    public void EndDay()
    {
        if (!isDayRunning) return;
        isDayRunning = false;
        OnDayEnded?.Invoke();
        
        Debug.Log($"Day {currentDay} ended.");
    }
}
