using System;
using UnityEngine;

public class RoomStaticLightsScheduler : MonoBehaviour
{
    [System.Serializable]
    public struct LightWindow
    {
        [Range(0, 24)] public float startHour;
        [Range(0, 24)] public float endHour;
    }

    [SerializeField] private DayManager dayManager;
    [SerializeField] private RoomStaticLightsController[] staticLightsController;
    
    [Header("Room Static Lights are ON in these windows")]
    [SerializeField] private LightWindow[] lightWindows;

    private bool? lastLightState;           // stores prev state so not apply state every frame

    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayInitialized += ApplyCurrentLightState;
            dayManager.OnTimeChanged += ApplyCurrentLightState;
        }
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayInitialized -= ApplyCurrentLightState;
            dayManager.OnTimeChanged -= ApplyCurrentLightState;
        }
    }

    private void ApplyCurrentLightState()
    {
        bool state = IsInsideAnyLightWindow(dayManager.CurrentHour);
        if (lastLightState.HasValue && lastLightState == state) return;
        
        lastLightState = state;

        for (int i = 0; i < staticLightsController.Length; i++)
        {
            if (staticLightsController[i] != null)
            {
                staticLightsController[i].SetStaticLights(state);
            }
        }
    }

    private bool IsInsideAnyLightWindow(float hour)
    {
        for (int i = 0; i < lightWindows.Length; i++)
        {
            if (lightWindows[i].startHour <= lightWindows[i].endHour)
            {
                if (hour >= lightWindows[i].startHour && hour < lightWindows[i].endHour)
                {
                    return true;
                }
            }
            else
            {
                // windows like 21:00 - 6:00
                if (hour >= lightWindows[i].startHour || hour < lightWindows[i].endHour)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
