using UnityEngine;

[CreateAssetMenu(fileName = "SleepAction", menuName = "Actions/Sleep")]
public class SleepAction : PrisonerAction
{
    [SerializeField] private bool useAssignedCellRoom = true;

    public override string GetTargetRoomName(PrisonerActionController controller)
    {
        return useAssignedCellRoom ? controller.Prisoner.PrisonerData.AssignedCellRoom : targetRoomName;
    }

    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        string sleepingRoomName = useAssignedCellRoom
            ? prisonerActionController.Prisoner.PrisonerData.AssignedCellRoom
            : targetRoomName;

        GoToRoom(sleepingRoomName, prisonerActionController);
        Debug.Log($"Prisoner_{prisonerActionController.Prisoner.PrisonerID} is sleeping in {sleepingRoomName}");
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
}
