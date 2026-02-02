using UnityEngine;

[DefaultExecutionOrder(-400)]
public class ReplayPlayback : MonoBehaviour
{
    public GatherInput input;

    private ReplayClip clip;
    private int tick;
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();
    }

    public void StartPlayback(ReplayClip replayClip)
    {
        clip = replayClip;
        tick = 0;
        IsPlaying = true;

        if (input != null)
            input.SetMode(GatherInput.InputMode.Replay);

        if (clip != null && clip.FrameCount > 0 && input != null)
        {
            input.ApplyReplayFrame(clip.GetFrame(0));
            tick = 1;
        }

        Debug.Log($"REPLAY: Playback started. Profile={clip?.profileId}, frames={clip?.FrameCount}");
    }

    private void FixedUpdate()
    {
        if (!IsPlaying || clip == null || input == null) return;

        if (tick >= clip.FrameCount)
        {
            IsPlaying = false;
            Debug.Log("REPLAY: Playback finished.");
            return;
        }

        var frame = clip.GetFrame(tick);
        input.ApplyReplayFrame(frame);

        tick++;
        if (frame.jumpDown || frame.dashDown)
            Debug.Log($"GHOST APPLY T{tick}: jumpDown={frame.jumpDown} dashDown={frame.dashDown}");
    }
}
