using UnityEngine;

/// <summary>
/// Drives the WallSlide animator parameter.
/// </summary>
public class WallSlideAbility : BaseAbility
{
    private const string wallSlideAnimParameterName = "WallSlide";
    private int wallSlideParameterID;

    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        wallSlideParameterID = Animator.StringToHash(wallSlideAnimParameterName);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(wallSlideParameterID, linkedStateMachine.currentState == PlayerStates.State.WallSlide);
    }
}