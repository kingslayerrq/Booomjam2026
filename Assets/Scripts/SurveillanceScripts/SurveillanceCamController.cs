using UnityEngine;
using UnityEngine.InputSystem;

public class SurveillanceCamController : MonoBehaviour
{
    [Header("Rotation Speed")]
    [SerializeField] private float yawSpeed = 25f;
    [SerializeField] private float pitchSpeed = 18f;

    [Header("Rotation Limits")]
    [SerializeField] private float yawLimit = 45f;
    [SerializeField] private float lookUpLimit = 25f;
    [SerializeField] private float lookDownLimit = 30f;

    [Header("Options")]
    [SerializeField] private bool resetWhenControlStarts = false;
    

    private Quaternion baseLocalRotation;

    private float yawOffset;
    private float pitchOffset;

    private bool isControlled;

    private void Awake()
    {
        baseLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!isControlled)
            return;
        if (GameManager.IsMenuOpen || GameManager.BlockCamControl || SurveillancePrisonerInteractionPanel.IsAnyOpen)
            return;

        HandleCameraInput();
        ApplyRotation();
    }

    public void SetControlled(bool controlled)
    {
        isControlled = controlled;

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

    private void ApplyRotation()
    {
        Quaternion offsetRotation = Quaternion.Euler(pitchOffset, yawOffset, 0f);
        transform.localRotation = baseLocalRotation * offsetRotation;
    }
}
