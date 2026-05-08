using UnityEngine;

[CreateAssetMenu(fileName = "NewSabotageAction", menuName = "Actions/Sabotage")]
public class SabotageAction : PrisonerAction
{
    public override void StartAction(PrisonerActionController prisonerActionController)
    {
        base.StartAction(prisonerActionController);
    }

    public override void UpdateAction(PrisonerActionController prisonerActionController)
    {
        
    }

    public override void EndAction(PrisonerActionController prisonerActionController)
    {
        base.EndAction(prisonerActionController);
    }

    public override bool IsComplete(PrisonerActionController prisonerActionController)
    {
        return base.IsComplete(prisonerActionController);
    }
    
}
