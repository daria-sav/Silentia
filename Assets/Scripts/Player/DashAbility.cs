using UnityEngine;

/// <summary>
/// Handles dash requests and updates the Dash animator parameter.
/// </summary>
public class DashAbility : BaseAbility
{
    private const string dashAnimParameterName = "Dash";
    private int dashParameterID;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        dashParameterID = Animator.StringToHash(dashAnimParameterName);
    }
    #endregion

    // ───────────────── API ─────────────────

    #region Public API
    public bool TryStartDash()
    {
        if (!isPermitted) return false;
        if (linkedMotor == null) return false;

        // sends the dash request to the movement motor
        linkedMotor.PressDash();
        return true;
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(dashParameterID, linkedStateMachine.currentState == PlayerStates.State.Dash);
    }
    #endregion
}