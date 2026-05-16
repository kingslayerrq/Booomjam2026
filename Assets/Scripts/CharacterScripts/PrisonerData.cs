using System;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Sprite prisonerHeadProfilePic;
    [TextArea(5, 10)]
    [SerializeField] private string arrestedReason;
    
    [Tooltip("The room the prisoner is assigned to")]
    [RoomDropdown]
    [SerializeField] private string assignedCellRoom;

    [Header("Appearance")]
    [SerializeField] private CharacterAppearance appearance;

    public string PrisonerName => prisonerName;
    public string PrisonerID => prisonerID;
    public Sprite PrisonerHeadProfilePic => prisonerHeadProfilePic;
    public string ArrestedReason => arrestedReason;
    public string AssignedCellRoom => assignedCellRoom;
    public CharacterAppearance Appearance => appearance;

    private void OnValidate()
    {
        prisonerID = name.Replace("Prisoner", "").Replace(" ", "");
    }
}
