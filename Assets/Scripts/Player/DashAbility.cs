using UnityEngine;

/// <summary>
/// Bridges dash requests to the movement motor
/// and drives the Dash animator parameter.
/// </summary>
public class DashAbility : BaseAbility
{
    private const string dashAnimParameterName = "Dash";
    private int dashParameterID;

    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        dashParameterID = Animator.StringToHash(dashAnimParameterName);
    }

    // requests dash from the movement motor
    public bool TryStartDash()
    {
        if (!isPermitted) return false;
        if (linkedMotor == null) return false;

        linkedMotor.PressDash();
        return true;
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(dashParameterID, linkedStateMachine.currentState == PlayerStates.State.Dash);
    }
}