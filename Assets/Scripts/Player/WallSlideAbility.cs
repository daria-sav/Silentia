using UnityEngine;

public class WallSlideAbility : BaseAbility
{
    private string wallSlideAnimParameterName = "WallSlide";
    private int wallSlideParameterID;

    protected override void Initialization()
    {
        base.Initialization();
        wallSlideParameterID = Animator.StringToHash(wallSlideAnimParameterName);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(wallSlideParameterID, linkedStateMachine.currentState == PlayerStates.State.WallSlide);
    }
}
