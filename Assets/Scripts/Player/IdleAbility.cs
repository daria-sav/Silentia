using UnityEngine;

/// <summary>
/// Drives the Idle animator parameter.
/// </summary>
public class IdleAbility : BaseAbility
{
    private const string idleAnimParameterName = "Idle";
    private int idleParameterID;

    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        idleParameterID = Animator.StringToHash(idleAnimParameterName);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(idleParameterID, linkedStateMachine.currentState == PlayerStates.State.Idle);
    }
}