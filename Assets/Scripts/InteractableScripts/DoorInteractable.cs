using System;
using DG.Tweening;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    private const float OpenY = 3f;
    private const float ClosedY = 1.25f;
    private const float AnimDuration = 0.4f;

    [RoomDropdown]
    [Tooltip("The room this door protects. For night attacks this should match NightAttackManager's surveillance room.")]
    [SerializeField] private string assignedRoomName;
    [SerializeField] private bool startOpen = true;
    [Tooltip("The mesh transform to move on open/close. If null, animates this transform.")]
    [SerializeField] private Transform doorMesh;
    [Tooltip("Legacy fallback for night attackers. NightAttackManager can override this with left/right final destination transforms.")]
    [SerializeField] private Transform arrivalTransform;
    [Tooltip("Battery drained per second while the door is closed.")]
    [SerializeField] private float closedBatteryDrainPerSecond = 5f;
    [SerializeField] private PlayerResource playerResource;

    public string AssignedRoomName => assignedRoomName;
    public bool IsOpen { get; private set; }
    public Transform ArrivalTransform => arrivalTransform;

    public event Action<DoorInteractable, bool> OnDoorStateChanged;

    private void Awake()
    {
        if (playerResource == null)
            playerResource = FindFirstObjectByType<PlayerResource>();

        IsOpen = startOpen;
        Transform visual = doorMesh != null ? doorMesh : transform;
        Vector3 pos = visual.localPosition;
        pos.y = IsOpen ? OpenY : ClosedY;
        visual.localPosition = pos;
    }

    private void Update()
    {
        if (IsOpen || playerResource == null) return;

        playerResource.ReduceBatteryLevel(closedBatteryDrainPerSecond * Time.deltaTime);

        if (playerResource.CurrentBatteryLevel <= 0f)
            SetOpen(true);
    }

    public void Interact()
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;

        IsOpen = open;
        Transform visual = doorMesh != null ? doorMesh : transform;
        visual.DOLocalMoveY(IsOpen ? OpenY : ClosedY, AnimDuration).SetEase(Ease.InOutQuad);
        GameObject room = RoomManager.Instance != null ? RoomManager.Instance.GetRoomByName(assignedRoomName) : null;
        GameAudioManager.Instance.PlayRandomDoor(room, visual.position);
        OnDoorStateChanged?.Invoke(this, IsOpen);
        Debug.Log($"[Door] {name} is now {(IsOpen ? "open" : "closed")}");
    }
}
