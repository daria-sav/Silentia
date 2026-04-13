using UnityEngine;

/// <summary>
/// Drives the Walk animator parameter.
/// </summary>
public class MoveAbility : BaseAbility
{
    private const string walkAnimParameterName = "Walk";
    private int walkParameterID;

    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        walkParameterID = Animator.StringToHash(walkAnimParameterName);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(walkParameterID, linkedStateMachine.currentState == PlayerStates.State.Walk);
    }
}
