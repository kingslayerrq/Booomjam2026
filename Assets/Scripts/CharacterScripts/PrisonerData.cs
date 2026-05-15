using System;
using UnityEngine;

[Serializable]
public class CharacterAppearance
{
    public Material torso;
    public Material head;
    public Material armL;
    public Material armR;
    public Material legL;
    public Material legR;
}

[CreateAssetMenu(fileName = "New Prisoner Data", menuName = "Characters/Prisoner Data")]
public class PrisonerData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string prisonerName;
    [SerializeField] private string prisonerID;
    [Tooltip("The room the prisoner is assigned to")]
    [RoomDropdown]
    [SerializeField] private string assignedCellRoom;

    [Header("Appearance")]
    [SerializeField] private CharacterAppearance appearance;

    public string PrisonerName => prisonerName;
    public string PrisonerID => prisonerID;
    public string AssignedCellRoom => assignedCellRoom;
    public CharacterAppearance Appearance => appearance;

    private void OnValidate()
    {
        prisonerID = name.Replace("Prisoner", "").Replace(" ", "");
    }
}
