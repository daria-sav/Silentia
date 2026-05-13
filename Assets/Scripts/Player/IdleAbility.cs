using UnityEngine;

/// <summary>
/// Updates the Idle animator parameter.
/// </summary>
public class IdleAbility : BaseAbility
{
    private const string idleAnimParameterName = "Idle";
    private int idleParameterID;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        idleParameterID = Animator.StringToHash(idleAnimParameterName);
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(idleParameterID, linkedStateMachine.currentState == PlayerStates.State.Idle);
    }
    #endregion
}