using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrisonerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    
    [Header("Prisoner Settings")] 
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private PrisonerData[] prisonerData;
    [Header("Format: [Day1_BadPrisonerCount, ...]")]
    [Tooltip("Index represents the day number.")]
    [SerializeField] private List<int> badPrisonersPerDay = new List<int>();
    
    [Header("Schedule Settings")]
    [SerializeField] private DayScheduleConfig[] masterSchedule;
    [Tooltip("Days 1 through this value are guaranteed exactly one concrete bad action. Later days have a 50% chance.")]
    [SerializeField] private int earlyGameDayThreshold = 3;
    
    [Header("Debug")]
    [SerializeField] private List<Prisoner> prisonerList = new List<Prisoner>();
    
    private Dictionary<Prisoner, PrisonerActionController> prisonerDirectory = new Dictionary<Prisoner, PrisonerActionController>();
    
    public IReadOnlyList<Prisoner> PrisonerList => prisonerList;
    public DayScheduleConfig[] MasterSchedule => masterSchedule;

    private void Awake()
    {
        if (masterSchedule == null)
        {
            Debug.LogError($"[PrisonerManager] There is no master schedule!");
            return;
        }

        if (dayManager == null)
        {
            Debug.LogError($"[PrisonerManager] There is no day manager!");
            return;
        }
        
    }

    private void OnEnable()
    {
        if (dayManager)
        {
            dayManager.OnDayStarted += HandleNewDayStart;
            dayManager.OnNightStarted += HandleNightStarted;
        }
    }

    private void OnDisable()
    {
        if (dayManager)
        {
            dayManager.OnDayStarted -= HandleNewDayStart;
            dayManager.OnNightStarted -= HandleNightStarted;
        }
    }
    

    public void InitPrisoners()
    {
        for (int i = 0; i < prisonerData.Length; i++)
        {
            SpawnPrisoner(prisonerData[i]);
        }
    }

    public void SpawnPrisoner(PrisonerData pData)
    {
        Prisoner p = new Prisoner(pData);
        Debug.Log($"Spawning prisoner {p.PrisonerID}, assigned cell: {pData.AssignedCellRoom}");
        GameObject prisonerObj = Instantiate(prisonerPrefab, transform);
        prisonerObj.GetComponentInChildren<CharacterVisual>()?.Apply(pData.Appearance);
        if (prisonerObj.GetComponent<PrisonerFootstepAudio>() == null)
        {
            prisonerObj.AddComponent<PrisonerFootstepAudio>();
        }

        PrisonerActionController prisonerController = prisonerObj.GetComponent<PrisonerActionController>();
        if (prisonerController != null)
        {
            prisonerController.Initialize(p, dayManager);
            prisonerObj.name = $"Prisoner_{p.PrisonerID}";
            
            RegisterPrisoner(p, prisonerController);
        }
        else
        {
            Debug.LogWarning("No prisoner controller found.");
        }
    }

    /// <summary>
    /// public wrapper
    /// Use to set up prisoners from a save starting in afternoon
    /// </summary>
    /// <param name="day"></param>
    public void SetupPrisonerForDay(int day)
    {
        ResetPrisoners();
        AssignBadPrisoners(day);
        AssignDailySchedule(day);
    }

    private void HandleNewDayStart()
    {
        Debug.Log($"[PrisonerManager] HandleNewDayStart — Day {dayManager.CurrentDay}," +
                  $" {prisonerList.Count} prisoners");
        ResetPrisoners();
        AssignBadPrisoners(dayManager.CurrentDay);
        AssignDailySchedule(dayManager.CurrentDay);
    }

    private void HandleNightStarted()
    {
        foreach (var pac in prisonerDirectory.Values)
        {
            if (pac == null) continue;
            if (pac.Prisoner.IsLockedUp)
                pac.Prisoner.ReleaseLockUp();
            pac.BeginReturnToCell(1.5f);
        }
    }

    private void ResetPrisoners()
    {
        // Resets bad && schedule
        for (int i = 0; i < prisonerList.Count; i++)
        {
            prisonerList[i].MakeBad(false);
            prisonerList[i].ClearEvidence();
            prisonerList[i].ClearSchedule();
        }
        
        // TODO: reset lockup?
    }
    public void AssignBadPrisoners(int currentDay)
    {
        List<Prisoner> allPrisonersCopy = new List<Prisoner>(prisonerList);

        int num = Mathf.Min(badPrisonersPerDay[currentDay-1], allPrisonersCopy.Count);
        Shuffle(allPrisonersCopy);

        for (int i = 0; i < num; i++)
            allPrisonersCopy[i].MakeBad(true);
    }

    private void AssignDailySchedule(int currentDay)
    {
        var daySchedule = masterSchedule[currentDay-1];
        if (daySchedule == null) return;

        // Separate bad vs good prisoners
        var badPrisoners = new List<Prisoner>();
        var goodPrisoners = new List<Prisoner>();
        foreach (var p in prisonerList)
        {
            if (p.IsBad) badPrisoners.Add(p);
            else goodPrisoners.Add(p);
        }

        // Decide how many bad prisoners get a concrete bad action this day
        int concreteBadCount = DetermineConcreteBadCount(currentDay);
        Shuffle(badPrisoners);

        // Mark the first concreteBadCount bad prisoners as the concrete-bad set
        var concreteBadSet = new HashSet<Prisoner>();
        for (int i = 0; i < Mathf.Min(concreteBadCount, badPrisoners.Count); i++)
            concreteBadSet.Add(badPrisoners[i]);

        for (int i = 0; i < daySchedule.scheduleBlocks.Count; i++)
        {
            var block = daySchedule.scheduleBlocks[i];
            var badPool = block.badActions != null
                ? new List<PrisonerAction>(block.badActions)
                : new List<PrisonerAction>();
            var goodPool = block.goodActions != null
                ? new List<PrisonerAction>(block.goodActions)
                : new List<PrisonerAction>();

            Shuffle(badPool);
            int badSlot = 0;

            foreach (var p in prisonerList)
            {
                if (concreteBadSet.Contains(p))
                {
                    if (badSlot < badPool.Count)
                    {
                        p.AddSchedule(new ScheduleBlock(block.startHour, block.endHour, badPool[badSlot++], true));
                    }
                    else
                    {
                        Debug.LogWarning($"[PrisonerManager] No bad action in pool for block [{block.startHour}-{block.endHour}]; " +
                                         $"concrete-bad prisoner {p.PrisonerID} falling back to good action + evidence.");
                        p.AddSchedule(new ScheduleBlock(block.startHour, block.endHour, PickRandomAction(goodPool)));
                    }
                }
                else
                {
                    p.AddSchedule(new ScheduleBlock(block.startHour, block.endHour, PickRandomAction(goodPool)));
                }
            }
        }

        foreach (var p in prisonerList)
        {
            string blocks = string.Join(", ",
                p.DailySchedule.ConvertAll(b =>
                    $"[{b.startHour}-{b.endHour}: {b.actualAction?.name ?? "null"}{(b.isConcreteBadAction ? "*" : "")}]"));
            // Debug.Log($"[PrisonerManager] {p.PrisonerID} ({(p.IsBad ? "BAD" : "good")}) schedule: {blocks}");
        }

        foreach (var p in prisonerList)
        {
            foreach (var block in p.DailySchedule)
            {
                if (block.actualAction == null)
                    Debug.LogWarning($"[PrisonerManager] {p.PrisonerID} has block " +
                                     $"[{block.startHour}-{block.endHour}] with no action assigned.");
            }
        }

        PopulateOfficialSchedule(daySchedule);

        PrisonerEvidenceManager.Instance.AssignEvidenceForDay(daySchedule, prisonerList);
    }

    private void PopulateOfficialSchedule(DayScheduleConfig daySchedule)
    {
        if (PrisonerSchedule.Instance == null)
            return;

        PrisonerSchedule.Instance.ClearSchedules();

        foreach (var p in prisonerList)
        {
            var entries = new List<PrisonerSchedule.ScheduleEntry>();
            PrisonerActionController controller = prisonerDirectory.TryGetValue(p, out var ctrl) ? ctrl : null;

            for (int i = 0; i < daySchedule.scheduleBlocks.Count; i++)
            {
                var configBlock = daySchedule.scheduleBlocks[i];
                var runtimeBlock = p.DailySchedule[i];

                // Resolve the runtime room once and cache it on the block so StartAction uses the same value
                runtimeBlock.resolvedTargetRoomName = controller != null && runtimeBlock.actualAction != null
                    ? runtimeBlock.actualAction.GetTargetRoomName(controller)
                    : runtimeBlock.actualAction?.TargetRoomName;

                // Official doc: concrete bad prisoners show a cover good action in a good room
                PrisonerAction officialAction;
                string officialRoomName;
                if (runtimeBlock.isConcreteBadAction)
                {
                    officialAction = PickRandomAction(configBlock.goodActions);
                    officialRoomName = controller != null && officialAction != null
                        ? officialAction.GetTargetRoomName(controller)
                        : officialAction?.TargetRoomName;
                }
                else
                {
                    officialAction = runtimeBlock.actualAction;
                    officialRoomName = runtimeBlock.resolvedTargetRoomName;
                }

                entries.Add(new PrisonerSchedule.ScheduleEntry
                {
                    startHour = configBlock.startHour,
                    endHour = configBlock.endHour,
                    officialAction = officialAction,
                    targetRoomName = officialRoomName
                });
            }

            PrisonerSchedule.Instance.SetSchedule(p.PrisonerID, entries);
        }

        PrisonerSchedule.Instance.LogAll();
    }

    /// <summary>
    /// Days 1–earlyGameDayThreshold: exactly 1 concrete bad action guaranteed.
    /// Later days: 50% chance of 1, otherwise 0 (all bad prisoners use high-risk + aux).
    /// </summary>
    private int DetermineConcreteBadCount(int day)
    {
        int result;
        if (day <= earlyGameDayThreshold)
        {
            result = 1;
        }
        else
        {
            float roll = Random.value;
            result = roll < 0.5f ? 1 : 0;
            Debug.Log($"[PrisonerManager] Day {day} concrete bad roll: {roll:F3} → {result}");
        }
        Debug.Log($"[PrisonerManager] Day {day} concreteBadCount={result} (threshold={earlyGameDayThreshold})");
        return result;
    }

    private static PrisonerAction PickRandomAction(IReadOnlyList<PrisonerAction> actions)
    {
        return actions != null && actions.Count > 0 ? actions[Random.Range(0, actions.Count)] : null;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    
    
    
    /// <summary>
    /// Gets a prisoner's 3D controller by their ID.
    /// </summary>
    public PrisonerActionController GetPrisonerController(Prisoner p)
    {
        if (prisonerDirectory.TryGetValue(p, out PrisonerActionController controller))
        {
            return controller;
        }
        
        Debug.LogWarning($"Could not find active prisoner: {p}");
        return null;
    }
    
    
    /// <summary>
    /// Removes a prisoner
    /// </summary>
    public void RemovePrisoner(Prisoner p)
    {
        if (prisonerDirectory.ContainsKey(p))
        {
            Destroy(prisonerDirectory[p].gameObject);
            
            prisonerDirectory.Remove(p);
        }
        
        prisonerList.Remove(p);
    }
    
    private void RegisterPrisoner(Prisoner p, PrisonerActionController pController)
    {
        prisonerList.Add(p);
        if (!prisonerDirectory.ContainsKey(p))
        {
            prisonerDirectory.Add(p, pController);
        }
        else
        {
            Debug.LogError($"{p} already exists!");
        }
    }
    
}
