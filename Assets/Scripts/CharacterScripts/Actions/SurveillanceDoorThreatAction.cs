using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurveillanceDoorThreatAction", menuName = "Actions/Bad/Surveillance Door Threat")]
public class SurveillanceDoorThreatAction : PrisonerAction
{
    [SerializeField] private Vector3 targetLocalPosition;
    [SerializeField] private float threatDurationSeconds = 30f;
    [SerializeField] private bool useTargetLocalPositionAsWanderCenter = true;
    [SerializeField] private int failureDamage = 1;
    [SerializeField] private string jumpToNightReason = "Surveillance door threat was not stopped.";

    private readonly Dictionary<PrisonerActionController, float> failureTimes = new Dictionary<PrisonerActionController, float>();
    private readonly HashSet<PrisonerActionController> resolvedFailures = new HashSet<PrisonerActionController>();

    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        failureTimes[prisonerActionController] = Time.time + threatDurationSeconds;
        resolvedFailures.Remove(prisonerActionController);

        GameObject room = RoomManager.Instance.GetRoomByName(targetRoomName);
        if (room != null)
        {
            prisonerActionController.EnterRoom(room, targetLocalPosition);
            Vector3 wanderCenter = useTargetLocalPositionAsWanderCenter
                ? targetLocalPosition
                : wanderCenterLocalPosition;
            prisonerActionController.StartWandering(
                room.transform,
                wanderCenter,
                wanderRadius,
                wanderFrequency,
                moveSpeed
            );
        }

        Debug.Log($"{prisonerActionController.Prisoner.PrisonerID} starts {ActionName}");
    }

    public override void UpdateAction(PrisonerActionController prisonerActionController)
    {
        if (prisonerActionController.Prisoner is { IsLockedUp: true } || resolvedFailures.Contains(prisonerActionController))
        {
            prisonerActionController.ClearMovement();
            return;
        }

        if (!failureTimes.TryGetValue(prisonerActionController, out float failureTime))
        {
            failureTime = Time.time + threatDurationSeconds;
            failureTimes[prisonerActionController] = failureTime;
        }

        if (Time.time < failureTime)
            return;

        TriggerFailure(prisonerActionController);
    }

    public override void EndAction(PrisonerActionController prisonerActionController)
    {
        failureTimes.Remove(prisonerActionController);
        resolvedFailures.Remove(prisonerActionController);
        base.EndAction(prisonerActionController);
    }

    private void TriggerFailure(PrisonerActionController prisonerActionController)
    {
        resolvedFailures.Add(prisonerActionController);
        failureTimes.Remove(prisonerActionController);
        prisonerActionController.ClearMovement();

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.TriggerSurveillanceDoorThreatFailure(failureDamage, jumpToNightReason);
        }
        else
        {
            Debug.LogWarning("[SurveillanceDoorThreatAction] No GameManager found; cannot trigger knockout failure.");
        }
    }
}
