using System;
using UnityEngine;

public class SurveillanceCamLightController : MonoBehaviour
{
    [SerializeField] private Light camLight;
    [SerializeField] private PlayerResource playerResource;
    [SerializeField] private float batteryDrainsPerSecond;

    public event Action<bool> OnLightStateChanged;

    public bool IsOn { get; private set; }

    private void Awake()
    {
        SetLight(false);
    }

    private void Update()
    {
        if (!IsOn || playerResource == null) return;
        
        // drain
        playerResource.ReduceBatteryLevel(batteryDrainsPerSecond * Time.deltaTime);

        if (playerResource.CurrentBatteryLevel <= 0)
        {
            SetLight(false);
        }
    }

    public void ToggleCamLight()
    {
        if (IsOn)
        {
            SetLight(false);
        }
        else
        {
            if (playerResource == null || playerResource.CurrentBatteryLevel <= 0) return;
            SetLight(true);
        }
    }

    public void TurnOffCamLight()
    {
        SetLight(false);
    }

    private void SetLight(bool isOn)
    {
        bool changed = IsOn != isOn;
        IsOn = isOn;

        if (camLight != null)
        {
            camLight.enabled = isOn;
        }

        if (changed)
        {
            OnLightStateChanged?.Invoke(IsOn);
        }
    }
}
