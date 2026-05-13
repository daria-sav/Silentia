using UnityEngine;

/// <summary>
/// Updates the WallSlide animator parameter.
/// </summary>
public class WallSlideAbility : BaseAbility
{
    private const string wallSlideAnimParameterName = "WallSlide";
    private int wallSlideParameterID;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        wallSlideParameterID = Animator.StringToHash(wallSlideAnimParameterName);
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(wallSlideParameterID, linkedStateMachine.currentState == PlayerStates.State.WallSlide);
    }
    #endregion
}