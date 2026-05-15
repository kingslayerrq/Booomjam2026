using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NightAttackConfig", menuName = "Night/Attack Config")]
public class NightAttackConfig : ScriptableObject
{
    [Tooltip("Total number of attackers to launch over the course of this night.")]
    [Min(0)]
    public int totalAttackers = 1;

    [Tooltip("Maximum attackers active simultaneously. Each must target a different door. Max 3.")]
    [Range(1, 3)]
    public int maxSimultaneous = 1;

    [Tooltip("Seconds to wait before launching the next wave after a slot opens.")]
    [Min(0f)]
    public float waveLaunchInterval = 3f;

    [Tooltip("Routes available this night. Needs at least maxSimultaneous routes with different finalRoomNames.")]
    public List<NightAttackRoute> routePool = new List<NightAttackRoute>();
}
