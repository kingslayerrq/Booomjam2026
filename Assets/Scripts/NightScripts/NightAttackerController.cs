using System;
using UnityEngine;

public class NightAttackerController : MonoBehaviour
{
    public enum AttackerState
    {
        Idle,
        TravelingToWaypoint,
        WaitingInWaypoint,
        TravelingToDoor,
        AtDoor,
        Done
    }

    private const float ArriveDistanceThreshold = 0.15f;

    private NightAttackRoute _route;
    private PrisonerActionController _pac;

    private AttackerState _state = AttackerState.Idle;
    private int _waypointIndex;
    private float _waitTimer;
    private Transform _arrivalTransform;
    private string _finalRoomName;
    private bool _isInvisible;
    private Renderer[] _cachedRenderers;
    private bool[] _cachedRendererEnabledStates;
    private PrisonerFootstepAudio _footstepAudio;

    public AttackerState State => _state;
    public NightAttackRoute Route => _route;
    public string FinalRoomName => _finalRoomName;
    public bool IsInvisible => _isInvisible;

    public event Action<NightAttackerController> OnArrivedAtDoor;
    public event Action<NightAttackerController> OnReturnedToCell;

    public void Initialize(
        NightAttackRoute route,
        PrisonerActionController pac,
        string finalRoomName,
        Transform arrivalTransform,
        bool isInvisible)
    {
        _route = route;
        _pac = pac;
        _finalRoomName = finalRoomName;
        _arrivalTransform = arrivalTransform;
        _isInvisible = isInvisible;
        _waypointIndex = 0;
        _state = AttackerState.Idle;

        if (route == null || pac == null || string.IsNullOrWhiteSpace(finalRoomName))
        {
            Debug.LogError("[NightAttacker] Initialize called with null route/controller or empty final room.");
            return;
        }

        _pac.AllowNightMovement = true;
        _footstepAudio = _pac.GetComponent<PrisonerFootstepAudio>();
        if (_footstepAudio == null)
        {
            _footstepAudio = _pac.gameObject.AddComponent<PrisonerFootstepAudio>();
        }
        ApplyInvisibleState(_isInvisible);
        Debug.Log($"[NightAttacker] {pac.Prisoner.PrisonerID} initialized on route to {_finalRoomName}" +
                  $"{(_isInvisible ? " (invisible)" : "")}");
        AdvanceToNextWaypoint();
    }

    private void Update()
    {
        switch (_state)
        {
            case AttackerState.TravelingToWaypoint:
                if (!_pac.HasMoveTarget)
                    EnterWaypointWait();
                break;

            case AttackerState.WaitingInWaypoint:
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                    AdvanceToNextWaypoint();
                break;

            case AttackerState.TravelingToDoor:
                if (!_pac.HasMoveTarget)
                    ArriveAtDoor();
                break;
        }
    }

    private void AdvanceToNextWaypoint()
    {
        SetWaitingFootsteps(false);

        if (_route.waypoints != null && _waypointIndex < _route.waypoints.Count)
        {
            var waypoint = _route.waypoints[_waypointIndex];
            GameObject room = RoomManager.Instance.GetRoomByName(waypoint.roomName);
            if (room != null)
            {
                _pac.EnterRoom(room);
                Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} traveling to waypoint {_waypointIndex}: {waypoint.roomName}");
            }
            else
            {
                Debug.LogWarning($"[NightAttacker] Waypoint room '{waypoint.roomName}' not found — skipping.");
            }

            _waypointIndex++;
            _state = AttackerState.TravelingToWaypoint;
        }
        else
        {
            TravelToFinalRoom();
        }
    }

    private void EnterWaypointWait()
    {
        var previousWaypoint = _route.waypoints[_waypointIndex - 1];
        _waitTimer = previousWaypoint.stayDuration;
        _state = AttackerState.WaitingInWaypoint;
        SetWaitingFootsteps(_isInvisible);
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} waiting in waypoint for {_waitTimer}s");
    }

    private void TravelToFinalRoom()
    {
        SetWaitingFootsteps(false);

        GameObject finalRoom = RoomManager.Instance.GetRoomByName(_finalRoomName);
        if (finalRoom == null)
        {
            Debug.LogError($"[NightAttacker] Final room '{_finalRoomName}' not found.");
            return;
        }

        Vector3 arrivalWorldPos = _arrivalTransform != null
            ? _arrivalTransform.position
            : finalRoom.transform.position;

        _pac.EnterRoom(finalRoom);
        _pac.transform.position = arrivalWorldPos;
        _pac.transform.rotation = _arrivalTransform != null ? _arrivalTransform.rotation : _pac.transform.rotation;

        _state = AttackerState.TravelingToDoor;
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} traveling to final room: {_finalRoomName}");
        ArriveAtDoor();
    }

    private void ArriveAtDoor()
    {
        _state = AttackerState.AtDoor;
        _pac.ClearMovement();
        SetWaitingFootsteps(_isInvisible);
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} arrived at door in {_finalRoomName}");
        OnArrivedAtDoor?.Invoke(this);
    }

    public void ReturnToCell()
    {
        if (_pac == null) return;

        string cellRoom = _pac.Prisoner.PrisonerData.AssignedCellRoom;
        GameObject room = RoomManager.Instance.GetRoomByName(cellRoom);
        if (room != null)
            _pac.EnterRoom(room);

        _pac.AllowNightMovement = false;
        SetWaitingFootsteps(false);
        RestoreRenderers();
        _state = AttackerState.Done;
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} returned to cell: {cellRoom}");
        OnReturnedToCell?.Invoke(this);
    }

    private void ApplyInvisibleState(bool invisible)
    {
        if (!invisible || _pac == null)
            return;

        Transform visualRoot = _pac.VisualRoot;
        _cachedRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        _cachedRendererEnabledStates = new bool[_cachedRenderers.Length];

        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            Renderer renderer = _cachedRenderers[i];
            if (renderer == null)
                continue;

            _cachedRendererEnabledStates[i] = renderer.enabled;
            renderer.enabled = false;
        }
    }

    private void RestoreRenderers()
    {
        if (_cachedRenderers == null || _cachedRendererEnabledStates == null)
            return;

        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            Renderer renderer = _cachedRenderers[i];
            if (renderer != null && i < _cachedRendererEnabledStates.Length)
            {
                renderer.enabled = _cachedRendererEnabledStates[i];
            }
        }

        _cachedRenderers = null;
        _cachedRendererEnabledStates = null;
    }

    private void SetWaitingFootsteps(bool active)
    {
        if (_footstepAudio == null)
            return;

        _footstepAudio.SetForcedLoop(active);
    }

    private void OnDestroy()
    {
        SetWaitingFootsteps(false);
        RestoreRenderers();
    }
}
