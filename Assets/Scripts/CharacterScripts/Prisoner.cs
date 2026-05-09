using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Prisoner
{
    [SerializeField] private PrisonerData prisonerData;

    [Header("Runtime Info")] 
    [SerializeField] private bool isBad;
    [SerializeField] private bool isLockedUp;
    [SerializeField] private List<ScheduleBlock> dailySchedule;
    
    public PrisonerData PrisonerData => prisonerData;
    public bool IsBad => isBad;
    public bool IsLockedUp => isLockedUp;
    public List<ScheduleBlock> DailySchedule => dailySchedule;

    public Prisoner(PrisonerData data, bool isBad = false)
    {
        this.prisonerData = data;
        this.isBad = isBad;
        this.isLockedUp = false;
        this.dailySchedule = new List<ScheduleBlock>();
    }
    
    public string PrisonerName => prisonerData.PrisonerName;
    public string PrisonerID => prisonerData.PrisonerID;

    public void MakeBad(bool bad)
    {
        isBad = bad;
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
