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
    [Tooltip("The room containing the door the attacker is targeting (e.g. SurveillanceRoom).")]
    public string finalRoomName;

    [Min(0.1f)]
    public float moveSpeed = 1.5f;
}
