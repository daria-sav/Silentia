using UnityEngine;

/// <summary>
/// Handles player death logic
/// and drives the Death animator parameter.
/// </summary>
public class DeathAbility : BaseAbility
{
    private const string DeathAnimParameterName = "Death";
    private int deathParameterID;

    protected override void InitializeLinks()
    {
        base.InitializeLinks();
        deathParameterID = Animator.StringToHash(DeathAnimParameterName);
    }

    public override void EnterAbility()
    {
        Debug.Log($"[DEATH] EnterAbility on {gameObject.name}. CurrentProfileIndex={GetComponent<CloneSwitcher>()?.CurrentProfileIndex}");
        if (player != null && player.gatherInput != null)
            player.gatherInput.DisablePlayerMap();

        if (linkedMotor != null)
            linkedMotor.ExternalFreeze(true);

        if (linkedAnimator != null)
            linkedAnimator.SetBool(deathParameterID, true);

        if (GetComponent<CloneSwitcher>()?.CurrentProfileIndex == 0)
        {
            SpawnMode.spawnFromCheckPoint = true;
        }
    }

    public override void ExitAbility()
    {
        if (linkedMotor != null)
            linkedMotor.ExternalFreeze(false);

        if (linkedAnimator != null)
            linkedAnimator.SetBool(deathParameterID, false);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(deathParameterID, linkedStateMachine.currentState == PlayerStates.State.Death);
    }

    public void ResetGame()
    {
        Debug.Log($"[DEATH] DeathAbility.ResetGame called. CurrentProfileIndex={GetComponent<CloneSwitcher>()?.CurrentProfileIndex} TS.State={(TerminalSession.Instance?.State)}");

        if (player != null && player.gameObject.name.Contains("GhostRoot"))
            return;

        var ts = TerminalSession.Instance;
        if (ts != null && (ts.State == TerminalSession.TerminalState.EnteringRecord
                        || ts.State == TerminalSession.TerminalState.EnteringTerminal))
        {
            Debug.Log($"[DEATH] DeathAbility.ResetGame IGNORED: TS already transitioning (state={ts.State})");
            return;
        }

        if (TerminalSession.Instance != null &&
            TerminalSession.Instance.State == TerminalSession.TerminalState.Recording)
        {
            TerminalSession.Instance.RequestRestartAndEnterTerminal();
            return;
        }

        LevelManager.Instance.RestartLevel();
    }
}