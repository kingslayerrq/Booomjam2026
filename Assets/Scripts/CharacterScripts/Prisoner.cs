using UnityEngine;

[System.Serializable]
public class Prisoner
{
    [SerializeField] private PrisonerData prisonerData;

    public Prisoner(PrisonerData data)
    {
        this.prisonerData = data;
    }
    
    public string PrisonerName => prisonerData.PrisonerName;
    public string PrisonerID => prisonerData.PrisonerID;
}
