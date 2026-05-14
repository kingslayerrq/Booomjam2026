using System.Collections.Generic;
using UnityEngine;

public enum BadActionZoneConsequence
{
    DamageOnly,
    DamageThenJumpToNight
}

public class BadActionZoneTrigger : MonoBehaviour
{
    private static readonly Dictionary<PrisonerAction, BadActionZoneTrigger> ActionTriggers = new Dictionary<PrisonerAction, BadActionZoneTrigger>();

    [SerializeField] private PrisonerAction requiredAction;
    [SerializeField] private Transform actionTargetPoint;
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float requiredStaySeconds = 30f;
    [SerializeField] private int sabotageDamage = 1;
    [SerializeField] private BadActionZoneConsequence consequence;
    [SerializeField] private string jumpToNightReason;

    private readonly Dictionary<PrisonerActionController, float> stayTimers = new Dictionary<PrisonerActionController, float>();
    private readonly HashSet<PrisonerActionController> prisonersInside = new HashSet<PrisonerActionController>();
    private readonly HashSet<PrisonerActionController> resolvedPrisoners = new HashSet<PrisonerActionController>();

    public static bool TryGetActionTarget(PrisonerAction action, out Transform target)
    {
        if (TryGetActionTrigger(action, out BadActionZoneTrigger trigger))
        {
            target = trigger.GetTargetPoint();
            return target != null;
        }

        target = null;
        return false;
    }

    public static bool TryGetActionTrigger(PrisonerAction action, out BadActionZoneTrigger trigger)
    {
        return ActionTriggers.TryGetValue(action, out trigger) && trigger != null;
    }

    public float RequiredStaySeconds => requiredStaySeconds;

    private void Awake()
    {
        if (dayManager == null)
        {
            dayManager = FindFirstObjectByType<DayManager>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        RegisterActionTarget();
    }

    private void OnDisable()
    {
        if (requiredAction != null
            && ActionTriggers.TryGetValue(requiredAction, out BadActionZoneTrigger registeredTrigger)
            && registeredTrigger == this)
        {
            ActionTriggers.Remove(requiredAction);
        }

        stayTimers.Clear();
        prisonersInside.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        PrisonerActionController prisonerActionController = other.GetComponentInParent<PrisonerActionController>();
        if (prisonerActionController != null)
        {
            prisonersInside.Add(prisonerActionController);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        PrisonerActionController prisonerActionController = other.GetComponentInParent<PrisonerActionController>();
        if (prisonerActionController == null)
            return;

        prisonersInside.Add(prisonerActionController);

        if (!CanTrack(prisonerActionController))
        {
            stayTimers.Remove(prisonerActionController);
            return;
        }

        stayTimers.TryGetValue(prisonerActionController, out float timer);
        timer += Time.deltaTime;
        stayTimers[prisonerActionController] = timer;

        if (timer >= requiredStaySeconds)
        {
            Resolve(prisonerActionController);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PrisonerActionController prisonerActionController = other.GetComponentInParent<PrisonerActionController>();
        if (prisonerActionController != null)
        {
            stayTimers.Remove(prisonerActionController);
            prisonersInside.Remove(prisonerActionController);
        }
    }

    public bool IsInside(PrisonerActionController prisonerActionController)
    {
        return prisonerActionController != null && prisonersInside.Contains(prisonerActionController);
    }

    public bool IsResolved(PrisonerActionController prisonerActionController)
    {
        return prisonerActionController != null && resolvedPrisoners.Contains(prisonerActionController);
    }

    private bool CanTrack(PrisonerActionController prisonerActionController)
    {
        return dayManager != null
               && dayManager.IsDayPhase
               && requiredAction != null
               && prisonerActionController.CurrentAction == requiredAction
               && prisonerActionController.Prisoner != null
               && !prisonerActionController.Prisoner.IsLockedUp
               && !resolvedPrisoners.Contains(prisonerActionController);
    }

    private void Resolve(PrisonerActionController prisonerActionController)
    {
        resolvedPrisoners.Add(prisonerActionController);
        stayTimers.Remove(prisonerActionController);

        playerHealth?.TakeSabotageDamage(sabotageDamage);

        if (consequence == BadActionZoneConsequence.DamageThenJumpToNight)
        {
            dayManager?.JumpToNight(jumpToNightReason);
        }
    }

    private void RegisterActionTarget()
    {
        if (requiredAction == null)
            return;

        ActionTriggers[requiredAction] = this;
    }

    private Transform GetTargetPoint()
    {
        return actionTargetPoint != null ? actionTargetPoint : transform;
    }
}
