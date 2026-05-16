using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ForbiddenMechanismZoneAction", menuName = "Actions/Bad/Forbidden Mechanism Zone")]
public class ForbiddenMechanismZoneAction : PrisonerAction
{
    [SerializeField] private Vector3 targetLocalPosition;
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField] private float fallbackHoldSeconds = 40f;

    private readonly Dictionary<PrisonerActionController, float> nextRetargetTimes = new Dictionary<PrisonerActionController, float>();
    private readonly Dictionary<PrisonerActionController, float> fallbackHoldTimers = new Dictionary<PrisonerActionController, float>();

    public override float BadActionZoneStaySecondsOverride => fallbackHoldSeconds;

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
        fallbackHoldTimers.Remove(prisonerActionController);
        prisonerActionController.ClearMovement();
        base.EndAction(prisonerActionController);
    }

    private void HoldInZoneUntilResolved(PrisonerActionController prisonerActionController)
    {
        if (!BadActionZoneTrigger.TryGetActionTrigger(this, out BadActionZoneTrigger trigger))
        {
            HoldAtTargetForFallbackDuration(prisonerActionController);
            return;
        }

        if (trigger.IsResolved(prisonerActionController))
        {
            prisonerActionController.ClearMovement();
            nextRetargetTimes.Remove(prisonerActionController);
            fallbackHoldTimers.Remove(prisonerActionController);
            return;
        }

        if (trigger.IsInside(prisonerActionController))
        {
            prisonerActionController.ClearMovement();
            fallbackHoldTimers[prisonerActionController] = 0f;
            return;
        }

        if (prisonerActionController.HasMoveTarget && Time.time < GetNextRetargetTime(prisonerActionController))
            return;

        MoveToZoneTarget(prisonerActionController);
        nextRetargetTimes[prisonerActionController] = Time.time + retargetInterval;
    }

    private void HoldAtTargetForFallbackDuration(PrisonerActionController prisonerActionController)
    {
        if (prisonerActionController.HasMoveTarget)
            return;

        fallbackHoldTimers.TryGetValue(prisonerActionController, out float holdTimer);
        if (holdTimer < fallbackHoldSeconds)
        {
            fallbackHoldTimers[prisonerActionController] = holdTimer + Time.deltaTime;
            prisonerActionController.ClearMovement();
            return;
        }

        nextRetargetTimes.Remove(prisonerActionController);
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
