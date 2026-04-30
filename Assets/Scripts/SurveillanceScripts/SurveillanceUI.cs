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
    
    private SurveillanceCamController activeCameraController;
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
    
    private void DisableActiveCameraControl()
    {
        if (activeCameraController != null)
        {
            activeCameraController.SetControlled(false);
            activeCameraController = null;
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
        
        // Get cam controll
        if (feed.camera != null)
        {
            activeCameraController = feed.camera.GetComponent<SurveillanceCamController>();

            if (activeCameraController != null)
            {
                activeCameraController.SetControlled(true);
            }
            else
            {
                Debug.LogWarning($"{feed.camera.name} has no SurveillanceCamController.");
            }
        }
    }
    
    public void CloseExpandedFeed()
    {
        // Remove cam controll
        DisableActiveCameraControl();
        
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
        // Remove cam controll
        DisableActiveCameraControl();
        
        IsOpen = false;

        surveillancePanel.SetActive(false);
        expandedPanel.SetActive(false);
    }
}
