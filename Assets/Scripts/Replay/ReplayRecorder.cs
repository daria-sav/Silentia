using System;
using UnityEngine;

/// <summary>
/// Records player input each fixed tick into a ReplayClip.
/// Input is captured early (before PlayerBrain consumes it),
/// while keyframe position/velocity recording is delegated to
/// ReplayLateKeyframer which runs after the motor.
///
/// Keyframes are saved periodically and on critical input events
/// (jumpDown, jumpUp, dashDown) for precise drift correction.
/// </summary>
[DefaultExecutionOrder(-300)]
public class ReplayRecorder : MonoBehaviour
{
    [Header("Links")]
    public GatherInput input;
    public CloneSwitcher cloneSwitcher;

    [Header("Limits")]
    public float maxSeconds = 15f;

    [Header("Drift correction")]
    [SerializeField] private int keyframeEveryTicks = 10; // ~0.2s if fixedDeltaTime=0.02

    public bool IsRecording { get; private set; }
    public ReplayClip CurrentClip { get; private set; }

    public event Action<ReplayClip> OnRecordingStopped;

    private Player player;
    private ReplayLateKeyframer lateKeyframer;
    private int tick;
    private int maxTicks;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();
        if (cloneSwitcher == null) cloneSwitcher = GetComponent<CloneSwitcher>();

        player = GetComponent<Player>();
        lateKeyframer = GetComponent<ReplayLateKeyframer>();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void StartRecording()
    {
        if (cloneSwitcher == null || cloneSwitcher.CurrentProfile == null)
        {
            Debug.Log($"[RR] StartRecording using CurrentProfile id={cloneSwitcher.CurrentProfile.id}, name={cloneSwitcher.CurrentProfile.name}, currentBody={(cloneSwitcher.CurrentProfileIndex)}");
            Debug.LogError("ReplayRecorder: CurrentProfile is null. Cannot start recording.");
            return;
        }

        CurrentClip = new ReplayClip(cloneSwitcher.CurrentProfile);

        // simulation contract
        CurrentClip.fixedDeltaTime = Time.fixedDeltaTime;
        CurrentClip.velocityIterations = Physics2D.velocityIterations;
        CurrentClip.positionIterations = Physics2D.positionIterations;        

        // start snapshot
        var motor = player != null ? player.motor : null;

        // jump
        if (motor != null)
            CurrentClip.startMotorSnapshot = motor.CaptureFullSnapshot();

        // legacy
        CurrentClip.startIsJumping = motor != null && motor.IsJumping;
        CurrentClip.startLastOnGroundTime = motor != null ? motor.LastOnGroundTime : 0f;
        CurrentClip.startAirJumpsLeft = motor != null ? motor.AirJumpsLeft : 0;

        CurrentClip.startPosition = motor != null ? motor.RB.position : (Vector2)transform.position;
        CurrentClip.startVelocity = motor != null ? motor.RB.linearVelocity : Vector2.zero;
        CurrentClip.startFacingRight = (player != null) ? player.facingRight : true;
        CurrentClip.startState = (player != null && player.stateMachine != null)
            ? player.stateMachine.currentState
            : PlayerStates.State.Idle;

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

        // final keyframe (recorded directly since this is the last tick)
        var motor = player != null ? player.motor : null;

        if (CurrentClip != null && motor != null)
            CurrentClip.AddKeyframe(tick, motor.RB.position, motor.RB.linearVelocity);

        if (CurrentClip != null)
            OnRecordingStopped?.Invoke(CurrentClip);
    }
    #endregion

    // ──────────── FIXED-STEP RECORDING ───────

    #region Fixed-step Recording
    private void FixedUpdate()
    {
        if (!IsRecording || input == null || CurrentClip == null) return;

        var frame = input.CaptureFrame(tick);
        CurrentClip.frames.Add(frame);

        // determine if this tick needs a keyframe
        bool isPeriodicKeyframe = keyframeEveryTicks > 0 && (tick % keyframeEveryTicks == 0);
        bool isCriticalAction = frame.jumpDown || frame.jumpUp || frame.dashDown;

        if (isPeriodicKeyframe || isCriticalAction)
            RequestKeyframe(tick);

        tick++;

        if (tick >= maxTicks)
            StopRecording();
    }
    #endregion

    // ──────────── HELPERS ────────────────────

    #region Helpers
    private void RequestKeyframe(int keyframeTick)
    {
        var motor = player != null ? player.motor : null;

        // prefer late keyframer (records AFTER motor processes this tick's input)
        if (lateKeyframer != null && motor != null)
        {
            lateKeyframer.ScheduleKeyframe(keyframeTick, CurrentClip, motor);
        }
        else
        {
            // fallback: record now (pre-motor, less accurate but functional)
            Vector2 pos = (motor != null) ? motor.RB.position : (Vector2)transform.position;
            Vector2 vel = (motor != null) ? motor.RB.linearVelocity : Vector2.zero;
            CurrentClip.AddKeyframe(keyframeTick, pos, vel);
        }
    }
    #endregion
}