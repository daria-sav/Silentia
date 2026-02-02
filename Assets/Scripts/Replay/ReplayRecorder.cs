using UnityEngine;

[DefaultExecutionOrder(-300)]
public class ReplayRecorder : MonoBehaviour
{
    [Header("Links")]
    public GatherInput input;
    public CloneSwitcher cloneSwitcher;

    [Header("Limits")]
    public float maxSeconds = 60f;

    public bool IsRecording { get; private set; }
    public ReplayClip CurrentClip { get; private set; }

    private int tick;
    private int maxTicks;

    private Player player;
    private PhysicsControl physics;

    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();
        if (cloneSwitcher == null) cloneSwitcher = GetComponent<CloneSwitcher>();

        player = GetComponent<Player>();
        physics = GetComponent<PhysicsControl>();
    }

    public void StartRecording()
    {
        if (cloneSwitcher == null || cloneSwitcher.CurrentProfile == null)
        {
            Debug.LogError("ReplayRecorder: CurrentProfile is null. Cannot start recording.");
            return;
        }

        CurrentClip = new ReplayClip(cloneSwitcher.CurrentProfile);

        CurrentClip.startPosition = transform.position;
        CurrentClip.startVelocity = (physics != null && physics.rb != null) ? physics.rb.linearVelocity : Vector2.zero;
        CurrentClip.startFacingRight = (player != null) ? player.facingRight : true;
        CurrentClip.startState = (player != null && player.stateMachine != null) ? player.stateMachine.currentState : PlayerStates.State.Idle;

        tick = 0;
        maxTicks = Mathf.RoundToInt(maxSeconds / Time.fixedDeltaTime);

        IsRecording = true;
        Debug.Log($"REPLAY: Recording started. Profile={CurrentClip.profileId}, maxTicks={maxTicks}");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;

        IsRecording = false;
        Debug.Log($"REPLAY: Recording stopped. Frames={CurrentClip?.FrameCount ?? 0}");

        int j = 0, d = 0;
        foreach (var f in CurrentClip.frames)
        {
            if (f.jumpDown) j++;
            if (f.dashDown) d++;
        }
        Debug.Log($"REPLAY STATS: jumpDown frames={j}, dashDown frames={d}");
    }

    private void FixedUpdate()
    {
        if (!IsRecording || input == null || CurrentClip == null) return;

        // take a frame
        var frame = input.CaptureFrame(tick);

        // add to clip
        CurrentClip.frames.Add(frame);

        tick++;

        // auto-stop for 60 seconds
        if (tick >= maxTicks)
        {
            StopRecording();
        }

        if (frame.jumpDown || frame.dashDown)
            Debug.Log($"REC T{tick}: jumpDown={frame.jumpDown} dashDown={frame.dashDown} jumpHeld={frame.jumpHeld}");
    }
}
