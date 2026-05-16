using System;
using UnityEngine;
using UnityEngine.UI;

public class PrisonerSlotUI : MonoBehaviour
{
    [SerializeField] private Image profilePic;
    [SerializeField] private Button button;

    private Action onClick;
    private string slotLabel;

    private void Awake()
    {
        if (button == null)
        {
            Debug.LogError($"[PrisonerSlotUI] {gameObject.name}: Button is not assigned in the Inspector!");
            return;
        }

        button.onClick.AddListener(HandleClick);
        Debug.Log($"[PrisonerSlotUI] {gameObject.name}: Button listener registered.");
    }

    public void Setup(Prisoner prisoner, Action<Prisoner> clickCallback)
    {
        slotLabel = $"{gameObject.name} (Prisoner {prisoner.PrisonerID})";
        onClick = () => clickCallback(prisoner);

        if (profilePic != null)
        {
            Sprite pic = prisoner.PrisonerData.PrisonerHeadProfilePic;
            profilePic.sprite = pic;
            profilePic.enabled = true;
            profilePic.raycastTarget = false; // never let the image eat button clicks
            if (pic == null)
                Debug.LogWarning($"[PrisonerSlotUI] {slotLabel}: No profile pic assigned on PrisonerData.");
        }
        else
        {
            Debug.LogWarning($"[PrisonerSlotUI] {gameObject.name}: profilePic Image is not assigned.");
        }

        // Debug.Log($"[PrisonerSlotUI] {slotLabel}: Setup complete, onClick assigned.");
    }

    public void Clear()
    {
        slotLabel = gameObject.name;
        onClick = null;
        if (profilePic != null)
        {
            profilePic.sprite = null;
            profilePic.enabled = true;
        }
    }

    private void HandleClick()
    {
        // Debug.Log($"[PrisonerSlotUI] {slotLabel}: Button clicked.");
        if (onClick == null)
            Debug.LogWarning($"[PrisonerSlotUI] {slotLabel}: onClick is null — was Setup() called?");
        onClick?.Invoke();
    }
}
