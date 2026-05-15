using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurveillanceFeedGridView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text feedTitle;
    [SerializeField] private Toggle toggle; 

    private SurveillanceFeed feed;
    private SurveillanceUI ui;

    public void Setup(SurveillanceFeed surveillanceFeed, SurveillanceUI surveillanceUI, ToggleGroup group)
    {
        feed = surveillanceFeed;
        ui = surveillanceUI;

        if (toggle == null)
        {
            Debug.LogError($"[SurveillanceFeedGridView] {gameObject.name} has no Toggle assigned.", gameObject);
            return;
        }

        toggle.onValueChanged.RemoveAllListeners();
        toggle.group = group;
        toggle.SetIsOnWithoutNotify(false);

        if (feedTitle != null)
        {
            feedTitle.text = feed.RoomLabel;
        }
        
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    public void SetToggleOffWithoutNotify()
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        // Debug.Log($"[SurveillanceFeedGridView] {gameObject.name} toggle state changed to: {isOn}", gameObject);
        if (isOn)
        {
            // Debug.Log($"[SurveillanceFeedGridView] Calling TryOpenExpandedFeed for: {feed.RoomLabel}");
            ui.TryOpenExpandedFeed(feed);
        }
    }
}
