using System.Collections.Generic;
using UnityEngine;

public class PrisonerSchedule : MonoBehaviour
{
    private static readonly Dictionary<string, string> RoomDisplayIds = new Dictionary<string, string>
    {
        { "Prison_Administration_Office", "Room01" },
        { "Interrogation_Room", "Room02" },
        { "FourPeople_Room", "Room03" },
        { "Guard_Break_Room", "Room04" },
        { "FourPeopleDorm02", "Room05" },
        { "Loading_Dock_Room", "Room06" },
        { "Cafe", "Room07" },
        { "Electrical_Room", "Room08" },
        { "Prison_Workshop", "Room09" },
        { "Gate", "Room10" },
        { "FittingRoom", "Room11" }
    };

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
        schedules[prisonerID] = BuildDisplayEntries(entries);
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

    public static string ToDisplayRoomId(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            return roomName;

        return RoomDisplayIds.TryGetValue(roomName, out string displayId)
            ? displayId
            : roomName;
    }

    private static List<ScheduleEntry> BuildDisplayEntries(IReadOnlyList<ScheduleEntry> entries)
    {
        var displayEntries = new List<ScheduleEntry>();
        if (entries == null)
            return displayEntries;

        for (int i = 0; i < entries.Count; i++)
        {
            ScheduleEntry entry = entries[i];
            if (entry == null)
                continue;

            displayEntries.Add(new ScheduleEntry
            {
                startHour = entry.startHour,
                endHour = entry.endHour,
                officialAction = entry.officialAction,
                targetRoomName = ToDisplayRoomId(entry.targetRoomName)
            });
        }

        return displayEntries;
    }
}
