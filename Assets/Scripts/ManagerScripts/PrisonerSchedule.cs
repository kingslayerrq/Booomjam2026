using System.Collections.Generic;
using UnityEngine;

public class PrisonerSchedule : MonoBehaviour
{
    [System.Serializable]
    public class ScheduleEntry
    {
        public float startHour;
        public float endHour;
        public PrisonerAction officialAction;
        public string targetRoomName;
    }

    private static PrisonerSchedule instance;

    public static PrisonerSchedule Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<PrisonerSchedule>();
            return instance;
        }
    }

    private readonly Dictionary<string, List<ScheduleEntry>> schedules =
        new Dictionary<string, List<ScheduleEntry>>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void SetSchedule(string prisonerID, List<ScheduleEntry> entries)
    {
        schedules[prisonerID] = entries;
    }

    public IReadOnlyList<ScheduleEntry> GetSchedule(string prisonerID)
    {
        return schedules.TryGetValue(prisonerID, out List<ScheduleEntry> entries) ? entries : null;
    }

    public void ClearSchedules()
    {
        schedules.Clear();
    }

    public void LogAll()
    {
        foreach (KeyValuePair<string, List<ScheduleEntry>> pair in schedules)
        {
            string blocks = string.Join(", ", pair.Value.ConvertAll(e =>
                $"[{e.startHour}-{e.endHour}: {e.officialAction?.ActionName ?? "free"} @ {e.targetRoomName ?? "?"}]"));
            Debug.Log($"[PrisonerSchedule] {pair.Key}: {blocks}");
        }
    }
}
