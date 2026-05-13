using System.Collections;
using UnityEngine;

/// <summary>
/// Applies knockback and handles the transition back to movement or death.
/// </summary>
public class KnockBackAbility : BaseAbility
{
    private const string KnockBackAnimParameterName = "KnockBack";
    private int knockBackParameterID;

    private Coroutine activeKnockBack;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        knockBackParameterID = Animator.StringToHash(KnockBackAnimParameterName);
    }

    public override void ExitAbility()
    {
        activeKnockBack = null;
    }
    #endregion

    // ───────────────── API ─────────────────

    #region Public API
    public void StartKnockBack(float duration, Vector2 force, Transform enemyObject, float deathDelaySeconds, bool waitForGroundBeforeDeath, float maxWaitForGround)
    {
        if (player.playerStats == null || !player.playerStats.GetCanTakeDamage())
            return;

        if (activeKnockBack != null)
            StopCoroutine(activeKnockBack);

        activeKnockBack = StartCoroutine(ExecuteKnockBack(duration, force, enemyObject, deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround));
    }
    #endregion

    // ──────────── KNOCKBACK SEQUENCE ─────────

    #region Knockback Sequence
    private IEnumerator ExecuteKnockBack(float duration, Vector2 force, Transform enemyObject, float deathDelaySeconds, bool waitForGroundBeforeDeath, float maxWaitForGround)
    {
        linkedStateMachine.ChangeState(PlayerStates.State.KnockBack);

        if (linkedMotor == null)
        {
            Debug.LogError("[KnockBackAbility] linkedMotor is null");
            yield break;
        }

        // applies an impulse away from the enemy
        Vector2 knockVelocity = CalculateKnockbackVelocity(force, enemyObject);

        linkedMotor.ExternalImpulse(knockVelocity, duration);

        yield return new WaitForSeconds(duration);

        linkedMotor.ClearExternalLock();

        // if the player survived, return to the appropriate movement state
        if (player.playerStats != null && player.playerStats.GetCurrentHealth() > 0)
        {
            TransitionToMovementState();
            yield break;
        }

        player.gatherInput?.DisablePlayerMap();
        linkedMotor.LastPressedJumpTime = 0f;

        // if the player died, optionally wait before switching to the death state
        yield return WaitBeforeDeath(deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround);

        if (player.playerStats != null)
            player.playerStats.SetDeferDeath(false);

        linkedStateMachine.ChangeState(PlayerStates.State.Death);
    }
    #endregion

    // ──────────── KNOCKBACK HELPERS ──────────

    #region Helpers
    private Vector2 CalculateKnockbackVelocity(Vector2 force, Transform enemyObject)
    {
        float dirX = linkedMotor.RB.position.x - GetEnemyContactX(enemyObject);

        // if the enemy is at the same X position, push in the direction the player faces
        if (Mathf.Abs(dirX) < 0.001f)
            dirX = (player != null && player.facingRight) ? 1f : -1f;

        return new Vector2(Mathf.Sign(dirX) * Mathf.Abs(force.x), force.y);
    }

    private float GetEnemyContactX(Transform enemyObject)
    {
        // uses the closest collider point when available for a more accurate knockback direction
        Collider2D srcCol = enemyObject.GetComponent<Collider2D>();

        if (srcCol == null)
            srcCol = enemyObject.GetComponentInParent<Collider2D>();

        if (srcCol != null)
            return srcCol.ClosestPoint(linkedMotor.RB.position).x;

        return enemyObject.position.x;
    }

    private void TransitionToMovementState()
    {
        bool grounded = linkedMotor.LastOnGroundTime > 0;

        if (grounded)
        {
            if (Mathf.Abs(linkedInput.move.x) > 0.01f)
                linkedStateMachine.ChangeState(PlayerStates.State.Walk);
            else
                linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
        else
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
        }
    }

    private IEnumerator WaitBeforeDeath(float deathDelay, bool waitForGround, float maxWait)
    {
        if (deathDelay > 0f)
            yield return new WaitForSeconds(deathDelay);

        if (!waitForGround)
            yield break;

        float waited = 0f;
        while (waited < maxWait && linkedMotor.LastOnGroundTime <= 0f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    // ────────────── ANIMATOR ───────────────

    #region Animator
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(knockBackParameterID, linkedStateMachine.currentState == PlayerStates.State.KnockBack);
    }
    #endregion
}