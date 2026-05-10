using UnityEngine;

[CreateAssetMenu(fileName = "SleepAction", menuName = "Actions/Sleep")]
public class SleepAction : PrisonerAction
{
    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        var sleepingRoomName = prisonerActionController.Prisoner.PrisonerData.AssignedCellRoom;
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
