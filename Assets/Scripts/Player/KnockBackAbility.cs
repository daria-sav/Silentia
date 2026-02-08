using System.Collections;
using UnityEngine;

public class KnockBackAbility : BaseAbility
{
    private Coroutine currentKnockBack;

    public override void ExitAbility()
    {
        currentKnockBack = null;
    }

    public void StartKnockBack(float duration, Vector2 force, Transform enemyObject)
    {
        if (player.playerStats.GetCanTakeDamage() == false)
            return;

        if (currentKnockBack == null)
        {
            currentKnockBack = StartCoroutine(KnockBack(duration, force, enemyObject));
        }
        else
        {
            // do nothing OR
            StopCoroutine(currentKnockBack);
            currentKnockBack = StartCoroutine(KnockBack(duration, force, enemyObject));
        }
    }

    public IEnumerator KnockBack (float duration, Vector2 force, Transform enemyObject)
    {
        linkedStateMachine.ChangeState(PlayerStates.State.KnockBack);
        linkedPhysics.ResetVelocity();
        if (transform.position.x >= enemyObject.transform.position.x)
        {
            linkedPhysics.rb.linearVelocity = force;
        }
        else
        {
            linkedPhysics.rb.linearVelocity = new Vector2(-force.x, force.y);
        }
        yield return new WaitForSeconds(duration);
        // return to other states
        if (player.playerStats.GetCurrentHealth() > 0)
        {
            if (linkedPhysics.isGrounded)
            {
                if (linkedInput.horizontalInput != 0)
                {
                    linkedStateMachine.ChangeState(PlayerStates.State.Walk);
                }
                else
                {
                    linkedStateMachine.ChangeState(PlayerStates.State.Idle);
                }
            }
            else
            {
                linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            }
        }
        else
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Death);
        }
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool("KnockBack", linkedStateMachine.currentState == PlayerStates.State.KnockBack);
    }
}