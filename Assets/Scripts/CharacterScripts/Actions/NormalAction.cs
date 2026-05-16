using UnityEngine;
using Random = UnityEngine.Random;

public enum NormalActionRoomMode
{
    UseTargetRoom,
    UseAssignedCell,
    PickRandomFromAllowedRooms
}

[System.Serializable]
public struct NormalActionRoomOption
{
    [RoomDropdown] public string roomName;
}

[CreateAssetMenu(fileName = "NormalAction", menuName = "Actions/Normal")]
public class NormalAction : PrisonerAction
{
    [Header("Room Selection")]
    [SerializeField] private NormalActionRoomMode roomMode = NormalActionRoomMode.PickRandomFromAllowedRooms;
    [SerializeField] private NormalActionRoomOption[] allowedRooms;
    [SerializeField] private bool fallbackToAssignedCellIfRoomMissing = true;

    public override string GetTargetRoomName(PrisonerActionController controller)
    {
        return ResolveRoomName(controller);
    }

    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        string roomName = prisonerActionController.CurrentScheduleBlock?.resolvedTargetRoomName;
        if (string.IsNullOrWhiteSpace(roomName))
            roomName = ResolveRoomName(prisonerActionController);

        GameObject room = GoToRoom(roomName, prisonerActionController);
        StartWanderingInRoom(room, prisonerActionController);

        // Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} starts {ActionName} in {roomName}");
    }

    public override void UpdateAction(PrisonerActionController prisonerActionController)
    {
        
    }

    public override void EndAction(PrisonerActionController prisonerActionController)
    {
        base.EndAction(prisonerActionController);
    }

    public override bool IsComplete(PrisonerActionController prisonerActionController)
    {
        return base.IsComplete(prisonerActionController);
    }

    private string ResolveRoomName(PrisonerActionController prisonerActionController)
    {
        string resolvedRoomName = roomMode switch
        {
            NormalActionRoomMode.UseAssignedCell => GetAssignedCellRoom(prisonerActionController),
            NormalActionRoomMode.PickRandomFromAllowedRooms => PickAllowedRoomName(),
            _ => targetRoomName
        };

        if (string.IsNullOrWhiteSpace(resolvedRoomName) && fallbackToAssignedCellIfRoomMissing)
        {
            resolvedRoomName = GetAssignedCellRoom(prisonerActionController);
        }

        return resolvedRoomName;
    }

    private string PickAllowedRoomName()
    {
        if (allowedRooms == null || allowedRooms.Length == 0)
            return targetRoomName;

        int startIndex = Random.Range(0, allowedRooms.Length);
        for (int i = 0; i < allowedRooms.Length; i++)
        {
            string roomName = allowedRooms[(startIndex + i) % allowedRooms.Length].roomName;
            if (!string.IsNullOrWhiteSpace(roomName))
                return roomName;
        }

        return targetRoomName;
    }

    private static string GetAssignedCellRoom(PrisonerActionController prisonerActionController)
    {
        return prisonerActionController.Prisoner.PrisonerData.AssignedCellRoom;
    }
}
