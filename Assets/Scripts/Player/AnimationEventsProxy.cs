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
        if (player == null)
        {
            Debug.LogError("AnimationEventsProxy: Player not found");
            return;
        }

        // 1) Ghosts NEVER restart the level
        if (player.gameObject.name.Contains("GhostRoot"))
            return;

        // 2) If this is the real hero AND we are recording -> STOP RECORDING (this will trigger terminal restart)
        var recorder = player.GetComponent<ReplayRecorder>();
        if (recorder != null && recorder.IsRecording)
        {
            recorder.StopRecording();
            return;
        }

        // 3) If terminal session says Recording (extra safety) -> go to terminal restart
        if (TerminalSession.Instance != null &&
            TerminalSession.Instance.State == TerminalSession.TerminalState.Recording)
        {
            TerminalSession.Instance.RequestRestartAndEnterTerminal();
            return;
        }

        // 4) IMPORTANT: do NOT block hero death restart because ghosts are playing

        // 5) Normal behavior
        var deathAbility = player.GetComponent<DeathAbility>();
        if (deathAbility != null) deathAbility.ResetGame();
        else if (LevelManager.instance != null) LevelManager.instance.RestartLevel();
        else Debug.LogError("AnimationEventsProxy: LevelManager.instance is null");
    }
}