using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Prisoner Data", menuName = "Characters/Prisoner Data")]
public class PrisonerData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string prisonerName;
    [SerializeField] private string prisonerID;
    [Tooltip("The room the prisoner is assigned to")]
    [RoomDropdown]
    [SerializeField] private string assignedCellRoom;

    public string PrisonerName => prisonerName;
    public string PrisonerID => prisonerID;
    public string AssignedCellRoom => assignedCellRoom;

    private void OnValidate()
    {
        prisonerID = name.Replace("Prisoner", "").Replace(" ", "");
    }
}
