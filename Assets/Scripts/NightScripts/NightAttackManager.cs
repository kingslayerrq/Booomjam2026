using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NightAttackManager : MonoBehaviour
{
    private const string AdminApproachRoomName = "Prison_Administration_Office";
    private const string InterrogationApproachRoomName = "Interrogation_Room";

    private enum AttackDestinationSide
    {
        Left,
        Right
    }

    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PrisonerManager prisonerManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Configuration")]
    [Tooltip("Per-night configs indexed by day number (same convention as masterSchedule).")]
    [SerializeField] private NightAttackConfig[] nightConfigs;
    [Tooltip("All attackable doors in the scene. Drag DoorInteractable GameObjects here.")]
    [SerializeField] private DoorInteractable[] doors;
    [RoomDropdown]
    [Tooltip("Final room for all night attackers.")]
    [SerializeField] private string surveillanceRoomName = "Surveillance_Room";
    [Tooltip("Final destination used when the previous room is Prison_Administration_Office.")]
    [SerializeField] private Transform leftFinalDestination;
    [Tooltip("Door the player must close for the left (Admin) approach.")]
    [SerializeField] private DoorInteractable leftDoor;
    [Tooltip("Final destination used when the previous room is Interrogation_Room.")]
    [SerializeField] private Transform rightFinalDestination;
    [Tooltip("Door the player must close for the right (Interrogation) approach.")]
    [SerializeField] private DoorInteractable rightDoor;
    [Tooltip("Seconds the player has to close the door once an attacker arrives.")]
    [SerializeField] private float doorCountdownSeconds = 5f;

    private readonly List<NightAttackerController> activeAttackers = new List<NightAttackerController>();
    private readonly Queue<Prisoner> pendingAttackers = new Queue<Prisoner>();
    private readonly Dictionary<NightAttackerController, DoorInteractable> attackerDoors =
        new Dictionary<NightAttackerController, DoorInteractable>();
    private readonly Dictionary<NightAttackerController, float> doorTimers =
        new Dictionary<NightAttackerController, float>();
    private readonly Dictionary<NightAttackerController, AttackDestinationSide> attackerSides =
        new Dictionary<NightAttackerController, AttackDestinationSide>();

    private NightAttackConfig currentConfig;
    private float nextWaveLaunchTime;
    private bool nightActive;

    public IReadOnlyDictionary<DoorInteractable, float> ActiveDoorTimers => BuildPublicTimers();
    public IReadOnlyList<NightAttackerController> ActiveAttackers => activeAttackers;
    public event Action OnDoorBreached;

    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted += HandleDayStarted;
            dayManager.OnNightStarted += HandleNightStarted;
            dayManager.OnNightEnded += HandleNightEnded;
        }
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted -= HandleDayStarted;
            dayManager.OnNightStarted -= HandleNightStarted;
            dayManager.OnNightEnded -= HandleNightEnded;
        }
    }

    private void Update()
    {
        if (!nightActive) return;

        TryLaunchNextWave();
    }

    private void LateUpdate()
    {
        if (!nightActive) return;

        TickDoorTimers();
    }

    private void HandleNightStarted()
    {
        int day = dayManager.CurrentDay;
        int configIndex = day - 1;
        if (nightConfigs == null || configIndex >= nightConfigs.Length || nightConfigs[configIndex] == null)
        {
            Debug.Log($"[NightAttackManager] No config for day {day} — skipping night attack.");
            return;
        }

        currentConfig = nightConfigs[configIndex];
        nightActive = true;

        SelectAndEnqueueAttackers();
        nextWaveLaunchTime = Time.time;

        Debug.Log($"[NightAttackManager] Night started — day {day}, " +
                  $"{pendingAttackers.Count} attackers queued, max simultaneous: {currentConfig.maxSimultaneous}");
    }

    private void HandleDayStarted()
    {
        CleanupNight();
    }

    private void HandleNightEnded()
    {
        CleanupNight();
    }

    private void SelectAndEnqueueAttackers()
    {
        var pool = new List<Prisoner>(prisonerManager.PrisonerList);
        Shuffle(pool);

        int count = Mathf.Min(currentConfig.totalAttackers, pool.Count);
        for (int i = 0; i < count; i++)
            pendingAttackers.Enqueue(pool[i]);
    }

    private void TryLaunchNextWave()
    {
        if (pendingAttackers.Count == 0) return;
        if (Time.time < nextWaveLaunchTime) return;

        int slotsAvailable = currentConfig.maxSimultaneous - activeAttackers.Count;
        if (slotsAvailable <= 0) return;

        int launched = 0;
        while (launched < slotsAvailable && pendingAttackers.Count > 0)
        {
            NightAttackRoute route = PickAvailableRoute();
            if (route == null)
            {
                Debug.LogWarning("[NightAttackManager] No available route with a free final destination — waiting.");
                break;
            }

            if (!TryGetDestinationForRoute(route, out AttackDestinationSide side, out Transform destination))
            {
                Debug.LogWarning($"[NightAttackManager] Route '{route.name}' has no valid surveillance destination.");
                break;
            }

            Prisoner prisoner = pendingAttackers.Dequeue();
            PrisonerActionController pac = prisonerManager.GetPrisonerController(prisoner);
            if (pac == null)
            {
                Debug.LogWarning($"[NightAttackManager] No controller for prisoner {prisoner.PrisonerID} — skipping.");
                continue;
            }

            NightAttackerController attacker = pac.gameObject.AddComponent<NightAttackerController>();
            attacker.OnArrivedAtDoor += HandleArrivedAtDoor;
            attacker.OnReturnedToCell += HandleReturnedToCell;
            activeAttackers.Add(attacker);
            attackerSides[attacker] = side;
            bool invisible = Random.value < currentConfig.invisibleAttackerChance;
            attacker.Initialize(route, pac, surveillanceRoomName, destination, invisible);
            launched++;
        }

        if (launched > 0)
            nextWaveLaunchTime = Time.time + currentConfig.waveLaunchInterval;
    }

    private NightAttackRoute PickAvailableRoute()
    {
        if (currentConfig.routePool == null) return null;

        foreach (NightAttackRoute route in currentConfig.routePool)
        {
            if (route == null) continue;

            if (!TryGetDestinationForRoute(route, out AttackDestinationSide side, out Transform destination))
                continue;

            if (destination == null)
                continue;

            bool sideTaken = false;
            foreach (AttackDestinationSide activeSide in attackerSides.Values)
            {
                if (activeSide == side)
                {
                    sideTaken = true;
                    break;
                }
            }

            if (!sideTaken) return route;
        }

        return null;
    }

    private void HandleArrivedAtDoor(NightAttackerController attacker)
    {
        attackerSides.TryGetValue(attacker, out AttackDestinationSide side);
        DoorInteractable door = side == AttackDestinationSide.Left ? leftDoor : rightDoor;

        if (door == null)
        {
            Debug.LogWarning($"[NightAttackManager] No door assigned for {side} side.");
            return;
        }

        attackerDoors[attacker] = door;
        doorTimers[attacker] = doorCountdownSeconds;

        Debug.Log($"[NightAttackManager] Attacker arrived at {side} door ({door.name}) — " +
                  $"{doorCountdownSeconds}s countdown started.");
    }

    private void HandleReturnedToCell(NightAttackerController attacker)
    {
        activeAttackers.Remove(attacker);
        attackerSides.Remove(attacker);
        Destroy(attacker);
    }

    private void TickDoorTimers()
    {
        // Collect keys to avoid modifying dict during iteration
        var keys = new List<NightAttackerController>(doorTimers.Keys);
        foreach (var attacker in keys)
        {
            doorTimers[attacker] -= Time.deltaTime;
            if (doorTimers[attacker] <= 0f)
                ResolveAttacker(attacker);
        }
    }

    private void ResolveAttacker(NightAttackerController attacker)
    {
        doorTimers.Remove(attacker);

        attackerDoors.TryGetValue(attacker, out DoorInteractable door);
        attackerDoors.Remove(attacker);
        attackerSides.Remove(attacker);

        if (door != null && !door.IsOpen)
        {
            Debug.Log($"[NightAttackManager] Door closed in time — returning attacker to cell.");
            attacker.ReturnToCell();
        }
        else
        {
            Debug.Log("[NightAttackManager] Door was open — player takes damage, night ending.");
            OnDoorBreached?.Invoke();
        }
    }

    private void CleanupNight()
    {
        if (!nightActive
            && activeAttackers.Count == 0
            && pendingAttackers.Count == 0
            && attackerDoors.Count == 0
            && doorTimers.Count == 0
            && attackerSides.Count == 0)
        {
            return;
        }

        nightActive = false;

        var attackersCopy = new List<NightAttackerController>(activeAttackers);
        foreach (var attacker in attackersCopy)
        {
            if (attacker == null) continue;
            attacker.OnReturnedToCell -= HandleReturnedToCell;
            attacker.ReturnToCell();
            Destroy(attacker);
        }

        activeAttackers.Clear();
        pendingAttackers.Clear();
        attackerDoors.Clear();
        doorTimers.Clear();
        attackerSides.Clear();
        currentConfig = null;

        Debug.Log("[NightAttackManager] Night cleanup complete.");
    }

    private DoorInteractable FindDoorForRoom(string roomName)
    {
        if (doors == null) return null;

        foreach (DoorInteractable door in doors)
        {
            if (door != null && door.AssignedRoomName == roomName)
                return door;
        }

        return null;
    }

    private bool TryGetDestinationForRoute(
        NightAttackRoute route,
        out AttackDestinationSide side,
        out Transform destination)
    {
        side = AttackDestinationSide.Left;
        destination = null;

        string approachRoomName = route != null ? route.PenultimateRoomName : null;
        if (approachRoomName == AdminApproachRoomName)
        {
            side = AttackDestinationSide.Left;
            destination = leftFinalDestination;
            return destination != null;
        }

        if (approachRoomName == InterrogationApproachRoomName)
        {
            side = AttackDestinationSide.Right;
            destination = rightFinalDestination;
            return destination != null;
        }

        return false;
    }

    private IReadOnlyDictionary<DoorInteractable, float> BuildPublicTimers()
    {
        var result = new Dictionary<DoorInteractable, float>();
        foreach (var pair in attackerDoors)
        {
            if (!doorTimers.TryGetValue(pair.Key, out float time))
                continue;

            DoorInteractable door = pair.Value;
            if (door == null)
                continue;

            if (!result.TryGetValue(door, out float currentTime) || time < currentTime)
                result[door] = time;
        }

        return result;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
