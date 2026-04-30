using System;
using TMPro;
using UnityEngine;

public class DayUI : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;

    [Header("UI")] 
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted += UpdateDayUI;
            dayManager.OnTimeChanged += UpdateTimeUI;
        }
        UpdateDayUI();
        UpdateTimeUI();
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted -= UpdateDayUI;
            dayManager.OnTimeChanged -= UpdateTimeUI;
        }
    }

    private void UpdateDayUI()
    {
        if (dayManager == null)
            return;

        if (dayText != null)
            dayText.text = $"Day {dayManager.CurrentDay}/{dayManager.TotalDays}";
        
    }

    private void UpdateTimeUI()
    {
        if (dayManager == null)
            return;

        if (timeText != null)
        {
            float totalHoursInDay = dayManager.EndHour - dayManager.StartHour;
            float currentTotalHours = dayManager.StartHour + (dayManager.NormalizedTime * totalHoursInDay);
            
            int hours = Mathf.FloorToInt(currentTotalHours);
            int minutes = Mathf.FloorToInt((currentTotalHours - hours) * 60f);
            
            timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
    }
}
