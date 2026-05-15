using System.Collections;
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
    
    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private RectTransform rootTransform;
    [SerializeField] private CanvasGroup staticOverlayCanvasGroup;
    [SerializeField] private GameObject staticOverlayObject;
    [SerializeField] private float openDuration;
    [SerializeField] private float feedSwitchDuration;
    
    private bool isTransitioning;
    private Coroutine transitionRoutine;

    [Header("Grid View Panel")]
    [SerializeField] private GameObject surveillancePanel;
    [SerializeField] private SurveillanceFeedGridView[] feedGridViews;
    [SerializeField] private ToggleGroup feedToggleGroup;
    
    [Header("Expanded View Panel")]
    [SerializeField] private GameObject expandedPanel;
    [SerializeField] private RawImage expandedImage;
    //[SerializeField] private TMP_Text expandedText;
    
    [Header("Visuals")]
    [SerializeField] private Material fisheyeMaterial;

    [Header("Evidence")]
    [SerializeField] private PrisonerEvidenceManager evidenceManager;
    [SerializeField] private SurveillanceEvidenceOverlay evidenceOverlay;

    [Header("Prisoner Interaction")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private SurveillancePrisonerInteractionPanel prisonerInteractionPanel;
    [SerializeField] private LayerMask prisonerInteractionLayerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private LayerMask prisonerOcclusionLayerMask;
    [SerializeField] private float prisonerInteractionMaxDistance = 1000f;
    
    private SurveillanceCamController activeCameraController;
    private readonly RaycastHit[] prisonerHitBuffer = new RaycastHit[16];
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
    private PrisonerActionController currentPrisonerTarget;
    private HighlightComponent currentPrisonerHighlight;
    private PrisonerActionController observedHighRiskPrisoner;
    private HighRiskEvidenceType observedHighRiskType;
    private float highRiskObserveTimer;

    public static bool IsFeedGridViewPanelActive {get; private set;}
    public SurveillanceFeed ActiveFeed { get; private set; }
    public Camera ActiveCamera { get; private set; }
    public GameObject ActiveRoom { get; private set; }
    public bool IsOpen { get; private set; }
    
    private void Start()
    {
        EnsureEvidenceSetup();
        EnsurePrisonerInteractionSetup();
        Close();
        SetupFeedsGrid();
    }
    
    private void Update()
    {
        if (isTransitioning) return;
        
        if (!IsOpen) return;

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
            UpdateEvidenceEffects();
        }
        else
        {
            ClearCurrentPrisonerTarget();
            ClearEvidenceObservation();
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

    private void EnsureEvidenceSetup()
    {
        if (evidenceManager == null)
        {
            evidenceManager = PrisonerEvidenceManager.Instance;
        }

        if (evidenceOverlay == null)
        {
            evidenceOverlay = GetComponent<SurveillanceEvidenceOverlay>();
            if (evidenceOverlay == null)
            {
                evidenceOverlay = gameObject.AddComponent<SurveillanceEvidenceOverlay>();
            }
        }

        if (expandedImage != null && evidenceOverlay != null)
        {
            evidenceOverlay.EnsureSetup(expandedImage.rectTransform);
        }
    }

    private void SetupFeedsGrid()
    {
        if (surveillanceManager == null)
        {
            Debug.LogError("[SurveillanceUI] SurveillanceManager is not assigned.", this);
            return;
        }

        if (feedGridViews == null || feedGridViews.Length == 0)
        {
            Debug.LogWarning("[SurveillanceUI] No feed grid views are assigned.", this);
            return;
        }

        EnsureFeedToggleGroup();

        SurveillanceFeed[] feeds = surveillanceManager.Feeds;

        for (int i = 0; i < feedGridViews.Length; i++)
        {
            if (feedGridViews[i] == null)
            {
                Debug.LogWarning($"[SurveillanceUI] Feed grid view slot {i} is not assigned.", this);
                continue;
            }

            bool hasFeed = i < feeds.Length;
            feedGridViews[i].gameObject.SetActive(hasFeed);
            if (hasFeed)
            {
                feedGridViews[i].Setup(feeds[i], this, feedToggleGroup);
            }
        }

        ResetFeedToggles();
    }

    private void EnsureFeedToggleGroup()
    {
        if (feedToggleGroup != null)
            return;

        Transform searchRoot = surveillancePanel != null ? surveillancePanel.transform : transform;
        feedToggleGroup = searchRoot.GetComponentInChildren<ToggleGroup>(true);

        if (feedToggleGroup == null && surveillancePanel != null)
        {
            feedToggleGroup = surveillancePanel.AddComponent<ToggleGroup>();
        }

        if (feedToggleGroup == null)
        {
            Debug.LogWarning("[SurveillanceUI] Feed ToggleGroup is not assigned and could not be created.", this);
            return;
        }

        feedToggleGroup.allowSwitchOff = true;
    }

    private void ResetFeedToggles()
    {
        if (feedToggleGroup != null)
        {
            feedToggleGroup.allowSwitchOff = true;
            feedToggleGroup.SetAllTogglesOff(false);
        }

        if (feedGridViews == null)
            return;

        for (int i = 0; i < feedGridViews.Length; i++)
        {
            if (feedGridViews[i] != null)
            {
                feedGridViews[i].SetToggleOffWithoutNotify();
            }
        }
    }

    public void TryOpenExpandedFeed(SurveillanceFeed feed)
    {
        if (!IsOpen || isTransitioning || feed == null) return;

        // Optimization: Don't restart transition if clicking the already active feed
        if (ActiveFeed == feed && expandedPanel.activeInHierarchy) return;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }
        transitionRoutine = StartCoroutine(SwitchFeedRoutine(feed));
    }
    
    private IEnumerator SwitchFeedRoutine(SurveillanceFeed feed)
    {
        isTransitioning = true;

        HidePrisonerInteractionPanel();
        ClearCurrentPrisonerTarget();
        DisableActiveCameraControl();

        staticOverlayObject.SetActive(true);

        yield return FadeStaticOverlay(0f, 1f, feedSwitchDuration * 0.4f);

        ClearActiveFeed();
        ApplyActiveFeed(feed);

        yield return FadeStaticOverlay(1f, 0f, feedSwitchDuration * 0.6f);

        staticOverlayObject.SetActive(false);

        isTransitioning = false;
        transitionRoutine = null;
    }

    private IEnumerator FadeStaticOverlay(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float flicker = Random.Range(-0.15f, 0.15f);
            
            staticOverlayCanvasGroup.alpha = Mathf.Clamp01(Mathf.Lerp(from, to, t) + flicker);

            yield return null;
        }

        staticOverlayCanvasGroup.alpha = to;
    }

    
    public void OpenExpandedFeed(SurveillanceFeed feed)
    {
        HidePrisonerInteractionPanel();
        ClearCurrentPrisonerTarget();
        DisableActiveCameraControl();

        ApplyActiveFeed(feed);
    }
    
    private void ApplyActiveFeed(SurveillanceFeed feed)
    {
        ClearEvidenceObservation();
        ActiveFeed = feed;
        ActiveCamera = feed.camera;
        ActiveRoom = ResolveActiveRoom(feed);

        expandedPanel.SetActive(true);

        expandedImage.texture = feed.renderTexture;
        expandedImage.material = fisheyeMaterial;
        // expandedText.text = feed.displayName;

        if (ActiveCamera != null)
        {
            activeCameraController = ActiveCamera.GetComponent<SurveillanceCamController>();

            if (activeCameraController != null)
                activeCameraController.SetControlled(true);
            else
                Debug.LogWarning($"{ActiveCamera.name} has no SurveillanceCamController.");
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
        DisableActiveCameraControl();
        ClearActiveFeed();
        
        expandedPanel.SetActive(false);
        surveillancePanel.SetActive(true);
        ResetFeedToggles();
    }

    public void TryOpen()
    {
        if (IsOpen || isTransitioning) return;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(OpenCoroutine());
    }

    private IEnumerator OpenCoroutine()
    {
        isTransitioning = true;
        IsOpen = true;

        surveillancePanel.SetActive(true);
        IsFeedGridViewPanelActive = true;
        expandedPanel.SetActive(false);
        HidePrisonerInteractionPanel();
        ClearActiveFeed();
        ResetFeedToggles();

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        Vector3 startScale = Vector3.one * 0.96f;
        Vector3 endScale = Vector3.one;
        rootTransform.localScale = startScale;

        float timer = 0f;

        while (timer < openDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / openDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            rootCanvasGroup.alpha = eased;
            rootTransform.localScale = Vector3.Lerp(startScale, endScale, eased);

            yield return null;
        }

        rootCanvasGroup.alpha = 1f;
        rootTransform.localScale = endScale;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        isTransitioning = false;
        transitionRoutine = null;
    }
    public void Open()
    {
        IsOpen = true;

        surveillancePanel.SetActive(true);
        IsFeedGridViewPanelActive = true;
        expandedPanel.SetActive(false);
        HidePrisonerInteractionPanel();
        ClearActiveFeed();
        ResetFeedToggles();
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
        ResetFeedToggles();
    }

    private bool CanProcessExpandedFeedClick(PointerEventData eventData)
    {
        return IsOpen
               && expandedPanel != null
               && expandedPanel.activeInHierarchy
               && expandedImage != null
               && ActiveCamera != null
               && prisonerInteractionPanel != null
               && eventData != null
               && dayManager != null && dayManager.IsDayPhase;
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
               && Mouse.current != null
               && !SurveillancePrisonerInteractionPanel.IsAnyOpen
               && dayManager != null && dayManager.IsDayPhase;
    }

    private GameObject ResolveActiveRoom(SurveillanceFeed feed)
    {
        if (RoomManager.Instance == null || feed == null)
            return null;

        string roomName = string.IsNullOrWhiteSpace(feed.roomName)
            ? feed.displayName
            : feed.roomName;

        return RoomManager.Instance.GetRoomByName(roomName);
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
            return true;

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
        ClearEvidenceObservation();
        if (evidenceOverlay != null)
        {
            evidenceOverlay.SetNightInterference(0f);
        }

        ActiveFeed = null;
        ActiveCamera = null;
        ActiveRoom = null;
    }

    private void UpdateEvidenceEffects()
    {
        EnsureEvidenceSetup();

        if (evidenceManager == null || evidenceOverlay == null)
            return;

        evidenceOverlay.SetNightInterference(evidenceManager.NightFeedInterferenceIntensity);

        if (!evidenceManager.TryGetVisibleHighRiskEvidence(
                ActiveRoom,
                out PrisonerActionController highRiskPrisoner,
                out HighRiskEvidenceDefinition highRiskDefinition))
        {
            ClearEvidenceObservation();
            return;
        }

        if (observedHighRiskPrisoner != highRiskPrisoner
            || observedHighRiskType != highRiskDefinition.EvidenceType)
        {
            observedHighRiskPrisoner = highRiskPrisoner;
            observedHighRiskType = highRiskDefinition.EvidenceType;
            highRiskObserveTimer = 0f;
            evidenceOverlay.ClearDayCue();
        }

        highRiskObserveTimer += Time.deltaTime;
        float requiredObserveSeconds = highRiskDefinition.ObserveSeconds > 0f
            ? highRiskDefinition.ObserveSeconds
            : evidenceManager.DefaultHighRiskObserveSeconds;

        if (highRiskObserveTimer >= requiredObserveSeconds)
        {
            evidenceOverlay.ShowDayCue(highRiskDefinition);
        }
    }

    private void ClearEvidenceObservation()
    {
        observedHighRiskPrisoner = null;
        observedHighRiskType = HighRiskEvidenceType.None;
        highRiskObserveTimer = 0f;

        if (evidenceOverlay != null)
        {
            evidenceOverlay.ClearDayCue();
        }
    }
}
