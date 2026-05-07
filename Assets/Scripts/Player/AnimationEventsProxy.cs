using UnityEngine;

public class AnimationEventsProxy : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();
    }

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

        // if this is the real hero -> stop record 
        var recorder = player.GetComponent<ReplayRecorder>();
        if (recorder != null && recorder.IsRecording)
        {
            recorder.StopRecording();
            return;
        }

        // if terminal session is Recording -> go to terminal restart
        if (TerminalSession.Instance != null &&
            TerminalSession.Instance.State == TerminalSession.TerminalState.Recording)
        {
            TerminalSession.Instance.RequestRestartAndEnterTerminal();
            return;
        }

        // normal behavior
        var deathAbility = player.GetComponent<DeathAbility>();
        if (deathAbility != null) deathAbility.ResetGame();
        else if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
        else Debug.LogError("AnimationEventsProxy: LevelManager.instance is null");
    }
}