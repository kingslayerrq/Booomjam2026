using UnityEngine;
using TMPro;

public class PrisonerScheduleEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeLabel;
    [SerializeField] private TextMeshProUGUI roomLabel;

    public void Setup(PrisonerSchedule.ScheduleEntry entry)
    {
        if (timeLabel != null)
            timeLabel.text = FormatHour(entry.startHour);

        if (roomLabel != null)
            roomLabel.text = string.IsNullOrEmpty(entry.targetRoomName) ? "—" : entry.targetRoomName;

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (timeLabel != null) timeLabel.text = "—";
        if (roomLabel != null) roomLabel.text = "—";
    }

    private static string FormatHour(float hour)
    {
        int h = Mathf.FloorToInt(hour);
        int m = Mathf.RoundToInt((hour - h) * 60f);
        return $"{h:D2}:{m:D2}";
    }
}
