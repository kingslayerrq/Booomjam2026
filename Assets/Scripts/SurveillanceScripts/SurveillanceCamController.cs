using UnityEngine;
using UnityEngine.InputSystem;

public class SurveillanceCamController : MonoBehaviour
{
    [Header("Battery consumption")]
    [SerializeField] private PlayerResource playerResource;
    [Tooltip("Battery drained per real-time second while this camera is controlled in expanded surveillance view.")]
    [SerializeField] private float camBatteryConsumption = 0.01f;
    [Tooltip("Room this camera belongs to. Used to detect AbnormalBatteryDrain prisoners.")]
    [SerializeField] private GameObject room;

    [Header("Camera Rotation Speed")]
    [SerializeField] private float yawSpeed = 25f;
    [SerializeField] private float pitchSpeed = 18f;

    [Header("Rotation Limits")]
    [SerializeField] private float yawLimit = 45f;
    [SerializeField] private float lookUpLimit = 25f;
    [SerializeField] private float lookDownLimit = 30f;

    [Header("Camera Light")]
    [SerializeField] private SurveillanceCamLightController surveillanceCamLightController;

    [Header("Options")]
    [SerializeField] private bool resetWhenControlStarts = false;


    private Quaternion baseLocalRotation;

    private float yawOffset;
    private float pitchOffset;

    private bool isControlled;
    private bool cameraMoveLoopActive;

    public SurveillanceCamLightController CamLightController
    {
        get
        {
            if (surveillanceCamLightController == null)
            {
                surveillanceCamLightController = GetComponent<SurveillanceCamLightController>();
            }

            return surveillanceCamLightController;
        }
    }

    private void Awake()
    {
        baseLocalRotation = transform.localRotation;

        _ = CamLightController;

        ResolvePlayerResource();
    }

    private void Update()
    {
        if (!isControlled)
            return;
        if (GameManager.IsMenuOpen || GameManager.BlockCamControl || SurveillancePrisonerInteractionPanel.IsAnyOpen)
        {
            SetCameraMoveLoop(false);
            return;
        }

        if (!ConsumeControlBattery())
        {
            SetCameraMoveLoop(false);
            return;
        }

        // Toggle lights
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CamLightController?.ToggleCamLight();
        }

        bool movingCamera = IsMovementInputPressed();
        HandleCameraInput();
        ApplyRotation();
        SetCameraMoveLoop(movingCamera);

    }

    public void SetControlled(bool controlled)
    {
        if (controlled && !HasBatteryForControl())
        {
            controlled = false;
        }

        isControlled = controlled;

        if (!isControlled)
        {
            CamLightController?.TurnOffCamLight();
            SetCameraMoveLoop(false);
            return;
        }

        if (isControlled && resetWhenControlStarts)
        {
            ResetView();
        }
    }

    public void ResetView()
    {
        yawOffset = 0f;
        pitchOffset = 0f;
        transform.localRotation = baseLocalRotation;
    }

    private void HandleCameraInput()
    {
        float yawInput = 0f;
        float pitchInput = 0f;

        if (Keyboard.current.aKey.isPressed)
            yawInput -= 1f;

        if (Keyboard.current.dKey.isPressed)
            yawInput += 1f;

        if (Keyboard.current.wKey.isPressed)
            pitchInput -= 1f;

        if (Keyboard.current.sKey.isPressed)
            pitchInput += 1f;

        yawOffset += yawInput * yawSpeed * Time.deltaTime;
        pitchOffset += pitchInput * pitchSpeed * Time.deltaTime;

        yawOffset = Mathf.Clamp(yawOffset, -yawLimit, yawLimit);
        pitchOffset = Mathf.Clamp(pitchOffset, -lookUpLimit, lookDownLimit);
    }

    private bool IsMovementInputPressed()
    {
        return Keyboard.current != null
               && (Keyboard.current.aKey.isPressed
                   || Keyboard.current.dKey.isPressed
                   || Keyboard.current.wKey.isPressed
                   || Keyboard.current.sKey.isPressed);
    }

    private void SetCameraMoveLoop(bool active)
    {
        if (cameraMoveLoopActive == active)
            return;

        cameraMoveLoopActive = active;
        GameAudioManager.Instance.SetCameraMoveLoopActive(active, transform);
    }

    private void ApplyRotation()
    {
        Quaternion offsetRotation = Quaternion.Euler(pitchOffset, yawOffset, 0f);
        transform.localRotation = baseLocalRotation * offsetRotation;
    }

    private bool ConsumeControlBattery()
    {
        if (!ResolvePlayerResource())
            return true;

        float drain = camBatteryConsumption;
        drain += PrisonerEvidenceManager.Instance?.GetAuxBatteryDrainRate(room) ?? 0f;

        if (drain > 0f)
            playerResource.ReduceBatteryLevel(drain * Time.deltaTime);

        if (playerResource.CurrentBatteryLevel > 0f)
            return true;

        SetControlled(false);
        return false;
    }

    private bool HasBatteryForControl()
    {
        return !ResolvePlayerResource() || playerResource.CurrentBatteryLevel > 0f;
    }

    private bool ResolvePlayerResource()
    {
        if (playerResource != null)
            return true;

        playerResource = FindFirstObjectByType<PlayerResource>();
        return playerResource != null;
    }
}
