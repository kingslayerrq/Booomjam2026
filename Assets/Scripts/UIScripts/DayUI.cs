using System;
using TMPro;
using UnityEngine;

public class DayUI : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;

    [Header("UI")] 
    [SerializeField] private TMP_Text dayText; // For the 03/16 MON part
    [SerializeField] private TMP_Text timeText; // For the 12:00:00 PM part

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

        // Matches the format: 03/16 MON
        // Using CurrentDay as a placeholder for date logic
        string dateFormatted = $"03/{15 + dayManager.CurrentDay:00}"; 
        string dayOfWeek = "MON"; // You could use an array of strings if you have a weekly cycle
        
        dayText.text = $"{dateFormatted} {dayOfWeek}";
    }

    private void UpdateTimeUI()
    {
        if (dayManager == null || timeText == null) return;

        float currentHour = dayManager.CurrentHour;
        
        // Calculate Hours, Minutes, and Seconds
        int hours24 = Mathf.FloorToInt(currentHour);
        int minutes = Mathf.FloorToInt((currentHour - hours24) * 60f);
        int seconds = Mathf.FloorToInt(((currentHour - hours24) * 3600f) % 60f);

        // Convert to 12-hour format
        string amPm = hours24 >= 12 ? "PM" : "AM";
        int hours12 = hours24 % 12;
        if (hours12 == 0) hours12 = 12; // Handle Midnight/Noon

        // Format: 12:30:45 PM
        timeText.text = string.Format("{0:00}:{1:00}:{2:00} {3}", hours12, minutes, seconds, amPm);
    }
}