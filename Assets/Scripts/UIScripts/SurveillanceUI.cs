using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SurveillanceUI : MonoBehaviour
{
    private static readonly int DistortionProperty = Shader.PropertyToID("_Distortion");
    private static readonly int ZoomProperty = Shader.PropertyToID("_Zoom");

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

    [Header("Prisoner Interaction")]
    [SerializeField] private SurveillancePrisonerInteractionPanel prisonerInteractionPanel;
    [SerializeField] private LayerMask prisonerInteractionLayerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private LayerMask prisonerOcclusionLayerMask;
    [SerializeField] private float prisonerInteractionMaxDistance = 1000f;
    
    private SurveillanceCamController activeCameraController;
    private readonly RaycastHit[] prisonerHitBuffer = new RaycastHit[16];
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
    private PrisonerActionController currentPrisonerTarget;
    private HighlightComponent currentPrisonerHighlight;

    public static bool IsFeedGridViewPanelActive {get; private set;}
    public SurveillanceFeed ActiveFeed { get; private set; }
    public Camera ActiveCamera { get; private set; }
    public GameObject ActiveRoom { get; private set; }
    public bool IsOpen { get; private set; }
    
    private void Start()
    {
        EnsurePrisonerInteractionSetup();
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

            return;
        }

        if (expandedPanel != null && expandedPanel.activeInHierarchy)
        {
            UpdateCurrentPrisonerTarget();
        }
        else
        {
            ClearCurrentPrisonerTarget();
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

    private void EnsurePrisonerInteractionSetup()
    {
        if (expandedImage != null)
        {
            SurveillanceFeedClickHandler clickHandler = expandedImage.GetComponent<SurveillanceFeedClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = expandedImage.gameObject.AddComponent<SurveillanceFeedClickHandler>();
            }

            clickHandler.Initialize(this);
        }

        if (prisonerInteractionPanel == null && expandedPanel != null && expandedImage != null)
        {
            Debug.LogError("[SurveillanceUI] Prisoner Interaction Panel is not assigned. Assign an authored panel instance in the inspector.", this);
        }
        else if (prisonerInteractionPanel != null && expandedImage != null)
        {
            prisonerInteractionPanel.SetClampBounds(expandedImage.rectTransform);
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
            }
        }
    }
    
    public void OpenExpandedFeed(SurveillanceFeed feed)
    {
        HidePrisonerInteractionPanel();
        ClearCurrentPrisonerTarget();
        DisableActiveCameraControl();

        ActiveFeed = feed;
        ActiveCamera = feed.camera;
        ActiveRoom = RoomManager.Instance != null
            ? RoomManager.Instance.GetRoomByName(feed.displayName)
            : null;

        // surveillancePanel.SetActive(false);
        expandedPanel.SetActive(true);

        expandedImage.texture = feed.renderTexture;
        expandedImage.material = fisheyeMaterial;
        expandedText.text = feed.displayName;
        
        // Get cam controll
        if (ActiveCamera != null)
        {
            activeCameraController = ActiveCamera.GetComponent<SurveillanceCamController>();

            if (activeCameraController != null)
            {
                activeCameraController.SetControlled(true);
            }
            else
            {
                Debug.LogWarning($"{ActiveCamera.name} has no SurveillanceCamController.");
            }
        }
    }

    public void TryOpenPrisonerInteraction(PointerEventData eventData)
    {
        if (!CanProcessExpandedFeedClick(eventData) || currentPrisonerTarget == null)
            return;

        prisonerInteractionPanel.Show(currentPrisonerTarget, eventData.position);
        ClearCurrentPrisonerTarget();
    }
    
    public void CloseExpandedFeed()
    {
        HidePrisonerInteractionPanel();
        ClearCurrentPrisonerTarget();

        // Remove cam controll
        DisableActiveCameraControl();
        ClearActiveFeed();
        
        expandedPanel.SetActive(false);
        surveillancePanel.SetActive(true);
    }
    
    public void Open()
    {
        IsOpen = true;

        surveillancePanel.SetActive(true);
        IsFeedGridViewPanelActive = true;
        expandedPanel.SetActive(false);
        HidePrisonerInteractionPanel();
        ClearActiveFeed();
    }

    public void Close()
    {
        HidePrisonerInteractionPanel();
        ClearCurrentPrisonerTarget();

        // Remove cam controll
        DisableActiveCameraControl();
        ClearActiveFeed();
        
        IsOpen = false;

        surveillancePanel.SetActive(false);
        IsFeedGridViewPanelActive = false;
        expandedPanel.SetActive(false);
    }

    private bool CanProcessExpandedFeedClick(PointerEventData eventData)
    {
        return IsOpen
               && expandedPanel != null
               && expandedPanel.activeInHierarchy
               && expandedImage != null
               && ActiveCamera != null
               && prisonerInteractionPanel != null
               && eventData != null;
    }

    private void UpdateCurrentPrisonerTarget()
    {
        if (!CanUpdatePrisonerTarget())
        {
            ClearCurrentPrisonerTarget();
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Camera eventCamera = GetExpandedImageEventCamera();

        if (!IsExpandedImageTopmostTarget(screenPosition) ||
            !TryGetSourceFeedUv(screenPosition, eventCamera, out Vector2 sourceUv))
        {
            ClearCurrentPrisonerTarget();
            return;
        }

        Ray ray = ActiveCamera.ViewportPointToRay(new Vector3(sourceUv.x, sourceUv.y, 0f));
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            prisonerHitBuffer,
            prisonerInteractionMaxDistance,
            prisonerInteractionLayerMask,
            QueryTriggerInteraction.Ignore
        );

        PrisonerActionController closestPrisoner = null;
        HighlightComponent closestHighlight = null;
        float closestPerpDist = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = prisonerHitBuffer[i];
            PrisonerActionController prisoner = hit.collider.GetComponentInParent<PrisonerActionController>();
            if (prisoner == null || !IsPrisonerInActiveRoom(prisoner) || IsPrisonerBlocked(hit))
                continue;

            if (hit.distance >= closestPerpDist)
                continue;

            closestPerpDist = hit.distance;
            closestPrisoner = prisoner;
            closestHighlight = prisoner.GetComponentInChildren<HighlightComponent>();
        }

        if (closestPrisoner == null)
        {
            ClearCurrentPrisonerTarget();
            return;
        }

        SetCurrentPrisonerTarget(closestPrisoner, closestHighlight);
    }

    private bool CanUpdatePrisonerTarget()
    {
        return IsOpen
               && expandedPanel != null
               && expandedPanel.activeInHierarchy
               && expandedImage != null
               && ActiveCamera != null
               && ActiveRoom != null
               && Mouse.current != null
               && !SurveillancePrisonerInteractionPanel.IsAnyOpen;
    }

    private bool TryGetSourceFeedUv(Vector2 screenPosition, Camera eventCamera, out Vector2 sourceUv)
    {
        sourceUv = default;
        RectTransform imageRect = expandedImage.rectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = imageRect.rect;
        Vector2 displayUv = new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
            Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
        );

        if (displayUv.x < 0f || displayUv.x > 1f || displayUv.y < 0f || displayUv.y > 1f)
            return false;

        sourceUv = MapFisheyeDisplayUvToSourceUv(displayUv);
        return sourceUv.x >= 0f && sourceUv.x <= 1f && sourceUv.y >= 0f && sourceUv.y <= 1f;
    }

    private bool IsExpandedImageTopmostTarget(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                expandedImage.rectTransform,
                screenPosition,
                GetExpandedImageEventCamera()
            );
        }

        uiRaycastResults.Clear();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        if (uiRaycastResults.Count == 0)
            return false;

        return uiRaycastResults[0].gameObject == expandedImage.gameObject;
    }

    private Camera GetExpandedImageEventCamera()
    {
        Canvas canvas = expandedImage != null ? expandedImage.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private Vector2 MapFisheyeDisplayUvToSourceUv(Vector2 displayUv)
    {
        Material material = expandedImage.material != null ? expandedImage.material : fisheyeMaterial;
        float distortion = material != null && material.HasProperty(DistortionProperty)
            ? material.GetFloat(DistortionProperty)
            : 0.25f;
        float zoom = material != null && material.HasProperty(ZoomProperty)
            ? material.GetFloat(ZoomProperty)
            : 1f;

        if (Mathf.Approximately(zoom, 0f))
        {
            zoom = 1f;
        }

        Vector2 centered = displayUv * 2f - Vector2.one;
        float radiusSquared = Vector2.Dot(centered, centered);
        float distortionFactor = 1f + distortion * radiusSquared;
        Vector2 distorted = centered * distortionFactor / zoom;

        return distorted * 0.5f + new Vector2(0.5f, 0.5f);
    }

    private bool IsPrisonerInActiveRoom(PrisonerActionController prisoner)
    {
        if (ActiveRoom == null)
            return false;

        return prisoner.transform == ActiveRoom.transform || prisoner.transform.IsChildOf(ActiveRoom.transform);
    }

    private bool IsPrisonerBlocked(RaycastHit hit)
    {
        if (prisonerOcclusionLayerMask.value == 0)
            return false;

        return Physics.Linecast(
            ActiveCamera.transform.position,
            hit.collider.bounds.center,
            prisonerOcclusionLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void SetCurrentPrisonerTarget(PrisonerActionController prisoner, HighlightComponent highlightComponent)
    {
        if (currentPrisonerTarget == prisoner)
            return;

        ClearCurrentPrisonerTarget();

        currentPrisonerTarget = prisoner;
        currentPrisonerHighlight = highlightComponent;

        if (currentPrisonerHighlight != null)
        {
            currentPrisonerHighlight.SetHighlight(true);
        }
    }

    private void ClearCurrentPrisonerTarget()
    {
        if (currentPrisonerHighlight != null)
        {
            currentPrisonerHighlight.SetHighlight(false);
        }

        currentPrisonerTarget = null;
        currentPrisonerHighlight = null;
    }

    private void HidePrisonerInteractionPanel()
    {
        if (prisonerInteractionPanel != null)
        {
            prisonerInteractionPanel.Hide();
        }
    }

    private void ClearActiveFeed()
    {
        ActiveFeed = null;
        ActiveCamera = null;
        ActiveRoom = null;
    }
}
