using System;
using System.Collections;
using UnityEngine;

public class CoffeeMugInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")] 
    [SerializeField] private float energyGainedOnInteract;
    [Range(0f, 1f)]
    [SerializeField] private float energyPercentageGainedOnInteract;
    [Tooltip("Blocking further interaction during interaction")]
    [SerializeField] private float interactionDurationInSeconds;
    
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PlayerResource playerResource;
    
    [Header("Options")]
    [SerializeField] private bool modifyEnergyByPercentage;
    
    private bool isInteracting = false;

    private void OnEnable()
    {
        dayManager.OnDayStarted += ResetCoffeeMug;
        dayManager.OnNightStarted += ResetCoffeeMug;
    }

    private void OnDisable()
    {
        dayManager.OnDayStarted -= ResetCoffeeMug;
        dayManager.OnNightStarted -= ResetCoffeeMug;
    }
    

    public void Interact()
    {
        if (isInteracting) return;
        
        Debug.Log("Interacted with Coffee Mug");
        if (modifyEnergyByPercentage)
        {
            playerResource.AddEnergy(playerResource.MaxEnergy * energyPercentageGainedOnInteract);
        }
        else
        {
            playerResource.AddEnergy(energyGainedOnInteract);
        }
        StartCoroutine(InteractCoroutine());
    }

    private IEnumerator InteractCoroutine()
    {
        isInteracting = true;
        yield return new WaitForSeconds(interactionDurationInSeconds);
        isInteracting = false;
    }

    private void ResetCoffeeMug()
    {
        
    }
}
