using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class CamFlashlightUI : MonoBehaviour
{
    [SerializeField] private SurveillanceUI surveillanceUI;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite lightOnSprite;
    [SerializeField] private Sprite lightOffSprite;
    [SerializeField] private bool hideWhenNoActiveCamera = true;

    private Camera observedCamera;
    private SurveillanceCamLightController observedLightController;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (surveillanceUI == null)
        {
            surveillanceUI = FindFirstObjectByType<SurveillanceUI>();
        }
    }

    private void OnEnable()
    {
        RefreshBinding();
    }

    private void OnDisable()
    {
        SetObservedLightController(null);
        observedCamera = null;
    }

    private void Update()
    {
        RefreshBinding();
    }

    private void RefreshBinding()
    {
        Camera activeCamera = surveillanceUI != null && surveillanceUI.IsOpen
            ? surveillanceUI.ActiveCamera
            : null;

        if (observedCamera == activeCamera)
            return;

        observedCamera = activeCamera;
        SetObservedLightController(ResolveLightController(activeCamera));
    }

    private SurveillanceCamLightController ResolveLightController(Camera activeCamera)
    {
        if (activeCamera == null)
            return null;

        SurveillanceCamController cameraController = activeCamera.GetComponent<SurveillanceCamController>();
        if (cameraController != null)
            return cameraController.CamLightController;

        return activeCamera.GetComponent<SurveillanceCamLightController>();
    }

    private void SetObservedLightController(SurveillanceCamLightController lightController)
    {
        if (observedLightController == lightController)
        {
            UpdateSprite(observedLightController != null && observedLightController.IsOn);
            return;
        }

        if (observedLightController != null)
        {
            observedLightController.OnLightStateChanged -= UpdateSprite;
        }

        observedLightController = lightController;

        if (observedLightController != null)
        {
            observedLightController.OnLightStateChanged += UpdateSprite;
        }

        UpdateSprite(observedLightController != null && observedLightController.IsOn);
    }

    private void UpdateSprite(bool isOn)
    {
        if (targetImage == null)
            return;

        Sprite nextSprite = isOn ? lightOnSprite : lightOffSprite;
        if (nextSprite != null)
        {
            targetImage.sprite = nextSprite;
        }

        targetImage.enabled = !hideWhenNoActiveCamera || observedLightController != null;
    }
}
