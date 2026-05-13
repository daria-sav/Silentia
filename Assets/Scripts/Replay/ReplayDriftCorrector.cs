using System.Collections;
using UnityEngine;

/// <summary>
/// Softly corrects small replay drift without overriding real physics divergence.
/// </summary>
[DefaultExecutionOrder(100)]
public class ReplayDriftCorrector : MonoBehaviour
{
    [Header("Drift Correction")]
    [Tooltip("Position error smaller than this is ignored.")]
    [SerializeField] private float positionEpsilon = 0.005f;

    [Tooltip("Large position gaps are treated as real divergence and are not corrected.")]
    [SerializeField] private float positionDivergenceThreshold = 0.5f;

    [Tooltip("Large velocity differences mean the replay is no longer on the same trajectory.")]
    [SerializeField] private float velocityDivergenceThreshold = 2f;

    [Tooltip("Soft pull amount applied to position when correcting.")]
    [Range(0f, 1f)]
    [SerializeField] private float pullStrength = 0.25f;

    [Tooltip("If enabled, velocity is also pulled toward the recorded value.")]
    [SerializeField] private bool correctVelocity = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private PlayerMovement motor;
    private Vector2 targetPos;
    private Vector2 targetVel;
    private bool hasPending;

    // ───────────── PUBLIC API ─────────────

    #region Public API
    public void ScheduleCorrection(Vector2 keyframePos, Vector2 keyframeVel, PlayerMovement motorRef)
    {
        motor = motorRef;
        targetPos = keyframePos;
        targetVel = keyframeVel;

        // reuses the pending coroutine and lets it apply the latest scheduled keyframe
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

    // ───────────── CORRECTION ─────────────

    #region Correction
    private IEnumerator CorrectAfterPhysics()
    {
        // runs after the current physics step, matching when replay keyframes were captured
        yield return new WaitForFixedUpdate();

        hasPending = false;

        if (motor == null || motor.RB == null) yield break;

        Vector2 curPos = motor.RB.position;
        Vector2 curVel = motor.RB.linearVelocity;

        float posErr = Vector2.Distance(curPos, targetPos);

        // large position error usually means the world state changed, not floating-point drift
        if (posErr < positionEpsilon)
        {
            if (debugLog) Debug.Log($"[Drift] noop  posErr={posErr:F4}");
            yield break;
        }

        // 2. Position diverged too far — worlds have parted ways.
        //    This is the ANTI-HOVER guard: stops the corrector from snapping
        //    the ghost back onto geometry that no longer exists at playback.
        if (posErr > positionDivergenceThreshold)
        {
            if (debugLog) Debug.Log($"[Drift] SKIP pos  posErr={posErr:F2} > {positionDivergenceThreshold} (world diverged)");
            yield break;
        }

        // large velocity error means the live replay has split onto a different trajectory
        float velDiff = Vector2.Distance(curVel, targetVel);
        if (velDiff > velocityDivergenceThreshold)
        {
            if (debugLog) Debug.Log($"[Drift] SKIP vel  velDiff={velDiff:F2} > {velocityDivergenceThreshold} (trajectory diverged)");
            yield break;
        }

        motor.RB.position = Vector2.Lerp(curPos, targetPos, pullStrength);

        if (correctVelocity)
            motor.RB.linearVelocity = Vector2.Lerp(curVel, targetVel, pullStrength);

        if (debugLog) Debug.Log($"[Drift] pull  posErr={posErr:F3} velDiff={velDiff:F3}");
    }
    #endregion
}