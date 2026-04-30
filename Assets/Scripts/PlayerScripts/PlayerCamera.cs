using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SurveillanceUI surveillanceUI;
    [SerializeField] private GameObject mainMenuUI;
    
    [Header("Settings")]
    [SerializeField] private float mouseSensivity;

    [SerializeField] private float yawLimit;
    [SerializeField] private float minPitch;
    [SerializeField] private float maxPitch;
    
    private float currentPitch;
    private float currentYaw;
    
    private void Update()
    {
        if (GameManager.IsMenuOpen || GameManager.IsCursorUnlocked || (surveillanceUI != null && surveillanceUI.IsOpen))
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

    
    private void OnDrawGizmos()
    {
        // Get the camera component attached to this object or a child
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) return;

        // Set the color for the debug lines
        Gizmos.color = Color.cyan;

        // This pulls the current matrix of the camera to draw the pyramid accurately
        Matrix4x4 srcMatrix = Gizmos.matrix;
        Gizmos.matrix = cam.transform.localToWorldMatrix;

        // Draw the Frustum (FOV)
        // 0.3f is how far from the camera the visualizer starts
        // cam.farClipPlane is the max distance, but you might want to use a smaller number 
        // like 10f just for a cleaner visualizer.
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, 10f, 0.3f, cam.aspect);

        // Reset the matrix so it doesn't mess up other Gizmos
        Gizmos.matrix = srcMatrix;
    }
}
