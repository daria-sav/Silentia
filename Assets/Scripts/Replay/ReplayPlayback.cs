using UnityEngine;

/// <summary>
/// Drives ghost playback by feeding recorded InputFrames into
/// GatherInput each fixed tick. Drift correction is delegated
/// to ReplayDriftCorrector which runs after the motor.
///
/// Execution order chain:
///   ReplayPlayback (-400) applies input
///   → PlayerBrain (-250) feeds motor
///   → PlayerMovement (0) processes physics
///   → ReplayDriftCorrector (100) corrects drift
/// </summary>
[DefaultExecutionOrder(-400)]
public class ReplayPlayback : MonoBehaviour
{
    public GatherInput input;

    private ReplayClip clip;
    private int tick;
    public bool IsPlaying { get; private set; }

    private Player player;
    private ReplayDriftCorrector driftCorrector;

    private int kfIndex;
    private bool destroyedByDeath;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (input == null) input = GetComponent<GatherInput>();
        player = GetComponent<Player>();
        driftCorrector = GetComponent<ReplayDriftCorrector>();
    }

    private void OnDisable()
    {
        if (IsPlaying)
            ClearCorrector();
        
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void StartPlayback(ReplayClip replayClip)
    {
        clip = replayClip;
        tick = 0;
        IsPlaying = true;
        kfIndex = 0;
        destroyedByDeath = false;

        WarnIfSimulationMismatch();

        if (input != null)
            input.SetMode(GatherInput.InputMode.Replay);

        ApplyStartSnapshot(clip);

        Debug.Log($"REPLAY: Playback started. Profile={clip?.profileId}, frames={clip?.FrameCount}");
    }
    #endregion

    // ──────────── FIXED-STEP PLAYBACK ────────

    #region Fixed-step Playback
    private void FixedUpdate()
    {
        if (!IsPlaying || clip == null || input == null) return;

        // clip finished
        if (tick >= clip.FrameCount)
        {
            FinishPlayback();
            return;
        }

        // feed this tick's input (motor will process it later this FixedUpdate cycle)
        var frame = clip.GetFrame(tick);
        input.ApplyReplayFrame(frame);

        // schedule drift correction to run AFTER the motor
        ScheduleDriftCorrection(tick);

        // ghost died during playback
        if (CheckGhostDeath()) return;

        tick++;
    }
    #endregion

    // ──────────── START / FINISH ─────────────

    #region Start and Finish
    private void ApplyStartSnapshot(ReplayClip clip)
    {
        if (clip == null) return;

        // position
        transform.position = clip.startPosition;

        // velocity
        var motor = player != null ? player.motor : null;
        if (motor != null)
        {
            motor.RB.linearVelocity = clip.startVelocity;
            motor.ResetMotorState();
            motor.SetGravityScale(motor.data != null ? motor.data.calculatedGravityScale : 1f);
        }

        // facing + visual flip
        if (player != null)
        {
            player.facingRight = clip.startFacingRight;

            if (player.motor != null)
                player.motor.UpdateFacingDirection(clip.startFacingRight);

            if (player.visual != null)
            {
                var scale = player.visual.localScale;
                scale.x = clip.startFacingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                player.visual.localScale = scale;
            }

            if (player.stateMachine != null)
                player.stateMachine.ForceChange(clip.startState);
        }
    }

    private void FinishPlayback()
    {
        IsPlaying = false;
        ClearCorrector();

        // clear ghost input
        input.ApplyReplayFrame(default);

        // stop horizontal drift, keep vertical
        var motor = player != null ? player.motor : null;
        if (motor != null)
            motor.RB.linearVelocity = new Vector2(0f, motor.RB.linearVelocity.y);

        if (player != null && player.stateMachine != null)
            player.stateMachine.ForceChange(PlayerStates.State.Idle);
    }
    #endregion

    // ──────────── DRIFT CORRECTION ───────────

    #region Drift Correction
    private void ScheduleDriftCorrection(int currentTick)
    {
        if (driftCorrector == null) return;
        if (clip.keyframes == null || clip.keyframes.Count == 0) return;

        var motor = player != null ? player.motor : null;
        if (motor == null) return;

        // advance keyframe pointer to latest keyframe <= currentTick
        while (kfIndex + 1 < clip.keyframes.Count && clip.keyframes[kfIndex + 1].tick <= currentTick)
            kfIndex++;

        var kf = clip.keyframes[kfIndex];

        // only correct on exact keyframe ticks
        if (kf.tick != currentTick) return;

        driftCorrector.ScheduleCorrection(kf.pos, kf.vel, motor);
    }

    private void ClearCorrector()
    {
        if (driftCorrector != null)
            driftCorrector.Clear();
    }
    #endregion

    // ──────────── HELPERS ────────────────────

    #region Helpers
    private bool CheckGhostDeath()
    {
        if (destroyedByDeath) return true;
        if (player == null || player.stateMachine == null) return false;
        if (player.stateMachine.currentState != PlayerStates.State.Death) return false;

        destroyedByDeath = true;
        IsPlaying = false;
        ClearCorrector();
        Destroy(gameObject);
        return true;
    }

    private void WarnIfSimulationMismatch()
    {
        if (clip == null) return;

        if (!Mathf.Approximately(Time.fixedDeltaTime, clip.fixedDeltaTime))
            Debug.LogWarning($"REPLAY: fixedDeltaTime mismatch. Now={Time.fixedDeltaTime} Clip={clip.fixedDeltaTime}");

        if (Physics2D.velocityIterations != clip.velocityIterations
            || Physics2D.positionIterations != clip.positionIterations)
            Debug.LogWarning("REPLAY: Physics2D iterations mismatch. Drift possible.");
    }
    #endregion
}