using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DayScheduleConfig", menuName = "Schedule/Day Schedule Config")]
public class DayScheduleConfig : ScriptableObject
{
    public List<ScheduleBlockConfig> scheduleBlocks;

    [Header("Evidence Settings")]
    public List<HighRiskEvidenceDefinition> highRiskEvidencePool;
    public List<AuxiliaryEvidenceDefinition> auxiliaryEvidencePool;
    [Range(0f, 1f)] public float goodAuxiliaryChance = 0.2f;
    [Min(0)] public int badAuxiliaryMinPerDay = 1;
    [Min(0)] public int badAuxiliaryMaxPerDay = 4;

    private void OnValidate()
    {
        ValidateSchedule();
    }

    [ContextMenu("Validate Schedule")]
    public void ValidateSchedule()
    {
        if (scheduleBlocks == null || scheduleBlocks.Count == 0) return;

        bool hasErrors = false;

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            var block = scheduleBlocks[i];

            // 1. Check Action Uniqueness
            if (block.goodActions != null && block.goodActions.Count > 0)
            {
                HashSet<PrisonerAction> uniqueGood = new HashSet<PrisonerAction>(block.goodActions);
                if (uniqueGood.Count != block.goodActions.Count)
                {
                    hasErrors = true;
                    Debug.LogError($"[DayScheduleConfig] '{name}': Please remove duplicate GOOD actions in Block {i}.", this);
                }
            }
            
            if (block.badActions != null && block.badActions.Count > 0)
            {
                HashSet<PrisonerAction> uniqueBad = new HashSet<PrisonerAction>(block.badActions);
                if (uniqueBad.Count != block.badActions.Count)
                {
                    hasErrors = true;
                    Debug.LogError($"[DayScheduleConfig] '{name}': Please remove duplicate bad actions in Block {i}.", this);
                }
            }

            // 2. Check Hour Validity
            if (block.startHour >= block.endHour)
            {
                Debug.LogWarning($"[DayScheduleConfig] '{name}': Block {i} invalid: Start ({block.startHour}) >= End ({block.endHour}).", this);
                hasErrors = true;
            }

            // 3. Check Overlapping (Inner loop)
            for (int j = i + 1; j < scheduleBlocks.Count; j++)
            {
                var blockB = scheduleBlocks[j];
                bool isOverlapping = (block.startHour < blockB.endHour) && (blockB.startHour < block.endHour);

                if (isOverlapping)
                {
                    Debug.LogError($"[DayScheduleConfig] '{name}': Overlap between Block {i} and Block {j}!", this);
                    hasErrors = true;
                }
            }
        }

        if (!hasErrors) Debug.Log($"[DayScheduleConfig] '{name}' is valid.", this);
    }
}

[System.Serializable]
public class ScheduleBlockConfig
{
    [Range(0f, 24f)] public float startHour;
    [Range(0f, 24f)] public float endHour;

    public List<PrisonerAction> goodActions;  // duplicates allowed, picked randomly per prisoner
    public List<PrisonerAction> badActions;   // no duplicate assignment at runtime
    
}
