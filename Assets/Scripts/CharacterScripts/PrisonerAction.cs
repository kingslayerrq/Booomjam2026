using System;
using UnityEngine;

public abstract class PrisonerAction : ScriptableObject
{
    [SerializeField] private string actionName;
    [Tooltip("The name of room saved in RoomManager")]
    [RoomDropdown]
    [SerializeField] protected string targetRoomName;
    
    public string ActionName => actionName;
    public string TargetRoomName => targetRoomName;

    public virtual void StartAction(PrisonerActionController prisonerActionController)
    {
        GoToRoom(targetRoomName, prisonerActionController);
        Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} starts {actionName}");
    }
    
    /// <summary>
    /// Runs logic every frame
    /// </summary>
    /// <param name="prisonerActionController"></param>
    public abstract void UpdateAction(PrisonerActionController prisonerActionController);

    public virtual void EndAction(PrisonerActionController prisonerActionController)
    {
        Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} ended {actionName}");
    }

    public virtual bool IsComplete(PrisonerActionController prisonerActionController)
    {
        if (prisonerActionController.CurrentScheduleBlock == null) return true;
        return prisonerActionController.DayManager.CurrentHour >= prisonerActionController.CurrentScheduleBlock.endHour;
    }

    protected virtual void OnValidate()
    {
        actionName = this.name.Replace(" ", "_");
    }

    protected void GoToRoom(string roomName, PrisonerActionController prisonerActionController)
    {
        var room = RoomManager.Instance.GetRoomByName(roomName);
        if (room != null)
        {
            prisonerActionController.EnterRoom(room);
        }
    }
}
