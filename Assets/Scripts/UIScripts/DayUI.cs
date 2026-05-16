using System;
using TMPro;
using UnityEngine;

public class DayUI : MonoBehaviour
{
    private static readonly string[] DaysOfWeek = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

    [SerializeField] private DayManager dayManager;
    [Tooltip("Day of week that Day 1 falls on. 0 = Sunday, 1 = Monday, etc.")]
    [SerializeField] [Range(0, 6)] private int startDayOfWeekIndex = 1;

    [Header("UI")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayInitialized += UpdateDayUI;
            dayManager.OnTimeChanged += UpdateTimeUI;
        }
        UpdateDayUI();
        UpdateTimeUI();
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayInitialized -= UpdateDayUI;
            dayManager.OnTimeChanged -= UpdateTimeUI;
        }
    }

    private void UpdateDayUI()
    {
        if (dayManager == null || dayText == null) return;

        int currentDay = dayManager.CurrentDay;
        string dateFormatted = $"03/{15 + currentDay:00}";
        string dayOfWeek = DaysOfWeek[(startDayOfWeekIndex + currentDay - 1) % DaysOfWeek.Length];

        dayText.text = $"{dateFormatted} {dayOfWeek}";
    }

    private void UpdateTimeUI()
    {
        if (dayManager == null || timeText == null) return;

        float displayHour = dayManager.DisplayHour;

        int hours24 = Mathf.FloorToInt(displayHour);
        int minutes = Mathf.FloorToInt((displayHour - hours24) * 60f);
        int seconds = Mathf.FloorToInt(((displayHour - hours24) * 3600f) % 60f);

        string amPm = hours24 >= 12 ? "PM" : "AM";
        int hours12 = hours24 % 12;
        if (hours12 == 0) hours12 = 12;

        timeText.text = $"{hours12:00}:{minutes:00}:{seconds:00} {amPm}";
    }
}