using UnityEngine;
using UnityEngine.EventSystems;

public class SurveillanceFeedClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SurveillanceUI surveillanceUI;

    public void Initialize(SurveillanceUI ui)
    {
        surveillanceUI = ui;
    }

    private void Awake()
    {
        if (surveillanceUI == null)
        {
            surveillanceUI = GetComponentInParent<SurveillanceUI>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || surveillanceUI == null)
            return;

        surveillanceUI.TryOpenPrisonerInteraction(eventData);
    }
}
