using System;
using UnityEngine;

public abstract class PrisonerAction : ScriptableObject
{
    [SerializeField] private string actionName;
    [Tooltip("The name of room saved in RoomManager")]
    [RoomDropdown]
    [SerializeField] protected string targetRoomName;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 1.5f;
    [SerializeField] protected bool wanderInTargetRoom = true;
    [SerializeField] protected bool useRoomWanderAnchors = true;
    [SerializeField] protected bool useCustomWanderCenter;
    [SerializeField] protected Vector3 wanderCenterLocalPosition;
    [SerializeField] protected float wanderRadius = 1.5f;
    [SerializeField] protected float wanderFrequency = 3f;
    
    public string ActionName => actionName;
    public string TargetRoomName => targetRoomName;

    public virtual string GetTargetRoomName(PrisonerActionController controller)
    {
        return targetRoomName;
    }

    public virtual void StartAction(PrisonerActionController prisonerActionController)
    {
        GameObject room = GoToRoom(targetRoomName, prisonerActionController);
        StartWanderingInRoom(room, prisonerActionController);

        // Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} starts {actionName}");
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

    protected GameObject GoToRoom(string roomName, PrisonerActionController prisonerActionController)
    {
        var room = RoomManager.Instance.GetRoomByName(roomName);
        if (room != null)
        {
            prisonerActionController.EnterRoom(room);
        }

        return room;
    }

    protected void StartWanderingInRoom(GameObject room, PrisonerActionController prisonerActionController)
    {
        if (!wanderInTargetRoom || room == null)
            return;

        Vector3 center = wanderCenterLocalPosition;
        float radius = wanderRadius;

        if (useRoomWanderAnchors && prisonerActionController.CurrentRoomAnchorSet != null)
        {
            center = prisonerActionController.CurrentRoomAnchorSet.GetWanderCenterLocalPosition(
                prisonerActionController.MovementIndex,
                wanderCenterLocalPosition,
                useCustomWanderCenter
            );
            radius = wanderRadius > 0f ? wanderRadius : prisonerActionController.CurrentRoomAnchorSet.FallbackWanderRadius;
        }

        prisonerActionController.StartWandering(
            room.transform,
            center,
            radius,
            wanderFrequency,
            moveSpeed
        );
    }
}
