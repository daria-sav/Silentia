using UnityEngine;

public class DeathAbility : BaseAbility
{
    public override void EnterAbility()
    {
        player.gatherInput.DisablePlayerMap();

        if (linkedMotor != null)
            linkedMotor.ExternalFreeze(true);

        linkedAnimator.SetBool("Death", true);
    }

    public override void ExitAbility()
    {
        if (linkedMotor != null)
            linkedMotor.ExternalFreeze(false);

        linkedAnimator.SetBool("Death", false);
    }
    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool("Death", linkedStateMachine.currentState == PlayerStates.State.Death);
    }

    public void ResetGame()
    {
        if (player != null && player.gameObject.name.Contains("GhostRoot"))
            return;

        if (TerminalSession.Instance != null &&
        TerminalSession.Instance.State == TerminalSession.TerminalState.Recording)
        {
            TerminalSession.Instance.RequestRestartAndEnterTerminal();
            return;
        }

        LevelManager.instance.RestartLevel();
    }
}