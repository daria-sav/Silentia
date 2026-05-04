using System.Collections;
using UnityEngine;

/// <summary>
/// Pulls the ghost to the recorded trajectory on the keyframes.
/// Comparison and application of corrections - after the current tick's physical step
/// (via WaitForFixedUpdate). This semantically matches the moment
/// when ReplayLateKeyframer saved the keyframe in the recording.
/// </summary>
[DefaultExecutionOrder(100)]
public class ReplayDriftCorrector : MonoBehaviour
{
    [Header("Drift correction")]
    [Tooltip("If err is less, don't adjust it.")]
    [SerializeField] private float positionEpsilon = 0.005f;

    [Tooltip("If err is greater, apply a hard snap")]
    [SerializeField] private float hardSnapThreshold = 5f;

    [Tooltip("Soft pull coefficient for each keyframe")]
    [Range(0f, 1f)]
    [SerializeField] private float pullStrength = 0.25f;

    [Tooltip("Also synchronize velocity")]
    [SerializeField] private bool correctVelocity = false;

    private PlayerMovement motor;
    private Vector2 targetPos;
    private Vector2 targetVel;
    private bool hasPending;

    public void ScheduleCorrection(Vector2 keyframePos, Vector2 keyframeVel, PlayerMovement motorRef)
    {
        motor = motorRef;
        targetPos = keyframePos;
        targetVel = keyframeVel;

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

        if (!hasPending || motor == null || motor.RB == null)
        {
            hasPending = false;
            yield break;
        }

        Vector2 curPos = motor.RB.position;
        float err = Vector2.Distance(curPos, targetPos);

        if (err < positionEpsilon)
        {
            hasPending = false;
            yield break;
        }

        Vector2 newPos = err >= hardSnapThreshold
            ? targetPos
            : Vector2.Lerp(curPos, targetPos, pullStrength);

        motor.RB.position = newPos;

        if (correctVelocity)
            motor.RB.linearVelocity = Vector2.Lerp(motor.RB.linearVelocity, targetVel, pullStrength);

        hasPending = false;
    }
}