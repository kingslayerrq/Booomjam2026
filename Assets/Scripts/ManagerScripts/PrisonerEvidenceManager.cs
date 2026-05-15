using System.Collections.Generic;
using UnityEngine;

public class PrisonerEvidenceManager : MonoBehaviour
{
    private static readonly HighRiskEvidenceType[] FallbackHighRiskPool =
    {
        HighRiskEvidenceType.CameraFlicker,
        HighRiskEvidenceType.StrangeSound,
        HighRiskEvidenceType.SpiritOrb
    };

    private static readonly AuxiliaryEvidenceType[] FallbackAuxiliaryPool =
    {
        AuxiliaryEvidenceType.OutOfScheduleRoom,
        AuxiliaryEvidenceType.StareAtCamera,
        AuxiliaryEvidenceType.ConstantMovement,
        AuxiliaryEvidenceType.AbnormalBatteryDrain,
        AuxiliaryEvidenceType.FeatureMismatch,
        AuxiliaryEvidenceType.ObjectMoved
    };

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private static PrisonerEvidenceManager instance;

    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PrisonerManager prisonerManager;
    [SerializeField] private PlayerResource playerResource;

    [Header("Fallback Evidence Definitions")]
    [SerializeField] private HighRiskEvidenceDefinition[] fallbackHighRiskDefinitions;
    [SerializeField] private AuxiliaryEvidenceDefinition[] fallbackAuxiliaryDefinitions;

    [Header("Fallback Tuning")]
    [SerializeField] private float defaultHighRiskObserveSeconds = 3f;
    [SerializeField] private float defaultAbnormalBatteryDrainPerSecond = 0.02f;

    [Header("Lockup Consequences")]
    [SerializeField] private float batteryReductionPerWrongLockup = 1f;

    private readonly Dictionary<HighRiskEvidenceType, HighRiskEvidenceDefinition> highRiskDefinitions =
        new Dictionary<HighRiskEvidenceType, HighRiskEvidenceDefinition>();
    private readonly Dictionary<AuxiliaryEvidenceType, AuxiliaryEvidenceDefinition> auxiliaryDefinitions =
        new Dictionary<AuxiliaryEvidenceType, AuxiliaryEvidenceDefinition>();
    private readonly Dictionary<PrisonerActionController, AuxiliaryEvidenceType> activeAuxiliaries =
        new Dictionary<PrisonerActionController, AuxiliaryEvidenceType>();
    private readonly Dictionary<PrisonerActionController, float> nextConstantMovementRetargetTimes =
        new Dictionary<PrisonerActionController, float>();
    private readonly Dictionary<PrisonerActionController, float> savedWanderSpeeds =
        new Dictionary<PrisonerActionController, float>();
    private readonly Dictionary<PrisonerActionController, Renderer[]> featureMismatchRenderers =
        new Dictionary<PrisonerActionController, Renderer[]>();
    private readonly HashSet<PrisonerActionController> objectMovedControllers = new HashSet<PrisonerActionController>();
    private readonly HashSet<Prisoner> wronglyLockedUpPrisoners = new HashSet<Prisoner>();

    private MaterialPropertyBlock featureMismatchBlock;
    private DayScheduleConfig currentScheduleConfig;
    private float nightFeedInterferenceIntensity;

    public static PrisonerEvidenceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PrisonerEvidenceManager>();
            }

            if (instance == null)
            {
                GameObject evidenceManagerObject = new GameObject("PrisonerEvidenceManager");
                instance = evidenceManagerObject.AddComponent<PrisonerEvidenceManager>();
            }

            return instance;
        }
    }

    public float NightFeedInterferenceIntensity => nightFeedInterferenceIntensity;
    public float NightAudioPromptMultiplier { get; private set; } = 1f;
    public float DefaultHighRiskObserveSeconds => defaultHighRiskObserveSeconds;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveReferences();
        RebuildDefinitionLookups(null);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (dayManager != null)
        {
            dayManager.OnDayStarted += ResetNightModifiers;
            dayManager.OnNightStarted += ResolveUncaughtHighRiskEvidence;
        }
    }

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayStarted -= ResetNightModifiers;
            dayManager.OnNightStarted -= ResolveUncaughtHighRiskEvidence;
        }
    }

    public void AssignEvidenceForDay(DayScheduleConfig scheduleConfig, IReadOnlyList<Prisoner> prisoners)
    {
        currentScheduleConfig = scheduleConfig;
        RebuildDefinitionLookups(scheduleConfig);
        ResetNightModifiers();
        EvidenceMovableObject.ResetAll();
        activeAuxiliaries.Clear();
        nextConstantMovementRetargetTimes.Clear();
        savedWanderSpeeds.Clear();
        objectMovedControllers.Clear();
        wronglyLockedUpPrisoners.Clear();

        if (prisoners == null)
            return;

        for (int i = 0; i < prisoners.Count; i++)
        {
            Prisoner prisoner = prisoners[i];
            if (prisoner == null)
                continue;

            prisoner.ClearEvidence();

            if (prisoner.IsBad)
            {
                AssignBadPrisonerEvidence(prisoner);
            }
            else
            {
                AssignGoodPrisonerAuxiliaryEvidence(prisoner);
            }
        }

        LogEvidenceAssignments(prisoners);
    }

    private void LogEvidenceAssignments(IReadOnlyList<Prisoner> prisoners)
    {
        for (int i = 0; i < prisoners.Count; i++)
        {
            Prisoner prisoner = prisoners[i];
            if (prisoner == null) continue;

            string highRisk = prisoner.HasHighRiskEvidence
                ? prisoner.HighRiskEvidenceType.ToString()
                : "none";

            var auxParts = new System.Collections.Generic.List<string>();
            foreach (var block in prisoner.DailySchedule)
            {
                if (block.HasAuxiliaryEvidence)
                    auxParts.Add($"[{block.startHour}-{block.endHour}: {block.auxiliaryEvidenceType}]");
            }

            string aux = auxParts.Count > 0 ? string.Join(", ", auxParts) : "none";
            Debug.Log($"[Evidence] {prisoner.PrisonerID} ({(prisoner.IsBad ? "BAD" : "good")}) " +
                      $"| HighRisk: {highRisk} | Aux: {aux}");
        }
    }

    public void StartAuxiliaryBehavior(PrisonerActionController controller)
    {
        if (!CanRunAuxiliary(controller, out ScheduleBlock block))
            return;

        AuxiliaryEvidenceType evidenceType = block.auxiliaryEvidenceType;
        activeAuxiliaries[controller] = evidenceType;
        AuxiliaryEvidenceDefinition definition = ResolveAuxiliaryDefinition(block);

        switch (evidenceType)
        {
            case AuxiliaryEvidenceType.OutOfScheduleRoom:
                MoveToOutOfScheduleRoom(controller);
                break;
            case AuxiliaryEvidenceType.StareAtCamera:
                controller.SetBillboardFollowCamera(true);
                break;
            case AuxiliaryEvidenceType.ConstantMovement:
                savedWanderSpeeds[controller] = controller.MoveSpeed;
                controller.SetMoveSpeed(controller.MoveSpeed * GetConstantMovementSpeedMultiplier(definition));
                controller.SetWanderFrequency(GetConstantMovementRetargetSeconds(definition));
                controller.ForceWanderRetarget();
                break;
            case AuxiliaryEvidenceType.FeatureMismatch:
                ApplyFeatureMismatch(controller, definition);
                break;
            case AuxiliaryEvidenceType.ObjectMoved:
                TryMoveObjectInCurrentRoom(controller, definition);
                break;
        }
    }

    public void UpdateAuxiliaryBehavior(PrisonerActionController controller)
    {
        if (!CanRunAuxiliary(controller, out ScheduleBlock block))
            return;

        AuxiliaryEvidenceDefinition definition = ResolveAuxiliaryDefinition(block);

        switch (block.auxiliaryEvidenceType)
        {
            case AuxiliaryEvidenceType.ConstantMovement:
                UpdateConstantMovement(controller, definition);
                break;
            case AuxiliaryEvidenceType.ObjectMoved:
                TryMoveObjectInCurrentRoom(controller, definition);
                break;
        }
    }

    public void EndAuxiliaryBehavior(PrisonerActionController controller)
    {
        if (controller == null)
            return;

        ClearFeatureMismatch(controller);
        RestoreWanderSpeed(controller);
        activeAuxiliaries.Remove(controller);
        nextConstantMovementRetargetTimes.Remove(controller);
        savedWanderSpeeds.Remove(controller);
        objectMovedControllers.Remove(controller);
    }

    public void HandlePrisonerLockedUp(PrisonerActionController controller)
    {
        EndAuxiliaryBehavior(controller);

        if (controller?.Prisoner is { IsBad: false } prisoner)
        {
            wronglyLockedUpPrisoners.Add(prisoner);
            Debug.Log($"[PrisonerEvidenceManager] {prisoner.PrisonerID} is WRONGLY LOCKED!");
        }
    }

    public bool TryGetVisibleHighRiskEvidence(
        GameObject activeRoom,
        out PrisonerActionController controller,
        out HighRiskEvidenceDefinition definition)
    {
        controller = null;
        definition = null;

        ResolveReferences();
        if (activeRoom == null || prisonerManager == null || dayManager == null || !dayManager.IsDayPhase)
            return false;

        IReadOnlyList<Prisoner> prisoners = prisonerManager.PrisonerList;
        for (int i = 0; i < prisoners.Count; i++)
        {
            Prisoner prisoner = prisoners[i];
            if (prisoner == null || !prisoner.HasHighRiskEvidence || prisoner.IsLockedUp)
                continue;

            PrisonerActionController prisonerController = prisonerManager.GetPrisonerController(prisoner);
            if (prisonerController == null || !IsControllerInRoom(prisonerController, activeRoom))
                continue;

            controller = prisonerController;
            definition = ResolveHighRiskDefinition(prisoner);
            return definition != null;
        }

        return false;
    }

    /// <summary>
    /// Returns the total extra battery drain rate (per second) from AbnormalBatteryDrain prisoners in the given room.
    /// Call this from SurveillanceCamController while the camera is controlled.
    /// </summary>
    public float GetAuxBatteryDrainRate(GameObject room)
    {
        if (room == null)
            return 0f;

        float total = 0f;
        foreach (KeyValuePair<PrisonerActionController, AuxiliaryEvidenceType> pair in activeAuxiliaries)
        {
            PrisonerActionController controller = pair.Key;
            if (controller == null
                || pair.Value != AuxiliaryEvidenceType.AbnormalBatteryDrain
                || controller.Prisoner == null
                || controller.Prisoner.IsLockedUp
                || !IsControllerInRoom(controller, room))
                continue;

            AuxiliaryEvidenceDefinition definition = ResolveAuxiliaryDefinition(controller.CurrentScheduleBlock);
            total += GetAbnormalBatteryDrainPerSecond(definition);
        }

        return total;
    }

    private void AssignBadPrisonerEvidence(Prisoner prisoner)
    {
        if (HasConcreteBadScheduleAction(prisoner))
            return;

        prisoner.SetHighRiskEvidence(PickHighRiskDefinition());

        int blockCount = prisoner.DailySchedule.Count;
        if (blockCount <= 0)
            return;

        int maxAuxiliary = Mathf.Min(GetBadAuxiliaryMaxPerDay(), blockCount);
        int minAuxiliary = Mathf.Min(GetBadAuxiliaryMinPerDay(), maxAuxiliary);
        int auxiliaryCount = Random.Range(minAuxiliary, maxAuxiliary + 1);

        List<int> blockIndexes = new List<int>();
        for (int i = 0; i < blockCount; i++)
        {
            blockIndexes.Add(i);
        }

        Shuffle(blockIndexes);
        for (int i = 0; i < auxiliaryCount; i++)
        {
            prisoner.DailySchedule[blockIndexes[i]].SetAuxiliaryEvidence(PickAuxiliaryDefinition());
        }
    }

    private static bool HasConcreteBadScheduleAction(Prisoner prisoner)
    {
        if (prisoner == null)
            return false;

        for (int i = 0; i < prisoner.DailySchedule.Count; i++)
        {
            if (prisoner.DailySchedule[i].isConcreteBadAction)
                return true;
        }

        return false;
    }

    private void AssignGoodPrisonerAuxiliaryEvidence(Prisoner prisoner)
    {
        float chance = currentScheduleConfig != null ? currentScheduleConfig.goodAuxiliaryChance : 0.2f;
        for (int i = 0; i < prisoner.DailySchedule.Count; i++)
        {
            if (Random.value <= chance)
            {
                prisoner.DailySchedule[i].SetAuxiliaryEvidence(PickAuxiliaryDefinition());
            }
        }
    }

    private void ResolveUncaughtHighRiskEvidence()
    {
        ResolveReferences();
        ResetNightModifiers();
        ResolveWrongLockupConsequences();

        if (prisonerManager == null)
            return;

        IReadOnlyList<Prisoner> prisoners = prisonerManager.PrisonerList;
        for (int i = 0; i < prisoners.Count; i++)
        {
            Prisoner prisoner = prisoners[i];
            if (prisoner == null || !prisoner.HasHighRiskEvidence || prisoner.IsLockedUp)
                continue;

            HighRiskEvidenceDefinition definition = ResolveHighRiskDefinition(prisoner);
            if (definition == null)
                continue;

            switch (prisoner.HighRiskEvidenceType)
            {
                case HighRiskEvidenceType.CameraFlicker:
                    nightFeedInterferenceIntensity += definition.NightFeedInterferenceIntensity;
                    break;
                case HighRiskEvidenceType.StrangeSound:
                    NightAudioPromptMultiplier = Mathf.Max(
                        0.1f,
                        NightAudioPromptMultiplier - definition.NightAudioPromptMultiplierPenalty
                    );
                    break;
                case HighRiskEvidenceType.SpiritOrb:
                    playerResource?.AddTemporaryMaxEnergyPenalty(definition.EnergyMaxReduction);
                    break;
            }
        }
    }

    private void ResetNightModifiers()
    {
        nightFeedInterferenceIntensity = 0f;
        NightAudioPromptMultiplier = 1f;
        playerResource?.ClearTemporaryMaxEnergyPenalty();
    }

    private void ResolveWrongLockupConsequences()
    {
        if (playerResource == null || wronglyLockedUpPrisoners.Count == 0)
            return;

        float totalReduction = batteryReductionPerWrongLockup * wronglyLockedUpPrisoners.Count;
        playerResource.ReduceBatteryLevel(totalReduction);
        Debug.Log($"[Evidence] Wrong lockup penalty: {wronglyLockedUpPrisoners.Count} good prisoner(s), " +
                  $"-{totalReduction} battery.");
        wronglyLockedUpPrisoners.Clear();
    }

    private bool CanRunAuxiliary(PrisonerActionController controller, out ScheduleBlock block)
    {
        block = controller != null ? controller.CurrentScheduleBlock : null;
        return controller != null
               && controller.Prisoner != null
               && !controller.Prisoner.IsLockedUp
               && block != null
               && block.HasAuxiliaryEvidence;
    }

    private void MoveToOutOfScheduleRoom(PrisonerActionController controller)
    {
        if (RoomManager.Instance == null)
            return;

        string scheduledRoomName = controller.CurrentAction != null
            ? controller.CurrentAction.GetTargetRoomName(controller)
            : null;

        List<GameObject> candidates = new List<GameObject>();
        foreach (KeyValuePair<string, GameObject> pair in RoomManager.Instance.RoomNameToObject)
        {
            if (pair.Value != null && pair.Key != scheduledRoomName)
                candidates.Add(pair.Value);
        }

        if (candidates.Count > 0)
            controller.EnterRoom(candidates[Random.Range(0, candidates.Count)]);
    }

    private void RestoreWanderSpeed(PrisonerActionController controller)
    {
        if (controller != null && savedWanderSpeeds.TryGetValue(controller, out float saved))
            controller.SetMoveSpeed(saved);
    }

    private void UpdateConstantMovement(PrisonerActionController controller, AuxiliaryEvidenceDefinition definition)
    {
        float retargetSeconds = GetConstantMovementRetargetSeconds(definition);
        controller.SetWanderFrequency(retargetSeconds);

        if (!nextConstantMovementRetargetTimes.TryGetValue(controller, out float nextRetargetTime)
            || Time.time >= nextRetargetTime
            || !controller.HasMoveTarget)
        {
            controller.ForceWanderRetarget();
            nextConstantMovementRetargetTimes[controller] = Time.time + retargetSeconds;
        }
    }

    private void ApplyFeatureMismatch(PrisonerActionController controller, AuxiliaryEvidenceDefinition definition)
    {
        if (featureMismatchRenderers.ContainsKey(controller))
            return;

        Renderer[] renderers = controller.VisualRoot.GetComponentsInChildren<Renderer>(true);
        featureMismatchRenderers[controller] = renderers;

        if (featureMismatchBlock == null)
        {
            featureMismatchBlock = new MaterialPropertyBlock();
        }

        Color mismatchColor = definition != null ? definition.FeatureMismatchColor : Color.red;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(featureMismatchBlock);
            featureMismatchBlock.SetColor(BaseColorProperty, mismatchColor);
            featureMismatchBlock.SetColor(ColorProperty, mismatchColor);
            renderer.SetPropertyBlock(featureMismatchBlock);
        }
    }

    private void ClearFeatureMismatch(PrisonerActionController controller)
    {
        if (!featureMismatchRenderers.TryGetValue(controller, out Renderer[] renderers))
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i]?.SetPropertyBlock(null);
        }

        featureMismatchRenderers.Remove(controller);
    }

    private void TryMoveObjectInCurrentRoom(PrisonerActionController controller, AuxiliaryEvidenceDefinition definition)
    {
        if (!objectMovedControllers.Add(controller))
            return;

        Vector3 offset = definition != null ? definition.ObjectMoveLocalOffset : Vector3.zero;
        EvidenceMovableObject.TryMoveObjectInRoom(controller.CurrentRoom, offset);
    }

    private static bool IsControllerInRoom(PrisonerActionController controller, GameObject room)
    {
        return controller != null
               && room != null
               && (controller.transform == room.transform || controller.transform.IsChildOf(room.transform));
    }

    private HighRiskEvidenceDefinition PickHighRiskDefinition()
    {
        List<HighRiskEvidenceDefinition> pool = GetHighRiskDefinitionPool();
        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        HighRiskEvidenceType evidenceType = FallbackHighRiskPool[Random.Range(0, FallbackHighRiskPool.Length)];
        return ResolveHighRiskDefinition(evidenceType);
    }

    private AuxiliaryEvidenceDefinition PickAuxiliaryDefinition()
    {
        List<AuxiliaryEvidenceDefinition> pool = GetAuxiliaryDefinitionPool();
        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        AuxiliaryEvidenceType evidenceType = FallbackAuxiliaryPool[Random.Range(0, FallbackAuxiliaryPool.Length)];
        return ResolveAuxiliaryDefinition(evidenceType);
    }

    private HighRiskEvidenceDefinition ResolveHighRiskDefinition(Prisoner prisoner)
    {
        if (prisoner == null || !prisoner.HasHighRiskEvidence)
            return null;

        if (prisoner.HighRiskEvidenceDefinition != null)
            return prisoner.HighRiskEvidenceDefinition;

        return ResolveHighRiskDefinition(prisoner.HighRiskEvidenceType);
    }

    private HighRiskEvidenceDefinition ResolveHighRiskDefinition(HighRiskEvidenceType evidenceType)
    {
        if (evidenceType == HighRiskEvidenceType.None)
            return null;

        if (highRiskDefinitions.TryGetValue(evidenceType, out HighRiskEvidenceDefinition definition) && definition != null)
            return definition;

        definition = HighRiskEvidenceDefinition.CreateRuntimeFallback(evidenceType);
        highRiskDefinitions[evidenceType] = definition;
        return definition;
    }

    private AuxiliaryEvidenceDefinition ResolveAuxiliaryDefinition(ScheduleBlock block)
    {
        if (block == null || !block.HasAuxiliaryEvidence)
            return null;

        if (block.auxiliaryEvidenceDefinition != null)
            return block.auxiliaryEvidenceDefinition;

        return ResolveAuxiliaryDefinition(block.auxiliaryEvidenceType);
    }

    private AuxiliaryEvidenceDefinition ResolveAuxiliaryDefinition(AuxiliaryEvidenceType evidenceType)
    {
        if (evidenceType == AuxiliaryEvidenceType.None)
            return null;

        if (auxiliaryDefinitions.TryGetValue(evidenceType, out AuxiliaryEvidenceDefinition definition) && definition != null)
            return definition;

        definition = AuxiliaryEvidenceDefinition.CreateRuntimeFallback(evidenceType);
        auxiliaryDefinitions[evidenceType] = definition;
        return definition;
    }

    private List<HighRiskEvidenceDefinition> GetHighRiskDefinitionPool()
    {
        List<HighRiskEvidenceDefinition> pool = new List<HighRiskEvidenceDefinition>();
        AddHighRiskDefinitions(pool, currentScheduleConfig != null ? currentScheduleConfig.highRiskEvidencePool : null);
        AddHighRiskDefinitions(pool, fallbackHighRiskDefinitions);
        return pool;
    }

    private List<AuxiliaryEvidenceDefinition> GetAuxiliaryDefinitionPool()
    {
        List<AuxiliaryEvidenceDefinition> pool = new List<AuxiliaryEvidenceDefinition>();
        AddAuxiliaryDefinitions(pool, currentScheduleConfig != null ? currentScheduleConfig.auxiliaryEvidencePool : null);
        AddAuxiliaryDefinitions(pool, fallbackAuxiliaryDefinitions);
        return pool;
    }

    private void RebuildDefinitionLookups(DayScheduleConfig scheduleConfig)
    {
        highRiskDefinitions.Clear();
        auxiliaryDefinitions.Clear();
        AddHighRiskDefinitionsToLookup(scheduleConfig != null ? scheduleConfig.highRiskEvidencePool : null);
        AddHighRiskDefinitionsToLookup(fallbackHighRiskDefinitions);
        AddAuxiliaryDefinitionsToLookup(scheduleConfig != null ? scheduleConfig.auxiliaryEvidencePool : null);
        AddAuxiliaryDefinitionsToLookup(fallbackAuxiliaryDefinitions);
    }

    private void AddHighRiskDefinitionsToLookup(IEnumerable<HighRiskEvidenceDefinition> definitions)
    {
        if (definitions == null)
            return;

        foreach (HighRiskEvidenceDefinition definition in definitions)
        {
            if (definition != null && definition.EvidenceType != HighRiskEvidenceType.None)
            {
                highRiskDefinitions[definition.EvidenceType] = definition;
            }
        }
    }

    private void AddAuxiliaryDefinitionsToLookup(IEnumerable<AuxiliaryEvidenceDefinition> definitions)
    {
        if (definitions == null)
            return;

        foreach (AuxiliaryEvidenceDefinition definition in definitions)
        {
            if (definition != null && definition.EvidenceType != AuxiliaryEvidenceType.None)
            {
                auxiliaryDefinitions[definition.EvidenceType] = definition;
            }
        }
    }

    private static void AddHighRiskDefinitions(List<HighRiskEvidenceDefinition> pool, IEnumerable<HighRiskEvidenceDefinition> definitions)
    {
        if (definitions == null)
            return;

        foreach (HighRiskEvidenceDefinition definition in definitions)
        {
            if (definition != null && definition.EvidenceType != HighRiskEvidenceType.None)
            {
                pool.Add(definition);
            }
        }
    }

    private static void AddAuxiliaryDefinitions(List<AuxiliaryEvidenceDefinition> pool, IEnumerable<AuxiliaryEvidenceDefinition> definitions)
    {
        if (definitions == null)
            return;

        foreach (AuxiliaryEvidenceDefinition definition in definitions)
        {
            if (definition != null && definition.EvidenceType != AuxiliaryEvidenceType.None)
            {
                pool.Add(definition);
            }
        }
    }

    private int GetBadAuxiliaryMinPerDay()
    {
        return currentScheduleConfig != null ? currentScheduleConfig.badAuxiliaryMinPerDay : 1;
    }

    private int GetBadAuxiliaryMaxPerDay()
    {
        return currentScheduleConfig != null ? currentScheduleConfig.badAuxiliaryMaxPerDay : 4;
    }

    private float GetConstantMovementRetargetSeconds(AuxiliaryEvidenceDefinition definition)
    {
        return definition != null ? definition.ConstantMovementRetargetSeconds : 0.35f;
    }

    private float GetConstantMovementSpeedMultiplier(AuxiliaryEvidenceDefinition definition)
    {
        return definition != null ? definition.ConstantMovementSpeedMultiplier : 2f;
    }

    private float GetAbnormalBatteryDrainPerSecond(AuxiliaryEvidenceDefinition definition)
    {
        return definition != null ? definition.AbnormalBatteryDrainPerSecond : defaultAbnormalBatteryDrainPerSecond;
    }

    private void ResolveReferences()
    {
        if (dayManager == null)
        {
            dayManager = FindFirstObjectByType<DayManager>();
        }

        if (prisonerManager == null)
        {
            prisonerManager = FindFirstObjectByType<PrisonerManager>();
        }

        if (playerResource == null)
        {
            playerResource = FindFirstObjectByType<PlayerResource>();
        }
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (values[i], values[randomIndex]) = (values[randomIndex], values[i]);
        }
    }
}
