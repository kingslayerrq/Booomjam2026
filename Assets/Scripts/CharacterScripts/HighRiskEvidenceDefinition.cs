using UnityEngine;

[CreateAssetMenu(fileName = "HighRiskEvidenceDefinition", menuName = "Evidence/High Risk Evidence")]
public class HighRiskEvidenceDefinition : ScriptableObject
{
    [SerializeField] private HighRiskEvidenceType evidenceType;
    [SerializeField] private string displayName;
    [SerializeField] private float observeSeconds = 3f;

    [Header("Day Cue")]
    [SerializeField] private float flickerMaxAlpha = 0.18f;
    [SerializeField] private Color spiritOrbColor = new Color(0.7f, 0.95f, 1f, 0.75f);
    [SerializeField] private Vector2 spiritOrbSize = new Vector2(34f, 34f);
    [SerializeField] private AudioClip strangeSoundClip;
    [SerializeField] private float strangeSoundInterval = 5f;

    [Header("Night Consequence")]
    [SerializeField] private float nightFeedInterferenceIntensity = 0.12f;
    [SerializeField] private float nightAudioPromptMultiplierPenalty = 0.25f;
    [SerializeField] private float energyMaxReduction = 20f;

    public HighRiskEvidenceType EvidenceType => evidenceType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? evidenceType.ToString() : displayName;
    public float ObserveSeconds => Mathf.Max(0f, observeSeconds);
    public float FlickerMaxAlpha => Mathf.Max(0f, flickerMaxAlpha);
    public Color SpiritOrbColor => spiritOrbColor;
    public Vector2 SpiritOrbSize => spiritOrbSize;
    public AudioClip StrangeSoundClip => strangeSoundClip;
    public float StrangeSoundInterval => Mathf.Max(0.1f, strangeSoundInterval);
    public float NightFeedInterferenceIntensity => Mathf.Max(0f, nightFeedInterferenceIntensity);
    public float NightAudioPromptMultiplierPenalty => Mathf.Max(0f, nightAudioPromptMultiplierPenalty);
    public float EnergyMaxReduction => Mathf.Max(0f, energyMaxReduction);

    public static HighRiskEvidenceDefinition CreateRuntimeFallback(HighRiskEvidenceType evidenceType)
    {
        HighRiskEvidenceDefinition definition = CreateInstance<HighRiskEvidenceDefinition>();
        definition.evidenceType = evidenceType;
        definition.displayName = evidenceType.ToString();
        return definition;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = evidenceType.ToString();
        }
    }
}
