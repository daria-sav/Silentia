using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Reads GatherInput every fixed tick and feeds the PlayerMovement motor.
/// Handles both continuous input (move direction) and discrete events
/// (jump press/release, dash press).
/// </summary>
[DefaultExecutionOrder(-250)]
public class PlayerBrain : MonoBehaviour
{
    private GatherInput input;
    private Player player;
    private MultipleJumpAbility jump;
    private DashAbility dash;
    private ReplayRecorder recorder;

    private void Awake()
    {
        input = GetComponent<GatherInput>();
        player = GetComponent<Player>();
        jump = GetComponent<MultipleJumpAbility>();
        dash = GetComponent<DashAbility>();
        recorder = GetComponent<ReplayRecorder>();
    }

    private void FixedUpdate()
    {
        if (input == null || player == null) return;

        var motor = player.motor;
        if (motor == null) return;

        // continuous: move direction
        motor.SetMoveInput(input.move.x, input.move.y);

        // discrete: jump
        if (input.jumpDownTick && jump != null)
        {
            jump.TryToJump();

            input.ClearJumpDownTick(); 
        }

        if (input.jumpUpTick && jump != null)
        {
            jump.OnJumpReleased();
            input.ClearJumpUpTick();
        }

        // discrete: dash
        if (input.dashDownTick && dash != null)
        {
            dash.TryStartDash();
            input.ClearDashDownTick(); 
        }

        // dashUpTick????

        // stop recording
        if (recorder != null && recorder.IsRecording && input.ConsumeStopRecordDown())
        {
            recorder.StopRecording();
        }

        HandleCameraYDamping(motor);
    }

    private void HandleCameraYDamping(PlayerMovement motor)
    {
        if (CameraManager.instance == null) return;

        float velY = motor.RB.linearVelocity.y;

        // Falling faster than the threshold - enable damping
        if (velY < CameraManager.instance.fallSpeedYDampingChangeThreshold
            && !CameraManager.instance.isLerpingYDamping
            && !CameraManager.instance.lerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpYDamping(true);
        }

        // landed / flying up - reset damping
        if (velY >= 0f
            && !CameraManager.instance.isLerpingYDamping
            && CameraManager.instance.lerpedFromPlayerFalling)
        {
            CameraManager.instance.lerpedFromPlayerFalling = false;
            CameraManager.instance.LerpYDamping(false);
        }
    }
}