using UnityEngine;

[CreateAssetMenu(fileName = "New Prisoner Data", menuName = "Characters/Prisoner Data")]
public class PrisonerData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string prisonerName;
    [SerializeField] private string prisonerID;


    public string PrisonerName => prisonerName;
    public string PrisonerID => prisonerID;
}
