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
        }
    }

    private void OnEnable()
    {
        if (dayManager)
        {
            dayManager.OnMorningStarted += HandleNewDayStart;
        }
    }

    private void OnDisable()
    {
        if (dayManager)
        {
            dayManager.OnMorningStarted -= HandleNewDayStart;
        }
    }

    private void Start()
    {
        InitPrisoners();
    }

    private void InitPrisoners()
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

    private void HandleNewDayStart()
    {
        Debug.Log($"[PrisonerManager] Starting new day {dayManager.CurrentDay}");
        ResetPrisoners();
        AssignBadPrisoners(dayManager.CurrentDay);
        AssignDailySchedule(dayManager.CurrentDay);
    }

    private void ResetPrisoners()
    {
        // Resets bad
        for (int i = 0; i < prisonerList.Count; i++)
        {
            prisonerList[i].MakeBad(false);
        }
        
        // TODO: reset lockup?
    }
    public void AssignBadPrisoners(int currentDay)
    {
        List<Prisoner> allPrisonersCopy = new List<Prisoner>(prisonerList);
        
        int num = badPrisonersPerDay[currentDay];
        if (num > allPrisonersCopy.Count)
        {
            num = allPrisonersCopy.Count;
        }
        
        // Fisher Yates shuffle
        for (int i = 0; i < allPrisonersCopy.Count; i++)
        {
            int randomIndex = Random.Range(i, allPrisonersCopy.Count);
            
            (allPrisonersCopy[i], allPrisonersCopy[randomIndex]) = (allPrisonersCopy[randomIndex], allPrisonersCopy[i]);
        }
        
        for (int i = 0; i < num; i++)
        {
            allPrisonersCopy[i].MakeBad(true);
        }
    }

    private void AssignDailySchedule(int currentDay)
    {
        var currentDayMasterSchedule = masterSchedule[currentDay];
        if (currentDayMasterSchedule == null) return;

        for (int i = 0; i < currentDayMasterSchedule.scheduleBlocks.Count; i++)
        {
            var currentBlock = currentDayMasterSchedule.scheduleBlocks[i];
            var availableBadActions = new List<PrisonerAction>(currentBlock.badActions);
            var availableGoodActions = new List<PrisonerAction>(currentBlock.goodActions);

            // Shuffle
            for (int j = availableBadActions.Count - 1; j > 0; j--)
            {
                int k = Random.Range(0, j+1);
                (availableBadActions[j], availableBadActions[k]) = (availableBadActions[k], availableBadActions[j]);
            }

            int badCount = 0;
            foreach (var p in prisonerList)
            {
                if (p.IsBad)
                {
                    if (badCount >= availableBadActions.Count)
                    {
                        Debug.LogWarning($"Trying to assign bad actions, but bad prisoners exceeds the available bad actions count!");
                        continue;
                    }
                    p.AddSchedule(new ScheduleBlock(currentBlock.startHour, currentBlock.endHour,
                        availableBadActions[badCount]));
                    badCount++;
                }
                else
                {
                    p.AddSchedule(new ScheduleBlock(currentBlock.startHour, currentBlock.endHour,
                        availableGoodActions[Random.Range(0, availableGoodActions.Count)]));
                }
            }
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
