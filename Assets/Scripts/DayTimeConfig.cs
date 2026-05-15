using System.Collections.Generic;
using UnityEngine;

public enum DayPhase
{
    Day,
    Night
}

public enum DayFormat
{
    TwelveHour = 12,
    TwentyFourHour = 24
}

[CreateAssetMenu(fileName = "DayTimeConfig", menuName = "Schedule/Day Time Config")]
public class DayTimeConfig : ScriptableObject
{
    [Header("Configuration")]
    [Tooltip("Determines if a full day is 12 hours or 24 hours.")]
    public DayFormat dayFormat = DayFormat.TwentyFourHour;

    [Header("Day Phase")]
    public List<TimeSegment> daySegments;

    [Header("Night Phase")]
    public List<TimeSegment> nightSegments;

    [Space]
    [Tooltip("Legacy full-day segments. Used as a fallback until Day/Night segments are authored.")]
    public List<TimeSegment> segments;

    /// <summary>
    /// Returns the total real-world duration of the full day in seconds,
    /// summed across all segments.
    /// </summary>
    public float TotalRealDuration()
    {
        return TotalRealDuration(GetConfiguredSegments(DayPhase.Day));
    }

    public float TotalRealDuration(DayPhase phase)
    {
        return TotalRealDuration(GetConfiguredSegments(phase));
    }

    /// <summary>
    /// Converts a game-hour value into the real elapsed seconds at which that hour is reached.
    /// Used once on day start to precompute _realSecondsToNoon.
    /// </summary>
    public float RealSecondsAtGameHour(float targetHour)
    {
        return RealSecondsAtGameHour(DayPhase.Day, targetHour);
    }

    public float RealSecondsAtGameHour(DayPhase phase, float targetHour)
    {
        float elapsed = 0f;
        foreach (var seg in GetConfiguredSegments(phase))
        {
            if (targetHour <= seg.gameStartHour) break;
            if (targetHour >= seg.gameEndHour)
            {
                elapsed += seg.realDurationInSeconds;
            }
            else
            {
                float t = (targetHour - seg.gameStartHour) / (seg.gameEndHour - seg.gameStartHour);
                elapsed += t * seg.realDurationInSeconds;
                break;
            }
        }
        return elapsed;
    }

    /// <summary>
    /// Converts real elapsed seconds into the current game hour using piecewise linear mapping.
    /// Called every frame by DayManager.CurrentHour.
    /// </summary>
    public float GameHourAtRealSeconds(float realSeconds)
    {
        return GameHourAtRealSeconds(DayPhase.Day, realSeconds);
    }

    public float GameHourAtRealSeconds(DayPhase phase, float realSeconds)
    {
        float remaining = realSeconds;
        List<TimeSegment> configuredSegments = GetConfiguredSegments(phase);
        foreach (var seg in configuredSegments)
        {
            if (remaining <= seg.realDurationInSeconds)
            {
                float t = remaining / seg.realDurationInSeconds;
                return Mathf.Lerp(seg.gameStartHour, seg.gameEndHour, t);
            }
            remaining -= seg.realDurationInSeconds;
        }
        return configuredSegments.Count > 0 ? configuredSegments[configuredSegments.Count - 1].gameEndHour : 0f;
    }

    public float PhaseStartHour(DayPhase phase)
    {
        List<TimeSegment> configuredSegments = GetConfiguredSegments(phase);
        return configuredSegments.Count > 0 ? configuredSegments[0].gameStartHour : 0f;
    }

    public float PhaseEndHour(DayPhase phase)
    {
        List<TimeSegment> configuredSegments = GetConfiguredSegments(phase);
        return configuredSegments.Count > 0 ? configuredSegments[configuredSegments.Count - 1].gameEndHour : 0f;
    }

    private List<TimeSegment> GetConfiguredSegments(DayPhase phase)
    {
        List<TimeSegment> configuredSegments = phase == DayPhase.Day ? daySegments : nightSegments;
        if (configuredSegments != null && configuredSegments.Count > 0)
            return configuredSegments;

        if (segments != null && segments.Count > 0)
            return segments;

        return new List<TimeSegment>();
    }

    private static float TotalRealDuration(List<TimeSegment> configuredSegments)
    {
        float total = 0f;
        foreach (var seg in configuredSegments)
        {
            total += Mathf.Max(0f, seg.realDurationInSeconds);
        }

        return total;
    }

    private void OnValidate()
    {
        ValidateSegments(daySegments, "Day");
        ValidateSegments(nightSegments, "Night");
        ValidateSegments(segments, "Legacy");
    }

    private void ValidateSegments(List<TimeSegment> configuredSegments, string label)
    {
        if (configuredSegments == null || configuredSegments.Count == 0) return;
        float maxHour = (float)dayFormat;

        for (int i = 0; i < configuredSegments.Count; i++)
        {
            if (configuredSegments[i].gameStartHour >= configuredSegments[i].gameEndHour)
            {
                Debug.LogWarning($"[DayTimeConfig] '{name}' - {label} segment {i} is invalid: Start Hour ({configuredSegments[i].gameStartHour}) must be less than End Hour ({configuredSegments[i].gameEndHour}).", this);
            }

            if (configuredSegments[i].gameEndHour > maxHour)
            {
                Debug.LogWarning($"[DayTimeConfig] '{name}' - {label} segment {i} ends after hour {maxHour}.", this);
            }

            if (i < configuredSegments.Count - 1)
            {
                if (!Mathf.Approximately(configuredSegments[i].gameEndHour, configuredSegments[i + 1].gameStartHour))
                {
                    Debug.LogWarning($"[DayTimeConfig] '{name}' - Gap or overlap detected in {label}! Segment {i} ends at {configuredSegments[i].gameEndHour}, but Segment {i + 1} starts at {configuredSegments[i + 1].gameStartHour}.", this);
                }
            }
        }
    }
}

[System.Serializable]
public class TimeSegment
{
    [Range(0f, 24f)] public float gameStartHour;
    [Range(0f, 24f)] public float gameEndHour;
    public float realDurationInSeconds;
}
