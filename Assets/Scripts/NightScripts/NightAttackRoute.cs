using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NightRouteWaypoint
{
    [RoomDropdown]
    public string roomName;
    [Min(0f)]
    public float stayDuration = 5f;
}

[CreateAssetMenu(fileName = "NightAttackRoute", menuName = "Night/Attack Route")]
public class NightAttackRoute : ScriptableObject
{
    [Tooltip("Rooms the attacker passes through before reaching the final door room.")]
    public List<NightRouteWaypoint> waypoints = new List<NightRouteWaypoint>();

    [RoomDropdown]
    [Tooltip("Legacy fallback. NightAttackManager now sends attackers to its configured surveillance room.")]
    public string finalRoomName;

    [Min(0.1f)]
    public float moveSpeed = 1.5f;

    public string PenultimateRoomName
    {
        get
        {
            if (waypoints == null || waypoints.Count == 0)
                return null;

            return waypoints[waypoints.Count - 1]?.roomName;
        }
    }
}
