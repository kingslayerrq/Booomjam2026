using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Space]
    public List<TimeSegment> segments;

    /// <summary>
    /// Returns the total real-world duration of the full day in seconds,
    /// summed across all segments.
    /// </summary>
    public float TotalRealDuration()
    {
        float total = 0f;
        foreach (var seg in segments) total += seg.realDurationInSeconds;
        return total;
    }

    /// <summary>
    /// Converts a game-hour value into the real elapsed seconds at which that hour is reached.
    /// Used once on day start to precompute _realSecondsToNoon.
    /// </summary>
    public float RealSecondsAtGameHour(float targetHour)
    {
        float elapsed = 0f;
        foreach (var seg in segments)
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
        float remaining = realSeconds;
        foreach (var seg in segments)
        {
            if (remaining <= seg.realDurationInSeconds)
            {
                float t = remaining / seg.realDurationInSeconds;
                return Mathf.Lerp(seg.gameStartHour, seg.gameEndHour, t);
            }
            remaining -= seg.realDurationInSeconds;
        }
        return segments[segments.Count - 1].gameEndHour;
    }

    private void OnValidate()
    {
        if (segments == null || segments.Count == 0) return;

        float maxHour = (float)dayFormat;

        // 1. Validate that the sequence covers the entire day from 0 to Max
        if (!Mathf.Approximately(segments[0].gameStartHour, 0f))
        {
            Debug.LogWarning($"[DayTimeConfig] '{name}' - The first segment must start at hour 0.", this);
        }

        if (!Mathf.Approximately(segments[segments.Count - 1].gameEndHour, maxHour))
        {
            Debug.LogWarning($"[DayTimeConfig] '{name}' - The last segment must end at hour {maxHour} (Current format: {dayFormat}).", this);
        }

        // 2. Validate individual segments and connectivity
        for (int i = 0; i < segments.Count; i++)
        {
            // Ensure segment isn't backwards or 0 duration
            if (segments[i].gameStartHour >= segments[i].gameEndHour)
            {
                Debug.LogWarning($"[DayTimeConfig] '{name}' - Segment {i} is invalid: Start Hour ({segments[i].gameStartHour}) must be less than End Hour ({segments[i].gameEndHour}).", this);
            }

            // Ensure this segment connects perfectly to the next one
            if (i < segments.Count - 1)
            {
                if (!Mathf.Approximately(segments[i].gameEndHour, segments[i + 1].gameStartHour))
                {
                    Debug.LogWarning($"[DayTimeConfig] '{name}' - Gap or overlap detected! Segment {i} ends at {segments[i].gameEndHour}, but Segment {i + 1} starts at {segments[i + 1].gameStartHour}.", this);
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