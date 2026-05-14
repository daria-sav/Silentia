using System.Collections;
using UnityEngine;

/// <summary>
/// Pulls the ghost toward the recorded trajectory on keyframes when the
/// only delta is floating-point drift.
///
/// Two safety channels skip the pull when the worlds have actually parted
/// ways (geometry changed, trajectory split) — without these guards the
/// corrector would hover the ghost where geometry no longer exists.
///
/// Runs after the current tick's physics step via WaitForFixedUpdate,
/// matching the post-physics moment captured by ReplayLateKeyframer.
/// </summary>
[DefaultExecutionOrder(100)]
public class ReplayDriftCorrector : MonoBehaviour
{
    [Header("Drift correction")]
    [Tooltip("Position error smaller than this is ignored.")]
    [SerializeField] private float positionEpsilon = 0.005f;

    [Tooltip("Above this position error the worlds have diverged — correction is skipped.")]
    [SerializeField] private float positionDivergenceThreshold = 0.5f;

    [Tooltip("Above this velocity error the trajectory has split — correction is skipped.")]
    [SerializeField] private float velocityDivergenceThreshold = 2f;

    [Tooltip("Pull amount applied to position. 1 = full snap to recorded value.")]
    [Range(0f, 1f)]
    [SerializeField] private float pullStrength = 0.25f;

    [Tooltip("Also pull velocity toward the recorded value. Tightens lock-step but changes physical feel.")]
    [SerializeField] private bool correctVelocity = false;

    [Header("Debug")]
    [Tooltip("Logs every correction decision to Console.")]
    [SerializeField] private bool debugLog = false;

    private PlayerMovement motor;
    private Vector2 targetPos;
    private Vector2 targetVel;
    private bool hasPending;

    // ────────────────── API ──────────────────

    #region Public API
    public void ScheduleCorrection(Vector2 keyframePos, Vector2 keyframeVel, PlayerMovement motorRef)
    {
        motor = motorRef;
        targetPos = keyframePos;
        targetVel = keyframeVel;

        // a pending coroutine picks up the latest target on resume
        if (!hasPending)
        {
            hasPending = true;
            StartCoroutine(CorrectAfterPhysics());
        }
    }

    public void Clear()
    {
        StopAllCoroutines();
        hasPending = false;
        motor = null;
    }
    #endregion

    // ────────── FIXED-STEP CORRECTION ─────────

    #region Fixed-step Correction
    private IEnumerator CorrectAfterPhysics()
    {
        yield return new WaitForFixedUpdate();

        hasPending = false;

        if (motor == null || motor.RB == null) yield break;

        Vector2 curPos = motor.RB.position;
        Vector2 curVel = motor.RB.linearVelocity;

        float posErr = Vector2.Distance(curPos, targetPos);

        // negligible drift
        if (posErr < positionEpsilon)
        {
            if (debugLog) Debug.Log($"[Drift] noop  posErr={posErr:F4}");
            yield break;
        }

        // worlds diverged (anti-hover guard)
        if (posErr > positionDivergenceThreshold)
        {
            if (debugLog) Debug.Log($"[Drift] SKIP pos  posErr={posErr:F2} > {positionDivergenceThreshold} (world diverged)");
            yield break;
        }

        // trajectory diverged
        float velDiff = Vector2.Distance(curVel, targetVel);
        if (velDiff > velocityDivergenceThreshold)
        {
            if (debugLog) Debug.Log($"[Drift] SKIP vel  velDiff={velDiff:F2} > {velocityDivergenceThreshold} (trajectory diverged)");
            yield break;
        }

        // FP drift — pull toward recorded trajectory
        motor.RB.position = Vector2.Lerp(curPos, targetPos, pullStrength);

        if (correctVelocity)
            motor.RB.linearVelocity = Vector2.Lerp(curVel, targetVel, pullStrength);

        if (debugLog) Debug.Log($"[Drift] pull  posErr={posErr:F3} velDiff={velDiff:F3}");
    }
    #endregion
}