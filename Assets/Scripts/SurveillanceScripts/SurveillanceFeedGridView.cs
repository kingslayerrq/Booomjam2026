using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurveillanceFeedGridView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage feedImage;
    [SerializeField] private TMP_Text feedTitle;
    [SerializeField] private Button button;

    private SurveillanceFeed feed;
    private SurveillanceUI ui;

    public void Setup(SurveillanceFeed surveillanceFeed, SurveillanceUI surveillanceUI, Material feedMaterial)
    {
        feed = surveillanceFeed;
        ui = surveillanceUI;

        if (feedImage != null)
        {
            feedImage.texture = feed.renderTexture;
            feedImage.material = feedMaterial;
        }
        
        feedTitle.text = feed.displayName;
        
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        ui.OpenExpandedFeed(feed);
    }

}
