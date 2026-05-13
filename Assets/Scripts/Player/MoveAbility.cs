using UnityEngine;

/// <summary>
/// Updates the Walk animator parameter.
/// </summary>
public class MoveAbility : BaseAbility
{
    private const string walkAnimParameterName = "Walk";
    private int walkParameterID;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        walkParameterID = Animator.StringToHash(walkAnimParameterName);
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(walkParameterID, linkedStateMachine.currentState == PlayerStates.State.Walk);
    }
    #endregion
}
