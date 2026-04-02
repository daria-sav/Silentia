using UnityEngine;
using UnityEngine.InputSystem;

public class DashAbility : BaseAbility
{
    private string dashAnimParameterName = "Dash";
    private int dashParameterID;

    protected override void Initialization()
    {
        base.Initialization();
        dashParameterID = Animator.StringToHash(dashAnimParameterName);
    }

    public bool TryStartDash()
    {
        if (!isPermitted) return false;
        if (linkedMotor == null) return false;

        linkedMotor.PressDash(linkedInput.move);
        return true;
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(dashParameterID, linkedStateMachine.currentState == PlayerStates.State.Dash);
    }
}
