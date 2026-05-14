using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Prisoner
{
    [SerializeField] private PrisonerData prisonerData;

    [Header("Runtime Info")] 
    [SerializeField] private bool isBad;
    [SerializeField] private bool isLockedUp;
    [SerializeField] private HighRiskEvidenceType highRiskEvidenceType;
    [SerializeField] private List<ScheduleBlock> dailySchedule;
    [NonSerialized] private HighRiskEvidenceDefinition highRiskEvidenceDefinition;
    
    public PrisonerData PrisonerData => prisonerData;
    public bool IsBad => isBad;
    public bool IsLockedUp => isLockedUp;
    public bool HasHighRiskEvidence => highRiskEvidenceType != HighRiskEvidenceType.None;
    public HighRiskEvidenceType HighRiskEvidenceType => highRiskEvidenceType;
    public HighRiskEvidenceDefinition HighRiskEvidenceDefinition => highRiskEvidenceDefinition;
    public List<ScheduleBlock> DailySchedule => dailySchedule;

    public Prisoner(PrisonerData data, bool isBad = false)
    {
        this.prisonerData = data;
        this.isBad = isBad;
        this.isLockedUp = false;
        this.highRiskEvidenceType = HighRiskEvidenceType.None;
        this.dailySchedule = new List<ScheduleBlock>();
    }
    
    public string PrisonerName => prisonerData.PrisonerName;
    public string PrisonerID => prisonerData.PrisonerID;

    public void MakeBad(bool bad)
    {
        isBad = bad;
    }

    public void SetHighRiskEvidence(HighRiskEvidenceDefinition definition)
    {
        highRiskEvidenceDefinition = definition;
        highRiskEvidenceType = definition != null ? definition.EvidenceType : HighRiskEvidenceType.None;
    }

    public void SetHighRiskEvidence(HighRiskEvidenceType evidenceType)
    {
        highRiskEvidenceDefinition = null;
        highRiskEvidenceType = evidenceType;
    }

    public void ClearEvidence()
    {
        highRiskEvidenceDefinition = null;
        highRiskEvidenceType = HighRiskEvidenceType.None;

        foreach (ScheduleBlock block in dailySchedule)
        {
            block?.ClearAuxiliaryEvidence();
        }
    }

    public void LockUp()
    {
        isLockedUp = true;
        Debug.Log($"{PrisonerID} is locked");
    }
    
    public void ReleaseLockUp()
    {
        isLockedUp = false;
        Debug.Log($"{PrisonerID} is released");
    }

    public void AddSchedule(ScheduleBlock scheduleBlock)
    {
        dailySchedule.Add(scheduleBlock);
    }
    
    public void ClearSchedule()
    {
        dailySchedule.Clear();
    }
}
