using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SurveillanceUI : MonoBehaviour
{
    [SerializeField] private SurveillanceManager surveillanceManager;

    [Header("Grid View Panel")]
    [SerializeField] private GameObject surveillancePanel;
    [SerializeField] private SurveillanceFeedGridView[] feedGridViews;
    
    [Header("Expanded View Panel")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private RawImage expandedImage;
    [SerializeField] private TMP_Text expandedText;
    
    [Header("Visuals")]
    [SerializeField] private Material fisheyeMaterial;
    
    public bool IsOpen { get; private set; }
    
    private void Start()
    {
        Close();
        SetupFeedsGrid();
    }
    
    private void Update()
    {
        if (!IsOpen)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (expandedPanel.activeSelf)
            {
                CloseExpandedFeed();
            }
            else
            {
                Close();
            }
        }
    }

    private void SetupFeedsGrid()
    {
        SurveillanceFeed[] feeds = surveillanceManager.Feeds;
        for (int i = 0; i < feedGridViews.Length; i++)
        {
            bool hasFeed = i < feeds.Length;
            feedGridViews[i].gameObject.SetActive(hasFeed);
            if (hasFeed)
            {
                feedGridViews[i].Setup(feeds[i], this, fisheyeMaterial);
                Debug.Log($"Setup Feed {i}");
            }
        }
    }
    
    public void OpenExpandedFeed(SurveillanceFeed feed)
    {
        surveillancePanel.SetActive(false);
        expandedPanel.SetActive(true);

        expandedImage.texture = feed.renderTexture;
        expandedImage.material = fisheyeMaterial;
        expandedText.text = feed.displayName;
    }
    
    public void CloseExpandedFeed()
    {
        expandedPanel.SetActive(false);
        surveillancePanel.SetActive(true);
    }
    
    public void Open()
    {
        IsOpen = true;

        surveillancePanel.SetActive(true);
        expandedPanel.SetActive(false);
    }

    public void Close()
    {
        IsOpen = false;

        surveillancePanel.SetActive(false);
        expandedPanel.SetActive(false);
    }
}
