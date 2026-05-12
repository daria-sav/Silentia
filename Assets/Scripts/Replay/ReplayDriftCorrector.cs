using System.Collections;
using UnityEngine;

/// <summary>
/// Nudges the ghost back onto the recorded trajectory only when the live
/// simulation is still on that same trajectory 
///
/// Runs after the current tick's physics step (via WaitForFixedUpdate),
/// matching the post-physics moment at which ReplayLateKeyframer captured
/// the recording.
/// </summary>
[DefaultExecutionOrder(100)]
public class ReplayDriftCorrector : MonoBehaviour
{
    [Header("Drift correction")]
    [Tooltip("Position error smaller than this is ignored.")]
    [SerializeField] private float positionEpsilon = 0.005f;

    [Tooltip("Position error larger than this is treated as real divergence and the correction is skipped")]
    [SerializeField] private float positionDivergenceThreshold = 0.5f;

    [Tooltip("Per-axis velocity tolerance.")]
    [SerializeField] private float velocityDivergenceThreshold = 0.75f;

    [Tooltip("Soft pull amount.")]
    [Range(0f, 1f)]
    [SerializeField] private float pullStrength = 0.25f;

    private PlayerMovement motor;
    private Vector2 targetPos;
    private Vector2 targetVel;
    private bool hasPending;

    public void ScheduleCorrection(Vector2 keyframePos, Vector2 keyframeVel, PlayerMovement motorRef)
    {
        motor = motorRef;
        targetPos = keyframePos;
        targetVel = keyframeVel;

        // if a coroutine is already in flight, it will pick up the latest target on resume
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

    private IEnumerator CorrectAfterPhysics()
    {
        yield return new WaitForFixedUpdate();

        hasPending = false;

        if (motor == null || motor.RB == null) yield break;

        Vector2 curPos = motor.RB.position;
        Vector2 curVel = motor.RB.linearVelocity;

        Vector2 posError = targetPos - curPos;
        Vector2 velError = targetVel - curVel;

        // negligible drift 
        float posErrorMag = posError.magnitude;
        if (posErrorMag < positionEpsilon) yield break;

        // position diverged too far
        if (posErrorMag > positionDivergenceThreshold) yield break;

        // velocity diverged on either axis
        if (Mathf.Abs(velError.x) > velocityDivergenceThreshold ||
            Mathf.Abs(velError.y) > velocityDivergenceThreshold)
            yield break;

        // same trajectory + small drift
        motor.RB.position = curPos + posError * pullStrength;
        motor.RB.linearVelocity = curVel + velError * pullStrength;
    }
}