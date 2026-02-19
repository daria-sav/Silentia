using UnityEngine;

public class DeathAbility : BaseAbility
{
    public override void EnterAbility()
    {
        player.gatherInput.DisablePlayerMap();
        linkedPhysics.ResetVelocity();
        linkedAnimator.SetBool("Death", true);
    }
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool("Death", linkedStateMachine.currentState == PlayerStates.State.Death);
    }

    public void ResetGame()
    {
        if (!RestartPolicy.AllowLevelRestart)
            return;

        if (player != null && player.restartLevelOnDeath == false)
            return;

        LevelManager.instance.RestartLevel();
    }
}