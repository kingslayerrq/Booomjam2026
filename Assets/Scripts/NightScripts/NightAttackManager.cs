using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NightAttackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PrisonerManager prisonerManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Configuration")]
    [Tooltip("Per-night configs indexed by day number (same convention as masterSchedule).")]
    [SerializeField] private NightAttackConfig[] nightConfigs;
    [Tooltip("All attackable doors in the scene. Drag DoorInteractable GameObjects here.")]
    [SerializeField] private DoorInteractable[] doors;
    [Tooltip("Seconds the player has to close the door once an attacker arrives.")]
    [SerializeField] private float doorCountdownSeconds = 5f;

    private readonly List<NightAttackerController> activeAttackers = new List<NightAttackerController>();
    private readonly Queue<Prisoner> pendingAttackers = new Queue<Prisoner>();
    private readonly Dictionary<DoorInteractable, NightAttackerController> doorAssignments =
        new Dictionary<DoorInteractable, NightAttackerController>();
    private readonly Dictionary<NightAttackerController, float> doorTimers =
        new Dictionary<NightAttackerController, float>();

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
            dayManager.OnNightStarted += HandleNightStarted;
            dayManager.OnNightEnded += HandleNightEnded;
        }
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnNightStarted -= HandleNightStarted;
            dayManager.OnNightEnded -= HandleNightEnded;
        }
    }

    private void Update()
    {
        if (!nightActive) return;

        TickDoorTimers();
        TryLaunchNextWave();
    }

    private void HandleNightStarted()
    {
        int day = dayManager.CurrentDay;
        if (nightConfigs == null || day >= nightConfigs.Length || nightConfigs[day] == null)
        {
            Debug.Log($"[NightAttackManager] No config for day {day} — skipping night attack.");
            return;
        }

        currentConfig = nightConfigs[day];
        nightActive = true;

        SelectAndEnqueueAttackers();
        nextWaveLaunchTime = Time.time;

        Debug.Log($"[NightAttackManager] Night started — day {day}, " +
                  $"{pendingAttackers.Count} attackers queued, max simultaneous: {currentConfig.maxSimultaneous}");
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
            NightAttackRoute route = PickRouteForAvailableDoor();
            if (route == null)
            {
                Debug.LogWarning("[NightAttackManager] No available route with a free door — waiting.");
                break;
            }

            DoorInteractable targetDoor = FindDoorForRoom(route.finalRoomName);

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
            attacker.Initialize(route, pac, targetDoor != null ? targetDoor.ArrivalTransform : null);
            launched++;
        }

        if (launched > 0)
            nextWaveLaunchTime = Time.time + currentConfig.waveLaunchInterval;
    }

    private NightAttackRoute PickRouteForAvailableDoor()
    {
        if (currentConfig.routePool == null) return null;

        foreach (NightAttackRoute route in currentConfig.routePool)
        {
            if (route == null) continue;

            // Skip routes whose final door is already occupied
            DoorInteractable door = FindDoorForRoom(route.finalRoomName);
            if (door == null) continue;
            if (doorAssignments.ContainsKey(door)) continue;

            // Skip if an active attacker is already heading to this final room
            bool finalRoomTaken = false;
            foreach (var active in activeAttackers)
            {
                if (active.Route != null && active.Route.finalRoomName == route.finalRoomName)
                {
                    finalRoomTaken = true;
                    break;
                }
            }

            if (!finalRoomTaken) return route;
        }

        return null;
    }

    private void HandleArrivedAtDoor(NightAttackerController attacker)
    {
        DoorInteractable door = FindDoorForRoom(attacker.Route.finalRoomName);
        if (door == null)
        {
            Debug.LogWarning($"[NightAttackManager] No door found for room '{attacker.Route.finalRoomName}'.");
            return;
        }

        doorAssignments[door] = attacker;
        doorTimers[attacker] = doorCountdownSeconds;

        Debug.Log($"[NightAttackManager] Attacker {attacker.Route.finalRoomName} at door — " +
                  $"{doorCountdownSeconds}s countdown started.");
    }

    private void HandleReturnedToCell(NightAttackerController attacker)
    {
        activeAttackers.Remove(attacker);
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

        DoorInteractable door = null;
        foreach (var pair in doorAssignments)
        {
            if (pair.Value == attacker)
            {
                door = pair.Key;
                break;
            }
        }

        if (door != null)
            doorAssignments.Remove(door);

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
        doorAssignments.Clear();
        doorTimers.Clear();
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

    private IReadOnlyDictionary<DoorInteractable, float> BuildPublicTimers()
    {
        var result = new Dictionary<DoorInteractable, float>();
        foreach (var pair in doorAssignments)
        {
            if (doorTimers.TryGetValue(pair.Value, out float time))
                result[pair.Key] = time;
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
