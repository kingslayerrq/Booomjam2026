using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform playerCamera;

    [Header("Settings")] 
    [SerializeField] private Key interactKey;

    [Header("Debug Settings")] 
    [SerializeField] private float debugInteractRange;

    private void Update()
    {
        if (GameManager.IsMenuOpen || GameManager.IsCursorUnlocked)
            return;

        if (Keyboard.current[interactKey].wasReleasedThisFrame)
        {
            TryInteract();
        }
        
        
        // Debug ray
        Vector3 forward = playerCamera.forward * debugInteractRange;
        bool isHit = Physics.Raycast(playerCamera.position, playerCamera.forward, out _, debugInteractRange);
        
        Debug.DrawRay(playerCamera.position, forward, isHit ? Color.green : Color.red);
    }

    private void TryInteract()
    {
        Ray ray = new  Ray(playerCamera.position, playerCamera.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        
        IInteractable  interactable = hit.collider.GetComponent<IInteractable>();
        
        if (interactable == null) return;
        
        interactable.Interact();
    }
}
