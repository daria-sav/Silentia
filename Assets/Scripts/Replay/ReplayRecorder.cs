using System;
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

    public event Action<ReplayClip> OnRecordingStopped;

    private int tick;
    private int maxTicks;

    private Player player;
    private PhysicsControl physics;

    [Header("Drift correction")]
    [SerializeField] private int keyframeEveryTicks = 10; // ~0.2s if fixedDeltaTime=0.02

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

        cloneSwitcher.SetHotkeysEnabled(false);

        CurrentClip = new ReplayClip(cloneSwitcher.CurrentProfile);

        // simulation contract
        CurrentClip.fixedDeltaTime = Time.fixedDeltaTime;
        CurrentClip.velocityIterations = Physics2D.velocityIterations;
        CurrentClip.positionIterations = Physics2D.positionIterations;

        // start snapshot
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

        if (CurrentClip != null && physics != null && physics.rb != null)
            CurrentClip.AddKeyframe(tick, transform.position, physics.rb.linearVelocity);

        if (CurrentClip != null)
            OnRecordingStopped?.Invoke(CurrentClip);

        int j = 0, d = 0;
        foreach (var f in CurrentClip.frames)
        {
            if (f.jumpDown) j++;
            if (f.dashDown) d++;
        }

    }

    private void FixedUpdate()
    {
        if (!IsRecording || input == null || CurrentClip == null) return;

        var frame = input.CaptureFrame(tick);
        CurrentClip.frames.Add(frame);

        // save drift-correction keyframe every N ticks
        if (keyframeEveryTicks > 0 && (tick % keyframeEveryTicks == 0))
        {
            Vector2 pos = transform.position;
            Vector2 vel = (physics != null && physics.rb != null) ? physics.rb.linearVelocity : Vector2.zero;
            CurrentClip.AddKeyframe(tick, pos, vel);
        }

        tick++;

        // auto-stop for 60 seconds
        if (tick >= maxTicks)
        {
            StopRecording();
        }
    }
}
