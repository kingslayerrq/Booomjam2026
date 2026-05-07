using UnityEngine;

public class PCView : MonoBehaviour, IInteractable
{
    [SerializeField] private SurveillanceUI surveillanceUI;
    
    public void Interact()
    {
        surveillanceUI.Open();
    }
}
