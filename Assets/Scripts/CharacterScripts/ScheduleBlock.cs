using System;
using UnityEngine;

[System.Serializable]
public class ScheduleBlock
{
    [Header("Time Settings (24h)")]
    [Range(0f, 24f)] public float startHour;
    [Range(0f, 24f)] public float endHour;

    [Header("Actual Behavior")]
    public PrisonerAction actualAction;
    public bool isConcreteBadAction;

    [Header("Runtime")]
    public string resolvedTargetRoomName;

    [Header("Evidence Variant")]
    public AuxiliaryEvidenceType auxiliaryEvidenceType;
    [NonSerialized] public AuxiliaryEvidenceDefinition auxiliaryEvidenceDefinition;

    public bool HasAuxiliaryEvidence => auxiliaryEvidenceType != AuxiliaryEvidenceType.None;

    public ScheduleBlock(float startHour, float endHour, PrisonerAction actualAction, bool isConcreteBadAction = false)
    {
        this.startHour = startHour;
        this.endHour = endHour;
        this.actualAction = actualAction;
        this.isConcreteBadAction = isConcreteBadAction;
        ClearAuxiliaryEvidence();
    }

    public void SetAuxiliaryEvidence(AuxiliaryEvidenceDefinition definition)
    {
        auxiliaryEvidenceDefinition = definition;
        auxiliaryEvidenceType = definition != null ? definition.EvidenceType : AuxiliaryEvidenceType.None;
    }

    public void SetAuxiliaryEvidence(AuxiliaryEvidenceType evidenceType)
    {
        auxiliaryEvidenceDefinition = null;
        auxiliaryEvidenceType = evidenceType;
    }

    public void ClearAuxiliaryEvidence()
    {
        auxiliaryEvidenceDefinition = null;
        auxiliaryEvidenceType = AuxiliaryEvidenceType.None;
    }
}
