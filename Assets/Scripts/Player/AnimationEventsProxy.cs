using UnityEngine;

/// <summary>
/// Forwards animation events from the visual object to the player logic.
/// </summary>
public class AnimationEventsProxy : MonoBehaviour
{
    [SerializeField] private Player player;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }
    #endregion

    // ─────────────── ANIMATION EVENTS ───────────────

    #region Animation Events
    public void ResetGame()
    {
        Debug.Log($"[ANIM] ResetGame called. recorder.IsRecording={(player?.GetComponent<ReplayRecorder>()?.IsRecording)} TS.State={(TerminalSession.Instance?.State)}");

        if (player == null)
        {
            Debug.LogError("AnimationEventsProxy: Player not found");
            return;
        }

        // ghosts never restart the level
        if (player.gameObject.name.Contains("GhostRoot"))
            return;

        var ts = TerminalSession.Instance;
        if (ts != null && (ts.State == TerminalSession.TerminalState.EnteringRecord
                        || ts.State == TerminalSession.TerminalState.EnteringTerminal))
        {
            Debug.Log($"[ANIM] ResetGame IGNORED: TS already transitioning (state={ts.State})");
            return;
        }

        // stops the active recording instead of restarting immediately
        var recorder = player.GetComponent<ReplayRecorder>();
        if (recorder != null && recorder.IsRecording)
        {
            recorder.StopRecording();
            return;
        }

        // during terminal recording, death returns the player to the terminal flow
        if (TerminalSession.Instance != null &&
            TerminalSession.Instance.State == TerminalSession.TerminalState.Recording)
        {
            TerminalSession.Instance.RequestRestartAndEnterTerminal();
            return;
        }

        // default death behavior outside terminal and recording states
        var deathAbility = player.GetComponent<DeathAbility>();
        if (deathAbility != null) deathAbility.ResetGame();
        else if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
        else Debug.LogError("AnimationEventsProxy: LevelManager.instance is null");
    }
    #endregion
}