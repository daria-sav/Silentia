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

        // if restart is globally forbidden (ghost playback), do nothing
        if (!RestartPolicy.AllowLevelRestart)
            return;

        // if this is the real hero and we are recording -> stop recording
        var recorder = player.GetComponent<ReplayRecorder>();
        if (recorder != null && recorder.IsRecording)
        {
            recorder.StopRecording(); 
            return;
        }

        // normal behavior

        var deathAbility = player.GetComponent<DeathAbility>();
        if (deathAbility != null) deathAbility.ResetGame();
        else LevelManager.instance.RestartLevel(); 
    }
}
