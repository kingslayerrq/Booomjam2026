using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform playerCamera;
    [SerializeField] private SurveillanceUI surveillanceUI;

    [Header("Interact Settings")] 
    [SerializeField] private float interactRange;

    [SerializeField] private float interactRadius;
    [SerializeField] private LayerMask wallLayerMask;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    [Header("Debug Settings")] 
    [SerializeField] private float debugInteractRange;
    [Range(8, 32)]
    [SerializeField] private int coneLinesCount;
    
    private IInteractable currentInteractable;
    private HighlightComponent currentHighlightable;

    private void Update()
    {
        if (GameManager.IsMenuOpen || GameManager.IsCursorUnlocked || (surveillanceUI != null && surveillanceUI.IsOpen))
        {
            ClearCurrentInteractTarget();
            return;
        }
        
        UpdateCurrentInteractTarget();

        if (currentInteractable != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            currentInteractable.Interact();
        }
        
        
        // Debug ray
        Vector3 forward = playerCamera.forward * debugInteractRange;
        bool isHit = Physics.Raycast(playerCamera.position, playerCamera.forward, out _, debugInteractRange);
        
        Debug.DrawRay(playerCamera.position, forward, isHit ? Color.green : Color.red);
        
        DrawDebugInteractCircle();
    }

    private void UpdateCurrentInteractTarget()
    {
        int hitCount = Physics.SphereCastNonAlloc(
            playerCamera.position,
            interactRadius,
            playerCamera.forward,
            hitBuffer,
            interactRange
        );
        Debug.Log($"hitCount: {hitCount}");
        IInteractable closestInteractable = null;
        HighlightComponent closestHighlight = null;
        float closestDist = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            // perpendicular distance from the object to the center ray
            Vector3 toObject = hit.collider.bounds.center - playerCamera.position;
            Vector3 projected = Vector3.Project(toObject, playerCamera.forward);
            float perpDist = (toObject - projected).magnitude;

            if (perpDist >= closestDist) continue;

            closestDist = perpDist;
            closestInteractable = interactable;
            closestHighlight = hit.collider.GetComponentInParent<HighlightComponent>();
        }

        if (closestInteractable == null)
        {
            ClearCurrentInteractTarget();
            return;
        }

        Transform targetTransform = closestHighlight != null
            ? closestHighlight.transform
            : ((MonoBehaviour)closestInteractable).transform;
        
        Debug.Log($"Closest candidate: {((MonoBehaviour)closestInteractable).gameObject.name}");
        bool blocked = Physics.Linecast(playerCamera.position, targetTransform.position, wallLayerMask);
        Debug.Log($"Linecast blocked: {blocked}");
        if (blocked) ClearCurrentInteractTarget();
        else SetCurrentTarget(closestInteractable, closestHighlight);
    }
    
    private void SetCurrentTarget(IInteractable interactable, HighlightComponent highlightComponent)
    {
        if (currentInteractable == interactable)
            return;

        ClearCurrentInteractTarget();

        currentInteractable = interactable;
        currentHighlightable = highlightComponent;

        if (currentHighlightable != null)
        {
            currentHighlightable.SetHighlight(true);
            // Debug.Log($"current highlight is {currentHighlightable.gameObject.name}");
        }
    }
    
    private void ClearCurrentInteractTarget()
    {
        if (currentHighlightable != null)
        {
            currentHighlightable.SetHighlight(false);
            // Debug.Log($"Disabled highlight: {currentHighlightable.gameObject.name}");
        }

        currentInteractable = null;
        currentHighlightable = null;
    }

    #region Debug Draw
    private void DrawDebugInteractCircle()
    {
        Vector3 tip = playerCamera.position;
        Vector3 center = playerCamera.position + playerCamera.forward * interactRange;
        Vector3 right = playerCamera.right;
        Vector3 up = playerCamera.up;

        int segments = 32;
        Vector3 prevPoint = center + right * interactRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * interactRadius;
            Debug.DrawLine(prevPoint, nextPoint, Color.yellow);  // base circle
            prevPoint = nextPoint;
        }

        // cone lines
        int coneLines = 8;
        for (int i = 0; i < coneLines; i++)
        {
            float angle = i * Mathf.PI * 2f / coneLines;
            Vector3 basePoint = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * interactRadius;
            Debug.DrawLine(tip, basePoint, Color.yellow);
        }
    }
    #endregion
}
