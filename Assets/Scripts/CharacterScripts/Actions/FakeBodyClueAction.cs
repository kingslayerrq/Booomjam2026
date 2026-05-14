using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FakeBodyClueAction", menuName = "Actions/Bad/Fake Body Clue")]
public class FakeBodyClueAction : PrisonerAction
{
    [RoomDropdown]
    [SerializeField] private string fakeBodyRoomName;
    [SerializeField] private Vector3 fakeBodyLocalPosition;
    [SerializeField] private Vector3 fakeBodyLocalEulerAngles;
    [SerializeField] private Vector3 fakeBodyLocalScale = Vector3.one;
    [SerializeField] private float missedClueBatteryReductionAmount;

    private readonly Dictionary<PrisonerActionController, GameObject> activeFakeBodies = new Dictionary<PrisonerActionController, GameObject>();
    private readonly HashSet<DayManager> subscribedDayManagers = new HashSet<DayManager>();

    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        base.StartAction(prisonerActionController);
        RegisterNightResolution(prisonerActionController.DayManager);
        SpawnFakeBody(prisonerActionController);
    }

    public override void UpdateAction(PrisonerActionController prisonerActionController)
    {
    }

    private void SpawnFakeBody(PrisonerActionController prisonerActionController)
    {
        if (prisonerActionController == null || prisonerActionController.VisualRoot == null)
            return;

        string roomName = string.IsNullOrWhiteSpace(fakeBodyRoomName) ? 
            prisonerActionController.Prisoner.PrisonerData.AssignedCellRoom : fakeBodyRoomName;
        GameObject room = RoomManager.Instance.GetRoomByName(roomName);
        if (room == null)
            return;

        if (activeFakeBodies.TryGetValue(prisonerActionController, out GameObject existingFakeBody) && existingFakeBody != null)
            return;

        Transform cloneTransform = Instantiate(prisonerActionController.VisualRoot, room.transform);
        cloneTransform.name = $"FakeBody_{prisonerActionController.Prisoner.PrisonerID}";
        cloneTransform.localPosition = fakeBodyLocalPosition;
        cloneTransform.localRotation = Quaternion.Euler(fakeBodyLocalEulerAngles);
        cloneTransform.localScale = fakeBodyLocalScale;

        foreach (Collider collider in cloneTransform.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (HighlightComponent highlight in cloneTransform.GetComponentsInChildren<HighlightComponent>(true))
        {
            highlight.enabled = false;
        }

        Camera roomCamera = room.GetComponentInChildren<Camera>();
        foreach (CharacterBillboardBehavior billboard in cloneTransform.GetComponentsInChildren<CharacterBillboardBehavior>(true))
        {
            billboard.SetCamera(roomCamera);
        }

        activeFakeBodies[prisonerActionController] = cloneTransform.gameObject;
    }

    private void RegisterNightResolution(DayManager dayManager)
    {
        if (dayManager == null || subscribedDayManagers.Contains(dayManager))
            return;

        dayManager.OnNightStarted += ResolveMissedClues;
        subscribedDayManagers.Add(dayManager);
    }

    private void ResolveMissedClues()
    {
        PlayerResource playerResource = FindFirstObjectByType<PlayerResource>();
        List<PrisonerActionController> resolvedPrisoners = new List<PrisonerActionController>(activeFakeBodies.Keys);

        foreach (PrisonerActionController prisonerActionController in resolvedPrisoners)
        {
            if (prisonerActionController != null && prisonerActionController.Prisoner != null && !prisonerActionController.Prisoner.IsLockedUp)
            {
                playerResource?.ReduceBatteryLevel(missedClueBatteryReductionAmount);
                Debug.Log($"[FakeBodyClueAction] Prisoner {prisonerActionController.Prisoner.PrisonerID} was not caught before night.");
            }

            DestroyFakeBody(prisonerActionController);
        }
    }

    private void DestroyFakeBody(PrisonerActionController prisonerActionController)
    {
        if (!activeFakeBodies.TryGetValue(prisonerActionController, out GameObject fakeBody))
            return;

        if (fakeBody != null)
        {
            Destroy(fakeBody);
        }

        activeFakeBodies.Remove(prisonerActionController);
    }
}
