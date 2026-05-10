using System;
using UnityEngine;

public class PrisonerActionController : MonoBehaviour
{
    private DayManager _dayManager;
    private CharacterBillboardBehavior _billboard;

    private Prisoner _prisoner;
    private PrisonerAction _currentAction;
    private ScheduleBlock _currentScheduleBlock;
    
    public Prisoner Prisoner => _prisoner;
    public DayManager DayManager => _dayManager;
    public PrisonerAction CurrentAction => _currentAction;
    public ScheduleBlock CurrentScheduleBlock => _currentScheduleBlock;

    public void Initialize(Prisoner prisoner, DayManager dayManager)
    {
        if (prisoner == null || dayManager == null) return;
        
        _dayManager = dayManager;
        _prisoner = prisoner;
        _billboard = GetComponentInChildren<CharacterBillboardBehavior>();
        
        // Spawn in assigned cell
        GameObject room = RoomManager.Instance.GetRoomByName(_prisoner.PrisonerData.AssignedCellRoom);

        if (room != null)
        {
            EnterRoom(room);
        }
        
    }
    
    private void Update()
    {
        if (_dayManager == null || !_dayManager.IsDayRunning) return;

        if (_prisoner is { IsLockedUp: true }) return;
        
        UpdateSchedule();

        // Run action
        if (_currentAction != null)
        {
            _currentAction.UpdateAction(this);
        }
        
    }

    private void UpdateSchedule()
    {
        float currentHour = _dayManager.CurrentHour;
        ScheduleBlock scheduledBlock = GetBlockForCurrentTime(currentHour);
        
        if (scheduledBlock == null && _currentScheduleBlock == null && _prisoner.DailySchedule.Count > 0)
            Debug.LogWarning($"[{_prisoner.PrisonerID}] " +
                             $"No schedule block matches hour {currentHour:F1} — " +
                             $"check block hour ranges cover the full day.");

        // switch to new action
        if (scheduledBlock != null && scheduledBlock != _currentScheduleBlock)
        {
            if (_currentAction != null)
            {
                _currentAction.EndAction(this);
            }
            
            _currentScheduleBlock = scheduledBlock;
            _currentAction = scheduledBlock.actualAction;
            Debug.Log($"[{_prisoner.PrisonerID}] Action changed at hour " +
                      $"{currentHour:F1} → {_currentAction?.name ?? "null"} " +
                      $"(block {scheduledBlock.startHour}-{scheduledBlock.endHour})");
            
            if (_currentAction != null)
            {
                _currentAction.StartAction(this); 
            }
        }
        if (scheduledBlock == null && _currentAction != null)
        {
            Debug.LogWarning($"[{_prisoner.PrisonerID}] No schedule block for hour " +
                             $"{currentHour:F1} — {_prisoner.DailySchedule.Count} blocks total");
        }
        
    }
    
    /// <summary>
    /// Get the current block
    /// </summary>
    /// <param name="hour"></param>
    /// <returns></returns>
    private ScheduleBlock GetBlockForCurrentTime(float hour)
    {
        foreach (var block in _prisoner.DailySchedule)
        {
            if (hour >= block.startHour && hour < block.endHour)
            {
                return block;
            }
            
            // Edge case: Handles overnight blocks if endHour is smaller than startHour (e.g., 22.0 to 6.0)
            if (block.endHour < block.startHour)
            {
                if (hour >= block.startHour || hour < block.endHour)
                {
                    return block;
                }
            }
        }
        return null; // Free time, no scheduled block found
    }

    

    /// <summary>
    /// Physically place the character in the room gameobject at (0, 0, 0), and assign the room camera
    /// </summary>
    /// <param name="room"></param>
    public void EnterRoom(GameObject room)
    {
        EnterRoom(room, Vector3.zero);
    }

    /// <summary>
    /// Physically place the character in the room gameobject, and assign the room camera
    /// </summary>
    /// <param name="room"></param>
    /// <param name="spawnPos"></param>
    public void EnterRoom(GameObject room, Vector3 spawnPos)
    {
        this.transform.SetParent(room.transform);
        this.transform.localPosition = spawnPos;
        this.transform.localRotation = Quaternion.identity;
        
        // Try to get the camera from the room
        Camera roomCam = room.GetComponentInChildren<Camera>();
        
        if (roomCam != null && _billboard != null)
        {
            _billboard.SetCamera(roomCam);
        }
        Debug.Log($"Prisoner_{Prisoner.PrisonerID} has entered {room.name}," +
                  $" camera configured: {roomCam != null && _billboard != null}");
    }
}
