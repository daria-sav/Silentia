using System.Collections;
using UnityEngine;

/// <summary>
/// Saves the keyframe AFTER the current tick's physics step, via a coroutine.
/// WaitForFixedUpdate. So keyframes[T].pos = the physics result of tick T,
/// and it can be directly compared with the same moment in the playback.
/// </summary>
[DefaultExecutionOrder(50)]
public class ReplayLateKeyframer : MonoBehaviour
{
    public void ScheduleKeyframe(int tick, ReplayClip clip, PlayerMovement motorRef)
    {
        if (clip == null || motorRef == null) return;
        StartCoroutine(SaveAfterPhysics(tick, clip, motorRef));
    }

    private IEnumerator SaveAfterPhysics(int tick, ReplayClip clip, PlayerMovement motor)
    {
        yield return new WaitForFixedUpdate();

        if (clip == null || motor == null || motor.RB == null) yield break;

        clip.AddKeyframe(tick, motor.RB.position, motor.RB.linearVelocity);
    }
}