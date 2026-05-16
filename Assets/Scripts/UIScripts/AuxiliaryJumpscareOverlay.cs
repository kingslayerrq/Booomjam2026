using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AuxiliaryJumpscareOverlay : MonoBehaviour
{
    [SerializeField] private Image jumpscareImage;
    [SerializeField] private int sortingOrder = 5000;

    private Canvas overlayCanvas;
    private CanvasGroup overlayCanvasGroup;
    private RectTransform overlayRect;
    private RectTransform jumpscareRect;
    private Sequence jumpscareSequence;

    public bool IsPlaying => jumpscareSequence != null && jumpscareSequence.IsActive() && jumpscareSequence.IsPlaying();

    public void EnsureSetup(RectTransform overlayRoot)
    {
        if (overlayRoot == null)
            return;

        if (overlayRect == null)
        {
            GameObject overlayObject = new GameObject("AuxiliaryJumpscareOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayCanvas = overlayObject.GetComponent<Canvas>();
            overlayCanvasGroup = overlayObject.GetComponent<CanvasGroup>();
        }
        else
        {
            overlayCanvas = overlayRect.GetComponent<Canvas>();
            overlayCanvasGroup = overlayRect.GetComponent<CanvasGroup>();
        }

        if (overlayRect.parent != overlayRoot)
        {
            overlayRect.SetParent(overlayRoot, false);
        }

        MatchParentRect(overlayRect);
        overlayRect.SetAsLastSibling();

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortingOrder;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.alpha = 0f;

        if (jumpscareImage == null)
        {
            GameObject imageObject = new GameObject("AuxiliaryJumpscareImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            jumpscareRect = imageObject.GetComponent<RectTransform>();
            jumpscareRect.SetParent(overlayRect, false);
            jumpscareImage = imageObject.GetComponent<Image>();
        }
        else
        {
            jumpscareRect = jumpscareImage.rectTransform;
            if (jumpscareRect.parent != overlayRect)
            {
                jumpscareRect.SetParent(overlayRect, false);
            }
        }

        MatchParentRect(jumpscareRect);

        jumpscareImage.raycastTarget = false;
        jumpscareImage.preserveAspect = true;
        jumpscareImage.color = Color.white;
        jumpscareImage.enabled = false;
    }

    public void Play(Sprite sprite, AuxiliaryEvidenceDefinition definition)
    {
        if (sprite == null || jumpscareImage == null)
            return;

        float maxScale = definition != null ? definition.JumpscareMaxScale : 1.65f;
        float scaleInSeconds = definition != null ? definition.JumpscareScaleInSeconds : 0.12f;
        float holdSeconds = definition != null ? definition.JumpscareHoldSeconds : 0.18f;
        float scaleOutSeconds = definition != null ? definition.JumpscareScaleOutSeconds : 0.16f;

        jumpscareSequence?.Kill();
        jumpscareImage.sprite = sprite;
        jumpscareImage.enabled = true;
        jumpscareImage.color = Color.white;
        MatchParentRect(overlayRect);
        MatchParentRect(jumpscareRect);
        overlayRect.SetAsLastSibling();
        overlayCanvas.sortingOrder = sortingOrder;
        overlayCanvasGroup.alpha = 1f;
        jumpscareRect.localScale = Vector3.zero;

        jumpscareSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(jumpscareRect.DOScale(maxScale, scaleInSeconds).SetEase(Ease.OutExpo))
            .AppendInterval(holdSeconds)
            .Append(jumpscareRect.DOScale(0f, scaleOutSeconds).SetEase(Ease.InBack))
            .OnComplete(Hide);
    }

    private static void MatchParentRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void Hide()
    {
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
        }

        if (jumpscareImage != null)
        {
            jumpscareImage.enabled = false;
        }

        if (jumpscareRect != null)
        {
            jumpscareRect.localScale = Vector3.zero;
        }
    }

    private void OnDisable()
    {
        jumpscareSequence?.Kill();
        jumpscareSequence = null;
        Hide();
    }
}
