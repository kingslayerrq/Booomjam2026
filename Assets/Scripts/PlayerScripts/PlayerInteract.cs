using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform playerCamera;
    [SerializeField] private SurveillanceUI surveillanceUI;
    

    [Header("Debug Settings")] 
    [SerializeField] private float debugInteractRange;
    
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
    }

    private void UpdateCurrentInteractTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, debugInteractRange))
        {
            ClearCurrentInteractTarget();
            return;
        }
        
        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            ClearCurrentInteractTarget();
            return;
        }
        HighlightComponent highlightComponent = hit.collider.GetComponentInParent<HighlightComponent>();
        
        SetCurrentTarget(interactable, highlightComponent);
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
}
