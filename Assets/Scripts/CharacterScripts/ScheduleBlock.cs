using UnityEngine;

[System.Serializable]
public class ScheduleBlock
{
    [Header("Time Settings (24h)")]
    [Range(0f, 24f)] public float startHour;
    [Range(0f, 24f)] public float endHour;

    [Header("Actual Behavior")]
    public PrisonerAction actualAction;

    public ScheduleBlock(float startHour, float endHour, PrisonerAction actualAction)
    {
        this.startHour = startHour;
        this.endHour = endHour;
        this.actualAction = actualAction;
    }
}
