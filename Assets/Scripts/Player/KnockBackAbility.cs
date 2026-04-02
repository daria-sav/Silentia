using System.Collections;
using UnityEngine;

public class KnockBackAbility : BaseAbility
{
    private Coroutine currentKnockBack;

    public override void ExitAbility()
    {
        currentKnockBack = null;
    }

    public void StartKnockBack(float duration, Vector2 force, Transform enemyObject, float deathDelaySeconds, bool waitForGroundBeforeDeath, float maxWaitForGround)
    {
        if (player.playerStats.GetCanTakeDamage() == false)
            return;

        if (currentKnockBack == null)
        {
            currentKnockBack = StartCoroutine(KnockBack(duration, force, enemyObject, deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround));
        }
        else
        {
            // do nothing OR
            StopCoroutine(currentKnockBack);
            currentKnockBack = StartCoroutine(KnockBack(duration, force, enemyObject, deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround));
        }
    }

    public IEnumerator KnockBack(float duration, Vector2 force, Transform enemyObject, float deathDelaySeconds, bool waitForGroundBeforeDeath, float maxWaitForGround)
    {
        linkedStateMachine.ChangeState(PlayerStates.State.KnockBack);

        if (linkedMotor == null)
        {
            Debug.LogError("[KnockBackAbility] linkedMotor is null");
            yield break;
        }

        Vector2 v = force;

        float playerX = linkedMotor.RB.position.x;

        Collider2D srcCol = enemyObject.GetComponent<Collider2D>() ?? enemyObject.GetComponentInParent<Collider2D>();

        float sourceX;
        if (srcCol != null)
        {
            Vector2 closest = srcCol.ClosestPoint(linkedMotor.RB.position);
            sourceX = closest.x;
        }
        else
        {
            sourceX = enemyObject.position.x;
        }

        float dirX = playerX - sourceX;

        if (Mathf.Abs(dirX) < 0.001f)
        {
            dirX = (player != null && player.facingRight) ? 1f : -1f;
        }

        v.x = Mathf.Sign(dirX) * Mathf.Abs(force.x);

        linkedMotor.ExternalImpulse(v, duration);

        yield return new WaitForSeconds(duration);

        linkedMotor.ClearExternalLock();

        if (player.playerStats.GetCurrentHealth() > 0)
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
        else
        {
            if (deathDelaySeconds > 0f)
                yield return new WaitForSeconds(deathDelaySeconds);

            if (waitForGroundBeforeDeath)
            {
                float t = 0f;
                while (t < maxWaitForGround && linkedMotor.LastOnGroundTime <= 0f)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            player.playerStats.SetDeferDeath(false);

            linkedStateMachine.ChangeState(PlayerStates.State.Death);
            yield break;
        }
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool("KnockBack", linkedStateMachine.currentState == PlayerStates.State.KnockBack);
    }
}