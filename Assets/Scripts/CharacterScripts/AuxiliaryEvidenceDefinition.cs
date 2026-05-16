using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AuxiliaryEvidenceDefinition", menuName = "Evidence/Auxiliary Evidence")]
public class AuxiliaryEvidenceDefinition : ScriptableObject
{
    [SerializeField] private AuxiliaryEvidenceType evidenceType;
    [SerializeField] private string displayName;

    [Header("Out Of Schedule Room")]
    [Tooltip("Rooms this auxiliary is allowed to move a prisoner into. Leave empty to disable the behavior until configured.")]
    [SerializeField] private List<string> alternateRooms = new List<string>();

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

    [Header("Jumpscare")]
    [SerializeField] private float jumpscareMaxScale = 1.65f;
    [SerializeField] private float jumpscareScaleInSeconds = 0.12f;
    [SerializeField] private float jumpscareHoldSeconds = 0.18f;
    [SerializeField] private float jumpscareScaleOutSeconds = 0.16f;

    public AuxiliaryEvidenceType EvidenceType => evidenceType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? evidenceType.ToString() : displayName;
    public IReadOnlyList<string> AlternateRooms => alternateRooms;
    public float ConstantMovementRetargetSeconds => Mathf.Max(0.05f, constantMovementRetargetSeconds);
    public float ConstantMovementSpeedMultiplier => Mathf.Max(1f, constantMovementSpeedMultiplier);
    public float AbnormalBatteryDrainPerSecond => Mathf.Max(0f, abnormalBatteryDrainPerSecond);
    public Color FeatureMismatchColor => featureMismatchColor;
    public Vector3 ObjectMoveLocalOffset => objectMoveLocalOffset;
    public float JumpscareMaxScale => Mathf.Max(1f, jumpscareMaxScale);
    public float JumpscareScaleInSeconds => Mathf.Max(0.01f, jumpscareScaleInSeconds);
    public float JumpscareHoldSeconds => Mathf.Max(0f, jumpscareHoldSeconds);
    public float JumpscareScaleOutSeconds => Mathf.Max(0.01f, jumpscareScaleOutSeconds);

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
