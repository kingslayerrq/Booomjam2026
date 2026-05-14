using UnityEngine;

[CreateAssetMenu(fileName = "AuxiliaryEvidenceDefinition", menuName = "Evidence/Auxiliary Evidence")]
public class AuxiliaryEvidenceDefinition : ScriptableObject
{
    [SerializeField] private AuxiliaryEvidenceType evidenceType;
    [SerializeField] private string displayName;

    [Header("Constant Movement")]
    [SerializeField] private float constantMovementRetargetSeconds = 0.35f;
    [SerializeField] private float constantMovementSpeedMultiplier = 2f;

    [Header("Battery")]
    [Tooltip("Extra battery drained per second while the controlling camera is active in this room.")]
    [SerializeField] private float abnormalBatteryDrainPerSecond = 0.02f;

    [Header("Visual")]
    [SerializeField] private Color featureMismatchColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Object Movement")]
    [SerializeField] private Vector3 objectMoveLocalOffset = new Vector3(0.35f, 0f, 0.2f);

    public AuxiliaryEvidenceType EvidenceType => evidenceType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? evidenceType.ToString() : displayName;
    public float ConstantMovementRetargetSeconds => Mathf.Max(0.05f, constantMovementRetargetSeconds);
    public float ConstantMovementSpeedMultiplier => Mathf.Max(1f, constantMovementSpeedMultiplier);
    public float AbnormalBatteryDrainPerSecond => Mathf.Max(0f, abnormalBatteryDrainPerSecond);
    public Color FeatureMismatchColor => featureMismatchColor;
    public Vector3 ObjectMoveLocalOffset => objectMoveLocalOffset;

    public static AuxiliaryEvidenceDefinition CreateRuntimeFallback(AuxiliaryEvidenceType evidenceType)
    {
        AuxiliaryEvidenceDefinition definition = CreateInstance<AuxiliaryEvidenceDefinition>();
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
