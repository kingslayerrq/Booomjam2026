using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NightAttackConfig", menuName = "Night/Attack Config")]
public class NightAttackConfig : ScriptableObject
{
    [Tooltip("Total number of attackers to launch over the course of this night.")]
    [Min(0)]
    public int totalAttackers = 1;

    [Tooltip("Maximum attackers active simultaneously. Left and right surveillance destinations can each hold one attacker.")]
    [Range(1, 3)]
    public int maxSimultaneous = 1;

    [Tooltip("Seconds to wait before launching the next wave after a slot opens.")]
    [Min(0f)]
    public float waveLaunchInterval = 3f;

    [Tooltip("Chance that each launched night attacker is invisible. Invisible attackers hide their renderers and emit footstep audio while waiting in waypoint rooms.")]
    [Range(0f, 1f)]
    public float invisibleAttackerChance = 0.35f;

    [Tooltip("Routes available this night. The last waypoint decides the surveillance side: Prison_Administration_Office = left, Interrogation_Room = right.")]
    public List<NightAttackRoute> routePool = new List<NightAttackRoute>();
}
