using UnityEngine;

public class DocInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private PrisonerScheduleUI prisonerScheduleUI;

    public void Interact()
    {
        Debug.Log($"Open prisoner schedule ui.");
        prisonerScheduleUI.Open();
    }
}
