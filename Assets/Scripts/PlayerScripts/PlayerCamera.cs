using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; 
using DG.Tweening;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SurveillanceUI surveillanceUI;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private Image blackoutImage; 
    
    [Header("Settings")]
    [SerializeField] private float mouseSensivity;
    [SerializeField] private float yawLimit;
    [SerializeField] private float minPitch;
    [SerializeField] private float maxPitch;
    
    [Header("Knockout Effect")]
    [SerializeField] private float fallDuration = 1.5f;
    [SerializeField] private float wakeDuration = 2.0f;
    [SerializeField] private Vector3 knockoutLocalPosition = new Vector3(0, -1f, 0); 
    [SerializeField] private Vector3 knockoutRotation = new Vector3(80f, 0, 45f);
    
    private float currentPitch;
    private float currentYaw;
    
    private Vector3 originalLocalPosition;
    private bool isKnockedOut = false;
    
    private Sequence effectSequence;

    private void Start()
    {
        originalLocalPosition = transform.localPosition;
        
        // Ensure the screen is clear when the game starts
        if (blackoutImage != null)
        {
            blackoutImage.color = new Color(0, 0, 0, 0);
        }
    }

    private void Update()
    {
        if (isKnockedOut || GameManager.IsMenuOpen || GameManager.IsCursorUnlocked || (surveillanceUI != null && surveillanceUI.IsOpen))
            return;
        
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        float mouseX = mouseDelta.x * mouseSensivity;
        float mouseY = mouseDelta.y * mouseSensivity;

        currentYaw += mouseX;
        currentPitch -= mouseY;
        
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        currentYaw = Mathf.Clamp(currentYaw, -yawLimit, yawLimit);
        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
    }

    public void TriggerKnockout(Action onComplete = null)
    {
        if (isKnockedOut) return;
        isKnockedOut = true;

        effectSequence?.Kill();
        effectSequence = DOTween.Sequence();

        effectSequence.Join(transform.DOLocalMove(knockoutLocalPosition, fallDuration).SetEase(Ease.OutBounce));
        effectSequence.Join(transform.DOLocalRotate(knockoutRotation, fallDuration).SetEase(Ease.OutCubic));

        if (blackoutImage != null)
            effectSequence.Join(blackoutImage.DOFade(1f, fallDuration).SetEase(Ease.InQuad));

        effectSequence.OnComplete(() => onComplete?.Invoke());
    }

    public void TriggerWakeUp()
    {
        if (!isKnockedOut) return;
        
        effectSequence?.Kill();
        effectSequence = DOTween.Sequence();

        effectSequence.Join(transform.DOLocalMove(originalLocalPosition, wakeDuration).SetEase(Ease.InOutSine));
        effectSequence.Join(transform.DOLocalRotate(Vector3.zero, wakeDuration).SetEase(Ease.InOutSine));
        
        // Fade the screen back to clear
        if (blackoutImage != null)
        {
            effectSequence.Join(blackoutImage.DOFade(0f, wakeDuration).SetEase(Ease.InOutSine));
        }
        
        effectSequence.OnComplete(() => 
        {
            currentPitch = 0f;
            currentYaw = 0f;
            isKnockedOut = false; 
        });
    }

    private void OnDrawGizmos()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) return;

        Gizmos.color = Color.cyan;
        Matrix4x4 srcMatrix = Gizmos.matrix;
        Gizmos.matrix = cam.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, 10f, 0.3f, cam.aspect);
        Gizmos.matrix = srcMatrix;
    }
}