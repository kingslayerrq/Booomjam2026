using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SurveillancePrisonerInteractionPanel : MonoBehaviour
{
    private const float PanelWidth = 190f;
    private const float PanelHeight = 112f;
    private const float ClickOffset = 12f;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text prisonerLabel;
    [SerializeField] private Button arrestButton;
    [SerializeField] private Button lockUpButton;
    [SerializeField] private RectTransform clampBounds;

    private PrisonerActionController selectedPrisoner;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
    private int shownFrame = -1;
    private bool isInitialized;
    private bool skipNextAwakeHide;

    public static bool IsAnyOpen { get; private set; }
    public bool IsOpen => panelRect != null && panelRect.gameObject.activeSelf;

    private void Awake()
    {
        Initialize();
        if (skipNextAwakeHide)
        {
            skipNextAwakeHide = false;
            return;
        }

        Hide();
    }

    private void Update()
    {
        if (!IsOpen || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (Time.frameCount == shownFrame)
            return;

        if (!IsPointerOverPanel(Mouse.current.position.ReadValue()))
        {
            Hide();
        }
    }

    private void OnDisable()
    {
        if (IsAnyOpen && panelRect != null && !panelRect.gameObject.activeInHierarchy)
        {
            IsAnyOpen = false;
        }
    }

    public void SetClampBounds(RectTransform bounds)
    {
        clampBounds = bounds;
    }

    public void Show(PrisonerActionController prisoner, Vector2 screenPosition)
    {
        if (prisoner == null || prisoner.Prisoner == null)
            return;

        Initialize();

        selectedPrisoner = prisoner;
        RefreshLabel();
        PositionNear(screenPosition);

        skipNextAwakeHide = true;
        panelRect.gameObject.SetActive(true);
        IsAnyOpen = true;
        shownFrame = Time.frameCount;
    }

    public void Hide()
    {
        Initialize();
        selectedPrisoner = null;

        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(false);
        }

        IsAnyOpen = false;
    }

    private void RefreshLabel()
    {
        Prisoner prisoner = selectedPrisoner.Prisoner;
        string displayName = string.IsNullOrWhiteSpace(prisoner.PrisonerName)
            ? $"Prisoner {prisoner.PrisonerID}"
            : prisoner.PrisonerName;
        string status = prisoner.IsLockedUp ? "Locked" : "Available";

        prisonerLabel.text = $"{displayName}\n{status}";
        lockUpButton.interactable = !prisoner.IsLockedUp;
    }

    private void PositionNear(Vector2 screenPosition)
    {
        if (panelRect.parent is not RectTransform parentRect)
            return;

        Camera eventCamera = GetEventCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 target = localPoint + new Vector2(ClickOffset, -ClickOffset);
        panelRect.anchoredPosition = ClampToBounds(parentRect, target);
    }

    private Vector2 ClampToBounds(RectTransform parentRect, Vector2 target)
    {
        if (clampBounds == null)
            return target;

        Vector3[] worldCorners = new Vector3[4];
        clampBounds.GetWorldCorners(worldCorners);

        Vector2 min = parentRect.InverseTransformPoint(worldCorners[0]);
        Vector2 max = parentRect.InverseTransformPoint(worldCorners[2]);
        Vector2 size = panelRect.rect.size;

        float x = Mathf.Clamp(target.x, min.x, max.x - size.x);
        float y = Mathf.Clamp(target.y, min.y + size.y, max.y);
        return new Vector2(x, y);
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private bool IsPointerOverPanel(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                panelRect,
                screenPosition,
                GetEventCamera()
            );
        }

        uiRaycastResults.Clear();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            Transform hitTransform = uiRaycastResults[i].gameObject.transform;
            if (hitTransform == panelRect || hitTransform.IsChildOf(panelRect))
                return true;
        }

        return false;
    }

    private void WireButtons()
    {
        if (arrestButton != null)
        {
            arrestButton.onClick.RemoveListener(ArrestSelectedPrisoner);
            arrestButton.onClick.AddListener(ArrestSelectedPrisoner);
        }

        if (lockUpButton != null)
        {
            lockUpButton.onClick.RemoveListener(LockUpSelectedPrisoner);
            lockUpButton.onClick.AddListener(LockUpSelectedPrisoner);
        }
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (panelRect == null)
        {
            panelRect = (RectTransform)transform;
        }

        WireButtons();
        isInitialized = true;
    }

    private void ArrestSelectedPrisoner()
    {
        if (selectedPrisoner != null && selectedPrisoner.Prisoner != null)
        {
            Debug.Log($"[Surveillance] Arrest placeholder selected for prisoner {selectedPrisoner.Prisoner.PrisonerID}.");
        }

        Hide();
    }

    private void LockUpSelectedPrisoner()
    {
        if (selectedPrisoner != null && selectedPrisoner.Prisoner != null)
        {
            selectedPrisoner.LockUpPrisoner();
            PrisonerEvidenceManager.Instance.HandlePrisonerLockedUp(selectedPrisoner);
            Debug.Log($"[Surveillance] Lock Them Up selected for prisoner {selectedPrisoner.Prisoner.PrisonerID}.");
        }

        Hide();
    }
}
