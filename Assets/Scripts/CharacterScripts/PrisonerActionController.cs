using System;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerActionController : MonoBehaviour
{
    private static readonly List<PrisonerActionController> ActiveControllers = new List<PrisonerActionController>();

    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool useMovementObstacleProbe;
    [SerializeField] private LayerMask movementObstacleMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float obstacleProbeSkin = 0.08f;
    [SerializeField] private int wanderTargetAttempts = 12;
    [SerializeField] private float blockedRetryDelay = 0.15f;
    [SerializeField] private float collisionRedirectCooldown = 0.25f;
    [SerializeField] private float stuckDistance = 0.01f;
    [SerializeField] private float stuckSeconds = 0.35f;

    private const float MoveArriveDistance = 0.05f;
    private const float MinMoveDistance = 0.001f;
    private const float WalkableNormalY = 0.7f;

    private DayManager _dayManager;
    private CharacterBillboardBehavior _billboard;
    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;

    private Prisoner _prisoner;
    private PrisonerAction _currentAction;
    private ScheduleBlock _currentScheduleBlock;
    private bool hasMoveTarget;
    private Vector3 moveTargetWorldPosition;
    private float moveSpeed;
    private bool isWandering;
    private Transform wanderRoot;
    private Vector3 wanderCenterLocalPosition;
    private float wanderRadius;
    private float wanderFrequency;
    private float nextWanderTime;
    private GameObject currentRoom;
    private RoomAnchorSet currentRoomAnchorSet;
    private Vector3 currentRoomEntryLocalPosition;
    private bool isReturningToCell;
    private Collider[] ownColliders = Array.Empty<Collider>();
    private readonly RaycastHit[] movementHits = new RaycastHit[12];
    private Vector3 lastMovementPosition;
    private float stuckTimer;
    private float nextCollisionRedirectTime;
    
    public Prisoner Prisoner => _prisoner;
    public DayManager DayManager => _dayManager;
    public PrisonerAction CurrentAction => _currentAction;
    public ScheduleBlock CurrentScheduleBlock => _currentScheduleBlock;
    public Transform VisualRoot => visualRoot != null ? visualRoot : transform;
    public GameObject CurrentRoom => currentRoom;
    public RoomAnchorSet CurrentRoomAnchorSet => currentRoomAnchorSet;
    public Vector3 CurrentRoomEntryLocalPosition => currentRoomEntryLocalPosition;
    public int MovementIndex => GetStableMovementIndex();
    public bool HasMoveTarget => hasMoveTarget;
    public float WanderFrequency => wanderFrequency;
    public float MoveSpeed => moveSpeed;
    public bool AllowNightMovement { get; set; }

    private static readonly Vector3 HoldingPosition = new Vector3(0f, 300f, 0f);

    public void LockUpPrisoner()
    {
        _prisoner.LockUp();
        ClearMovement();
        isReturningToCell = false;
        AllowNightMovement = false;
        transform.SetParent(null);
        transform.position = HoldingPosition;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        lastMovementPosition = transform.position;
        RefreshOwnColliders();
    }

    private void OnEnable()
    {
        RegisterCollisionIgnores();
    }

    private void OnDisable()
    {
        UnregisterCollisionIgnores();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!isWandering || Time.time < nextCollisionRedirectTime)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (ShouldIgnoreMovementCollision(contact.otherCollider, contact.normal))
                continue;

            nextCollisionRedirectTime = Time.time + collisionRedirectCooldown;
            HandleBlockedMovement(contact.normal);
            return;
        }
    }

    public void Initialize(Prisoner prisoner, DayManager dayManager)
    {
        if (prisoner == null || dayManager == null) return;
        
        _dayManager = dayManager;
        _prisoner = prisoner;
        _billboard = GetComponentInChildren<CharacterBillboardBehavior>();
        if (visualRoot == null && _billboard != null)
        {
            visualRoot = _billboard.transform;
        }
        
        // Spawn in assigned cell
        GameObject room = RoomManager.Instance.GetRoomByName(_prisoner.PrisonerData.AssignedCellRoom);

        if (room != null)
        {
            EnterRoom(room);
        }
        
    }
    
    private void Update()
    {
        if (_dayManager == null || (!_dayManager.IsDayRunning && !AllowNightMovement)) return;

        if (_prisoner is { IsLockedUp: true }) return;

        if (isReturningToCell && !hasMoveTarget)
        {
            isReturningToCell = false;
            AllowNightMovement = false;
        }

        if (_dayManager.IsDayRunning)
            UpdateSchedule();

        // Run action
        if (_currentAction != null)
        {
            _currentAction.UpdateAction(this);
        }

        PrisonerEvidenceManager.Instance?.UpdateAuxiliaryBehavior(this);

        if (_rigidbody == null)
        {
            UpdateMovement(Time.deltaTime);
        }
        
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null || _dayManager == null || (!_dayManager.IsDayRunning && !AllowNightMovement))
            return;

        if (_prisoner is { IsLockedUp: true })
            return;

        UpdateMovement(Time.fixedDeltaTime);
    }

    private void UpdateSchedule()
    {
        float currentHour = _dayManager.CurrentHour;
        ScheduleBlock scheduledBlock = GetBlockForCurrentTime(currentHour);
        
        if (scheduledBlock == null && _currentScheduleBlock == null && _prisoner.DailySchedule.Count > 0)
            Debug.LogWarning($"[{_prisoner.PrisonerID}] " +
                             $"No schedule block matches hour {currentHour:F1} — " +
                             $"check block hour ranges cover the full day.");

        // switch to new action
        if (scheduledBlock != null && scheduledBlock != _currentScheduleBlock)
        {
            if (_currentAction != null)
            {
                PrisonerEvidenceManager.Instance?.EndAuxiliaryBehavior(this);
                _currentAction.EndAction(this);
            }
            
            _currentScheduleBlock = scheduledBlock;
            _currentAction = scheduledBlock.actualAction;
            // Debug.Log($"[{_prisoner.PrisonerID}] Action changed at hour " +
            //           $"{currentHour:F1} → {_currentAction?.name ?? "null"} " +
            //           $"(block {scheduledBlock.startHour}-{scheduledBlock.endHour})");
            
            if (_currentAction != null)
            {
                _currentAction.StartAction(this); 
                PrisonerEvidenceManager.Instance?.StartAuxiliaryBehavior(this);
            }
        }
        if (scheduledBlock == null && _currentAction != null)
        {
            Debug.LogWarning($"[{_prisoner.PrisonerID}] No schedule block for hour " +
                             $"{currentHour:F1} — {_prisoner.DailySchedule.Count} blocks total");
        }
        
    }
    
    /// <summary>
    /// Get the current block
    /// </summary>
    /// <param name="hour"></param>
    /// <returns></returns>
    private ScheduleBlock GetBlockForCurrentTime(float hour)
    {
        foreach (var block in _prisoner.DailySchedule)
        {
            if (hour >= block.startHour && hour < block.endHour)
            {
                return block;
            }
            
            // Edge case: Handles overnight blocks if endHour is smaller than startHour (e.g., 22.0 to 6.0)
            if (block.endHour < block.startHour)
            {
                if (hour >= block.startHour || hour < block.endHour)
                {
                    return block;
                }
            }
        }
        return null; // Free time, no scheduled block found
    }

    

    /// <summary>
    /// Physically place the character in the room gameobject at (0, 0, 0), and assign the room camera
    /// </summary>
    /// <param name="room"></param>
    public void EnterRoom(GameObject room)
    {
        Vector3 spawnPos = Vector3.zero;
        RoomAnchorSet anchorSet = room != null ? room.GetComponent<RoomAnchorSet>() : null;
        if (anchorSet != null)
        {
            spawnPos = anchorSet.GetEntryLocalPosition(MovementIndex);
        }

        EnterRoom(room, spawnPos);
    }

    /// <summary>
    /// Physically place the character in the room gameobject, and assign the room camera
    /// </summary>
    /// <param name="room"></param>
    /// <param name="spawnPos"></param>
    public void EnterRoom(GameObject room, Vector3 spawnPos)
    {
        if (room == null)
            return;

        ClearMovement();
        currentRoom = room;
        currentRoomAnchorSet = room.GetComponent<RoomAnchorSet>();
        currentRoomEntryLocalPosition = spawnPos;

        this.transform.SetParent(room.transform);
        this.transform.localPosition = spawnPos;
        this.transform.localRotation = Quaternion.identity;
        
        // Try to get the camera from the room
        Camera roomCam = room.GetComponentInChildren<Camera>();
        
        if (roomCam != null && _billboard != null)
        {
            _billboard.SetCamera(roomCam);
        }
        // Debug.Log($"Prisoner_{Prisoner.PrisonerID} has entered {room.name}," +
                  // $" camera configured: {roomCam != null && _billboard != null}");
    }

    public void BeginReturnToCell(float speed)
    {
        string cellRoomName = _prisoner.PrisonerData.AssignedCellRoom;
        GameObject room = RoomManager.Instance.GetRoomByName(cellRoomName);
        if (room == null) return;

        EnterRoom(room);

        RoomAnchorSet anchorSet = room.GetComponent<RoomAnchorSet>();
        Vector3 targetLocalPos = anchorSet != null
            ? anchorSet.GetWanderCenterLocalPosition(MovementIndex, Vector3.zero, false)
            : Vector3.zero;
        Vector3 targetWorldPos = room.transform.TransformPoint(targetLocalPos);

        AllowNightMovement = true;
        isReturningToCell = true;
        MoveToWorldPosition(targetWorldPos, speed);
    }

    public void MoveToWorldPosition(Vector3 worldPosition, float speed)
    {
        isWandering = false;
        hasMoveTarget = true;
        moveTargetWorldPosition = worldPosition;
        moveSpeed = Mathf.Max(0f, speed);
        stuckTimer = 0f;
        lastMovementPosition = transform.position;
    }

    public void StartWandering(Transform root, Vector3 centerLocalPosition, float radius, float frequency, float speed)
    {
        if (root == null || radius <= 0f || speed <= 0f)
        {
            ClearMovement();
            return;
        }

        wanderRoot = root;
        wanderCenterLocalPosition = centerLocalPosition;
        wanderRadius = radius;
        wanderFrequency = Mathf.Max(0.1f, frequency);
        moveSpeed = speed;
        isWandering = true;
        PickNewWanderTarget();
    }

    public void ForceWanderRetarget(float nextDelay = 0f)
    {
        if (!isWandering)
            return;

        nextWanderTime = Time.time + Mathf.Max(0f, nextDelay);
        PickNewWanderTarget();
    }

    public void SetWanderFrequency(float frequency)
    {
        if (!isWandering)
            return;

        wanderFrequency = Mathf.Max(0.05f, frequency);
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void SetBillboardFollowCamera(bool follow)
    {
        _billboard?.SetFollowCameraRotation(follow);
    }

    public void FaceWorldPosition(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= MinMoveDistance * MinMoveDistance)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    public void ClearMovement()
    {
        hasMoveTarget = false;
        isWandering = false;
        wanderRoot = null;
        stuckTimer = 0f;
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void UpdateMovement(float deltaTime)
    {
        if (!hasMoveTarget)
        {
            if (isWandering && Time.time >= nextWanderTime)
            {
                PickNewWanderTarget();
            }

            return;
        }

        if (moveSpeed <= 0f)
            return;

        Vector3 currentPosition = transform.position;
        if (GetHorizontalDistance(currentPosition, moveTargetWorldPosition) <= MoveArriveDistance)
        {
            hasMoveTarget = false;
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            moveTargetWorldPosition,
            moveSpeed * deltaTime
        );
        Vector3 moveDelta = nextPosition - currentPosition;

        if (moveDelta.sqrMagnitude <= MinMoveDistance * MinMoveDistance)
            return;

        if (IsMovementBlocked(moveDelta.normalized, moveDelta.magnitude, out RaycastHit obstacleHit))
        {
            HandleBlockedMovement(obstacleHit.normal);
            return;
        }

        MoveToPosition(nextPosition);
        UpdateStuckRecovery(deltaTime);

        if (GetHorizontalDistance(transform.position, moveTargetWorldPosition) > MoveArriveDistance)
            return;

        hasMoveTarget = false;

        if (isWandering && Time.time >= nextWanderTime)
        {
            PickNewWanderTarget();
        }
    }

    private void PickNewWanderTarget()
    {
        PickNewWanderTarget(null);
    }

    private void PickNewWanderTarget(Vector3? preferredDirectionWorld)
    {
        if (wanderRoot == null)
        {
            ClearMovement();
            return;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, wanderTargetAttempts); attempt++)
        {
            Vector3 localTarget = GetWanderLocalTarget(attempt, preferredDirectionWorld);
            Vector3 worldTarget = wanderRoot.TransformPoint(localTarget);

            if (!HasClearMovementPath(worldTarget))
                continue;

            moveTargetWorldPosition = worldTarget;
            hasMoveTarget = true;
            nextWanderTime = Time.time + wanderFrequency;
            stuckTimer = 0f;
            lastMovementPosition = transform.position;
            return;
        }

        hasMoveTarget = false;
        nextWanderTime = Time.time + blockedRetryDelay;
    }

    private int GetStableMovementIndex()
    {
        if (_prisoner == null || string.IsNullOrWhiteSpace(_prisoner.PrisonerID))
            return Mathf.Abs(GetInstanceID());

        if (int.TryParse(_prisoner.PrisonerID, out int numericId))
            return Mathf.Max(0, numericId - 1);

        return Mathf.Abs(_prisoner.PrisonerID.GetHashCode());
    }

    private void RegisterCollisionIgnores()
    {
        RefreshOwnColliders();

        for (int i = 0; i < ActiveControllers.Count; i++)
        {
            PrisonerActionController other = ActiveControllers[i];
            if (other != null && other != this)
            {
                IgnoreCollisionWith(other, true);
            }
        }

        if (!ActiveControllers.Contains(this))
        {
            ActiveControllers.Add(this);
        }
    }

    private void UnregisterCollisionIgnores()
    {
        ActiveControllers.Remove(this);

        for (int i = 0; i < ActiveControllers.Count; i++)
        {
            PrisonerActionController other = ActiveControllers[i];
            if (other != null && other != this)
            {
                IgnoreCollisionWith(other, false);
            }
        }
    }

    private void IgnoreCollisionWith(PrisonerActionController other, bool ignore)
    {
        if (other == null)
            return;

        other.RefreshOwnColliders();

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider ownCollider = ownColliders[i];
            if (ownCollider == null)
                continue;

            for (int j = 0; j < other.ownColliders.Length; j++)
            {
                Collider otherCollider = other.ownColliders[j];
                if (otherCollider == null || otherCollider == ownCollider)
                    continue;

                Physics.IgnoreCollision(ownCollider, otherCollider, ignore);
            }
        }
    }

    private void RefreshOwnColliders()
    {
        ownColliders = GetComponentsInChildren<Collider>(true);
    }

    private void MoveToPosition(Vector3 nextPosition)
    {
        if (_rigidbody != null)
        {
            _rigidbody.MovePosition(nextPosition);
            return;
        }

        transform.position = nextPosition;
    }

    private void HandleBlockedMovement(Vector3 obstacleNormal)
    {
        hasMoveTarget = false;
        stuckTimer = 0f;

        if (!isWandering)
            return;

        Vector3 awayDirection = Vector3.ProjectOnPlane(obstacleNormal, Vector3.up).normalized;
        if (awayDirection.sqrMagnitude <= MinMoveDistance * MinMoveDistance)
        {
            awayDirection = (transform.position - moveTargetWorldPosition).normalized;
        }

        PickNewWanderTarget(awayDirection);
    }

    private void UpdateStuckRecovery(float deltaTime)
    {
        float movedDistance = Vector3.Distance(transform.position, lastMovementPosition);
        if (movedDistance > stuckDistance)
        {
            stuckTimer = 0f;
            lastMovementPosition = transform.position;
            return;
        }

        stuckTimer += deltaTime;
        if (stuckTimer < stuckSeconds)
            return;

        hasMoveTarget = false;
        stuckTimer = 0f;

        if (isWandering)
        {
            Vector3 awayDirection = (transform.position - moveTargetWorldPosition).normalized;
            PickNewWanderTarget(awayDirection);
        }
    }

    private Vector3 GetWanderLocalTarget(int attempt, Vector3? preferredDirectionWorld)
    {
        if (attempt == 0 && preferredDirectionWorld.HasValue)
        {
            Vector3 localDirection = wanderRoot.InverseTransformDirection(preferredDirectionWorld.Value);
            localDirection.y = 0f;

            if (localDirection.sqrMagnitude > MinMoveDistance * MinMoveDistance)
            {
                return ClampWanderLocalTarget(
                    wanderRoot.InverseTransformPoint(transform.position) + localDirection.normalized * wanderRadius * 0.7f
                );
            }
        }

        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * wanderRadius;
        return wanderCenterLocalPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
    }

    private Vector3 ClampWanderLocalTarget(Vector3 localTarget)
    {
        Vector3 offset = localTarget - wanderCenterLocalPosition;
        offset.y = 0f;

        if (offset.magnitude > wanderRadius)
        {
            offset = offset.normalized * wanderRadius;
        }

        Vector3 clampedTarget = wanderCenterLocalPosition + offset;
        clampedTarget.y = localTarget.y;
        return clampedTarget;
    }

    private bool HasClearMovementPath(Vector3 worldTarget)
    {
        Vector3 currentPosition = transform.position;
        Vector3 toTarget = worldTarget - currentPosition;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance <= MoveArriveDistance)
            return true;

        return !IsMovementBlocked(toTarget / distance, distance, out _);
    }

    private static float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool IsMovementBlocked(Vector3 direction, float distance, out RaycastHit blockingHit)
    {
        blockingHit = default;

        if (!useMovementObstacleProbe || movementObstacleMask.value == 0 || distance <= 0f)
            return false;

        GetMovementCapsule(out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);
        int hitCount = Physics.CapsuleCastNonAlloc(
            capsuleBottom,
            capsuleTop,
            capsuleRadius,
            direction,
            movementHits,
            distance + obstacleProbeSkin,
            movementObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = movementHits[i];
            if (ShouldIgnoreMovementCollision(hit.collider, hit.normal))
                continue;

            blockingHit = hit;
            return true;
        }

        return false;
    }

    private void GetMovementCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        if (_capsuleCollider == null)
        {
            Vector3 center = transform.position + Vector3.up;
            bottom = center - Vector3.up * 0.5f;
            top = center + Vector3.up * 0.5f;
            radius = 0.4f;
            return;
        }

        Vector3 scale = transform.lossyScale;
        radius = _capsuleCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(_capsuleCollider.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 centerWorld = transform.TransformPoint(_capsuleCollider.center);
        Vector3 verticalOffset = transform.up * Mathf.Max(0f, height * 0.5f - radius);

        bottom = centerWorld - verticalOffset;
        top = centerWorld + verticalOffset;
    }

    private bool ShouldIgnoreMovementCollision(Collider hitCollider, Vector3 normal)
    {
        if (normal.y >= WalkableNormalY)
            return true;

        if (hitCollider == null)
            return true;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (hitCollider == ownColliders[i])
                return true;
        }

        return hitCollider.GetComponentInParent<PrisonerActionController>() != null;
    }
}
