using UnityEngine;

/// <summary>
/// Applies drift correction AFTER PlayerMovement has processed
/// the current tick's input. This ensures the correction targets
/// the exact post-motor state that was recorded by ReplayLateKeyframer,
/// without the motor adding extra movement on top.
///
/// Execution order: ReplayPlayback (-400) → PlayerBrain (-250)
/// → PlayerMovement (0) → ReplayDriftCorrector (100)
/// </summary>
[DefaultExecutionOrder(100)]
public class ReplayDriftCorrector : MonoBehaviour
{
    [Header("Drift correction")]
    [SerializeField] private float positionEpsilon = 0.15f; // ignore drift below this distance
    [SerializeField] private float hardSnapThreshold = 0.75f; // teleport to keyframe if drift exceeds this distance
    [SerializeField] private float pullStrength = 0.5f; // soft pull strength per tick

    private bool hasPending;
    private Vector2 targetPos;
    private Vector2 targetVel;
    private PlayerMovement motor;

    // ────────────────── API ──────────────────

    #region Public API
    /// schedules a correction to be applied after the motor runs
    public void ScheduleCorrection(Vector2 keyframePos, Vector2 keyframeVel, PlayerMovement motorRef)
    {
        motor = motorRef;
        targetPos = keyframePos;
        targetVel = keyframeVel;
        hasPending = true;
    }

    // Cancels any pending correction. Called when playback finishes or the ghost is destroyed
    public void Clear()
    {
        hasPending = false;
        motor = null;
    }
    #endregion

    // ──────────── FIXED-STEP CORRECTION ──────

    #region Fixed-step Correction
    private void FixedUpdate()
    {
        if (!hasPending) return;
        hasPending = false;

        if (motor == null) return;

        Vector2 curPos = transform.position;
        float err = Vector2.Distance(curPos, targetPos);

        if (err < positionEpsilon) return;

        if (err >= hardSnapThreshold)
        {
            // large drift: hard snap (collisions may have diverged)
            transform.position = targetPos;
            motor.RB.linearVelocity = targetVel;
        }
        else
        {
            // small drift: smooth pull to avoid visible teleport
            transform.position = Vector2.Lerp(curPos, targetPos, pullStrength);
            motor.RB.linearVelocity = Vector2.Lerp(motor.RB.linearVelocity, targetVel, pullStrength);
        }
    }
    #endregion
}