using UnityEngine;

/// <summary>
/// Handles jump input and jump animation parameters.
/// </summary>
public class MultipleJumpAbility : BaseAbility
{
    private string jumpAnimParameterName = "Jump";
    private string ySpeedAnimParameterName = "ySpeed";

    private int jumpParameterID;
    private int ySpeedParameterID;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        jumpParameterID = Animator.StringToHash(jumpAnimParameterName);
        ySpeedParameterID = Animator.StringToHash(ySpeedAnimParameterName);
    }
    #endregion

    // ───────────────── API ─────────────────

    #region Public API
    public bool TryToJump()
    {
        if (!isPermitted || linkedMotor == null)
            return false;

        linkedMotor.PressJump();
        return true;
    }

    public void OnJumpReleased()
    {
        if (linkedMotor == null) 
            return;

        linkedMotor.ReleaseJump();
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(jumpParameterID, linkedStateMachine.currentState == PlayerStates.State.Jump || linkedStateMachine.currentState == PlayerStates.State.WallJump);

        if (linkedMotor != null)
            linkedAnimator.SetFloat(ySpeedParameterID, linkedMotor.RB.linearVelocity.y);
    }
    #endregion
}