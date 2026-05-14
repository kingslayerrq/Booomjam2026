using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ForbiddenMechanismZoneAction", menuName = "Actions/Bad/Forbidden Mechanism Zone")]
public class ForbiddenMechanismZoneAction : PrisonerAction
{
    [SerializeField] private Vector3 targetLocalPosition;
    [SerializeField] private float retargetInterval = 0.5f;

    private readonly Dictionary<PrisonerActionController, float> nextRetargetTimes = new Dictionary<PrisonerActionController, float>();

    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        GameObject room = RoomManager.Instance.GetRoomByName(targetRoomName);
        if (room != null)
        {
            prisonerActionController.EnterRoom(room, targetLocalPosition);
        }

        MoveToZoneTarget(prisonerActionController);
        Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} starts {ActionName}");
    }

    public override void UpdateAction(PrisonerActionController prisonerActionController)
    {
        HoldInZoneUntilResolved(prisonerActionController);
    }

    public override void EndAction(PrisonerActionController prisonerActionController)
    {
        nextRetargetTimes.Remove(prisonerActionController);
        base.EndAction(prisonerActionController);
    }

    private void HoldInZoneUntilResolved(PrisonerActionController prisonerActionController)
    {
        if (!BadActionZoneTrigger.TryGetActionTrigger(this, out BadActionZoneTrigger trigger))
            return;

        if (trigger.IsResolved(prisonerActionController))
        {
            prisonerActionController.ClearMovement();
            nextRetargetTimes.Remove(prisonerActionController);
            return;
        }

        if (trigger.IsInside(prisonerActionController))
        {
            prisonerActionController.ClearMovement();
            return;
        }

        if (prisonerActionController.HasMoveTarget && Time.time < GetNextRetargetTime(prisonerActionController))
            return;

        MoveToZoneTarget(prisonerActionController);
        nextRetargetTimes[prisonerActionController] = Time.time + retargetInterval;
    }

    private float GetNextRetargetTime(PrisonerActionController prisonerActionController)
    {
        return nextRetargetTimes.TryGetValue(prisonerActionController, out float nextRetargetTime)
            ? nextRetargetTime
            : 0f;
    }

    private void MoveToZoneTarget(PrisonerActionController prisonerActionController)
    {
        if (BadActionZoneTrigger.TryGetActionTarget(this, out Transform target))
        {
            prisonerActionController.MoveToWorldPosition(target.position, moveSpeed);
        }
    }
}
