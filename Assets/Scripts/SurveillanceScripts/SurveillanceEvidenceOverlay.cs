using UnityEngine;
using UnityEngine.UI;

public class SurveillanceEvidenceOverlay : MonoBehaviour
{
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private Image flickerImage;
    [SerializeField] private Image spiritOrbImage;
    [SerializeField] private AudioSource audioSource;

    private HighRiskEvidenceType activeDayCue = HighRiskEvidenceType.None;
    private HighRiskEvidenceDefinition activeDefinition;
    private float nightInterferenceIntensity;
    private float nextStrangeSoundTime;
    private AudioClip fallbackStrangeSoundClip;
    private Sprite spiritOrbSprite;

    public void EnsureSetup(RectTransform parent)
    {
        if (parent == null)
            return;

        if (overlayRoot == null)
        {
            GameObject overlayObject = new GameObject("EvidenceOverlay", typeof(RectTransform));
            overlayRoot = overlayObject.GetComponent<RectTransform>();
            overlayRoot.SetParent(parent, false);
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.SetAsLastSibling();
        }

        if (flickerImage == null)
        {
            flickerImage = CreateOverlayImage("EvidenceFlicker", overlayRoot, Color.clear, true);
        }

        if (spiritOrbImage == null)
        {
            spiritOrbImage = CreateOverlayImage("EvidenceSpiritOrb", overlayRoot, Color.clear, false);
            spiritOrbImage.sprite = GetSpiritOrbSprite();
            spiritOrbImage.preserveAspect = true;
            spiritOrbImage.rectTransform.sizeDelta = new Vector2(34f, 34f);
        }

        if (audioSource == null)
        {
            audioSource = overlayRoot.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = overlayRoot.gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    public void ShowDayCue(HighRiskEvidenceDefinition definition)
    {
        if (definition == null || definition.EvidenceType == HighRiskEvidenceType.None)
        {
            ClearDayCue();
            return;
        }

        bool changedCue = activeDayCue != definition.EvidenceType || activeDefinition != definition;
        activeDayCue = definition.EvidenceType;
        activeDefinition = definition;

        if (changedCue && activeDayCue == HighRiskEvidenceType.StrangeSound)
        {
            PlayStrangeSound();
        }
    }

    public void ClearDayCue()
    {
        activeDayCue = HighRiskEvidenceType.None;
        activeDefinition = null;
        nextStrangeSoundTime = 0f;

        if (spiritOrbImage != null)
        {
            spiritOrbImage.gameObject.SetActive(false);
        }
    }

    public void SetNightInterference(float intensity)
    {
        nightInterferenceIntensity = Mathf.Max(0f, intensity);
    }

    private void Update()
    {
        UpdateFlicker();
        UpdateSpiritOrb();
        UpdateStrangeSound();
    }

    private void UpdateFlicker()
    {
        if (flickerImage == null)
            return;

        float cueAlpha = activeDayCue == HighRiskEvidenceType.CameraFlicker && activeDefinition != null
            ? activeDefinition.FlickerMaxAlpha
            : 0f;
        float maxAlpha = Mathf.Max(cueAlpha, nightInterferenceIntensity);
        bool isActive = maxAlpha > 0f;

        flickerImage.gameObject.SetActive(isActive);
        if (!isActive)
            return;

        float alpha = Random.Range(0f, maxAlpha);
        flickerImage.color = new Color(1f, 1f, 1f, alpha);
    }

    private void UpdateSpiritOrb()
    {
        if (spiritOrbImage == null)
            return;

        bool isActive = activeDayCue == HighRiskEvidenceType.SpiritOrb && activeDefinition != null;
        spiritOrbImage.gameObject.SetActive(isActive);
        if (!isActive)
            return;

        RectTransform rectTransform = spiritOrbImage.rectTransform;
        rectTransform.sizeDelta = activeDefinition.SpiritOrbSize;

        float x = Mathf.Sin(Time.time * 0.83f) * 0.42f;
        float y = Mathf.Cos(Time.time * 0.57f) * 0.32f;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(
            x * overlayRoot.rect.width,
            y * overlayRoot.rect.height
        );
        spiritOrbImage.color = activeDefinition.SpiritOrbColor;
    }

    private void UpdateStrangeSound()
    {
        if (activeDayCue != HighRiskEvidenceType.StrangeSound || activeDefinition == null)
            return;

        if (Time.time < nextStrangeSoundTime)
            return;

        PlayStrangeSound();
    }

    private void PlayStrangeSound()
    {
        if (audioSource == null || activeDefinition == null)
            return;

        AudioClip clip = activeDefinition.StrangeSoundClip != null
            ? activeDefinition.StrangeSoundClip
            : GetFallbackStrangeSoundClip();

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }

        nextStrangeSoundTime = Time.time + activeDefinition.StrangeSoundInterval;
    }

    private AudioClip GetFallbackStrangeSoundClip()
    {
        if (fallbackStrangeSoundClip != null)
            return fallbackStrangeSoundClip;

        int frequency = 22050;
        int sampleCount = frequency;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)frequency;
            float wave = Mathf.Sin(t * 2f * Mathf.PI * 120f) * Mathf.Sin(t * 2f * Mathf.PI * 7f);
            samples[i] = wave * 0.14f;
        }

        fallbackStrangeSoundClip = AudioClip.Create("EvidenceStrangeSound", sampleCount, 1, frequency, false);
        fallbackStrangeSoundClip.SetData(samples, 0);
        return fallbackStrangeSoundClip;
    }

    private Sprite GetSpiritOrbSprite()
    {
        if (spiritOrbSprite != null)
            return spiritOrbSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        spiritOrbSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        spiritOrbSprite.hideFlags = HideFlags.HideAndDontSave;
        return spiritOrbSprite;
    }

    private static Image CreateOverlayImage(string name, RectTransform parent, Color color, bool stretch)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        if (stretch)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        imageObject.SetActive(false);
        return image;
    }
}
