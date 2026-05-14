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

    public AttackerState State => _state;
    public NightAttackRoute Route => _route;

    public event Action<NightAttackerController> OnArrivedAtDoor;
    public event Action<NightAttackerController> OnReturnedToCell;

    public void Initialize(NightAttackRoute route, PrisonerActionController pac, Transform arrivalTransform)
    {
        _route = route;
        _pac = pac;
        _arrivalTransform = arrivalTransform;
        _waypointIndex = 0;
        _state = AttackerState.Idle;

        if (route == null || pac == null)
        {
            Debug.LogError("[NightAttacker] Initialize called with null route or controller.");
            return;
        }

        _pac.AllowNightMovement = true;
        Debug.Log($"[NightAttacker] {pac.Prisoner.PrisonerID} initialized on route to {route.finalRoomName}");
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
        if (_route.waypoints != null && _waypointIndex < _route.waypoints.Count)
        {
            var waypoint = _route.waypoints[_waypointIndex];
            GameObject room = RoomManager.Instance.GetRoomByName(waypoint.roomName);
            if (room != null)
            {
                _pac.EnterRoom(room);
                _pac.MoveToWorldPosition(room.transform.position, _route.moveSpeed);
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
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} waiting in waypoint for {_waitTimer}s");
    }

    private void TravelToFinalRoom()
    {
        GameObject finalRoom = RoomManager.Instance.GetRoomByName(_route.finalRoomName);
        if (finalRoom == null)
        {
            Debug.LogError($"[NightAttacker] Final room '{_route.finalRoomName}' not found.");
            return;
        }

        Vector3 arrivalWorldPos = _arrivalTransform != null
            ? _arrivalTransform.position
            : finalRoom.transform.position;

        Vector3 arrivalLocalPos = finalRoom.transform.InverseTransformPoint(arrivalWorldPos);
        _pac.EnterRoom(finalRoom, arrivalLocalPos);
        _pac.MoveToWorldPosition(arrivalWorldPos, _route.moveSpeed);

        _state = AttackerState.TravelingToDoor;
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} traveling to final room: {_route.finalRoomName}");
    }

    private void ArriveAtDoor()
    {
        _state = AttackerState.AtDoor;
        _pac.ClearMovement();
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} arrived at door in {_route.finalRoomName}");
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
        _state = AttackerState.Done;
        Debug.Log($"[NightAttacker] {_pac.Prisoner.PrisonerID} returned to cell: {cellRoom}");
        OnReturnedToCell?.Invoke(this);
    }

}
