using UnityEngine;

/// <summary>
/// Records a replay keyframe after the current physics tick has already been
/// processed by <see cref="PlayerMovement"/>.
///
/// This component acts as a delayed companion to <see cref="ReplayRecorder"/>:
/// the recorder captures input at the beginning of the fixed tick, while this
/// class stores the resulting post-movement position and velocity later in the
/// same tick.
///
/// This is important for replay drift correction, because jump and dash impulses
/// must already be applied before the keyframe is saved. Otherwise playback
/// would compare against an outdated pre-movement state.
///
/// Execution order:
/// - ReplayRecorder (-300) captures input
/// - PlayerMovement (0) applies movement and impulses
/// - ReplayLateKeyframer (50) stores the resulting keyframe
/// </summary>
[DefaultExecutionOrder(50)]
public class ReplayLateKeyframer : MonoBehaviour
{
    // pending keyframe request for the current fixed-step cycle
    private bool hasPendingRequest;
    private int pendingTick;
    private ReplayClip pendingClip;
    private PlayerMovement pendingMotor;

    // ────────────────── API ──────────────────

    #region Public API
    /// <summary>
    /// Schedules a keyframe to be recorded later in this fixed tick,
    /// after <see cref="PlayerMovement"/> has already updated the body.
    /// </summary>
    /// <param name="tick"> replay tick that this keyframe belongs to.</param>
    /// <param name="clip"> target replay clip that receives the keyframe.</param>
    /// <param name="motorRef"> movement component used to read the final velocity.</param>
    public void ScheduleKeyframe(int tick, ReplayClip clip, PlayerMovement motorRef)
    {
        pendingTick = tick;
        pendingClip = clip;
        pendingMotor = motorRef;
        hasPendingRequest = true;
    }
    #endregion

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void FixedUpdate()
    {
        if (!hasPendingRequest) 
            return;

        // consume the scheduled request once per tick
        hasPendingRequest = false;

        if (pendingClip == null || pendingMotor == null)
        {
            ClearPendingReferences();
            return;
        }

        // capture the post-movement transform and velocity so the saved
        // keyframe matches the final result of this simulation tick
        Vector2 pos = transform.position;
        Vector2 vel = pendingMotor.RB.linearVelocity;

        pendingClip.AddKeyframe(pendingTick, pos, vel);

        ClearPendingReferences();
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    /// <summary>
    /// Clears references from the previously consumed keyframe request
    /// </summary>
    private void ClearPendingReferences()
    {
        pendingClip = null;
        pendingMotor = null;
    }
    #endregion
}