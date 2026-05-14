using System.Collections.Generic;
using UnityEngine;

public class EvidenceMovableObject : MonoBehaviour
{
    private static readonly List<EvidenceMovableObject> ActiveObjects = new List<EvidenceMovableObject>();

    [SerializeField] private Vector3 fallbackMoveLocalOffset = new Vector3(0.35f, 0f, 0.2f);

    private Vector3 originalLocalPosition;
    private bool hasMoved;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        if (!ActiveObjects.Contains(this))
        {
            ActiveObjects.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveObjects.Remove(this);
    }

    public static bool TryMoveObjectInRoom(GameObject room, Vector3 localOffset)
    {
        if (room == null)
            return false;

        for (int i = 0; i < ActiveObjects.Count; i++)
        {
            EvidenceMovableObject movableObject = ActiveObjects[i];
            if (movableObject == null || movableObject.hasMoved)
                continue;

            if (!movableObject.transform.IsChildOf(room.transform))
                continue;

            movableObject.Move(localOffset);
            return true;
        }

        return false;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < ActiveObjects.Count; i++)
        {
            ActiveObjects[i]?.ResetPosition();
        }
    }

    private void Move(Vector3 localOffset)
    {
        Vector3 offset = localOffset == Vector3.zero ? fallbackMoveLocalOffset : localOffset;
        transform.localPosition = originalLocalPosition + offset;
        hasMoved = true;
    }

    private void ResetPosition()
    {
        transform.localPosition = originalLocalPosition;
        hasMoved = false;
    }
}
