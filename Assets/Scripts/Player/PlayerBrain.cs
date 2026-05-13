using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Feeds fixed-tick input into player movement, abilities, recording, and camera damping.
/// </summary>
[DefaultExecutionOrder(-250)]
public class PlayerBrain : MonoBehaviour
{
    private GatherInput input;
    private Player player;
    private MultipleJumpAbility jump;
    private DashAbility dash;
    private ReplayRecorder recorder;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
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

        // sends continuous movement input to the motor
        motor.SetMoveInput(input.move.x, input.move.y);

        // handles jump press for the current fixed tick
        if (input.jumpDownTick && jump != null)
        {
            jump.TryToJump();

            input.ClearJumpDownTick(); 
        }

        // handles jump release for variable jump height
        if (input.jumpUpTick && jump != null)
        {
            jump.OnJumpReleased();
            input.ClearJumpUpTick();
        }

        // handles dash press for the current fixed tick
        if (input.dashDownTick && dash != null)
        {
            dash.TryStartDash();
            input.ClearDashDownTick(); 
        }

        // stops recording when the stop-record input is consumed
        if (recorder != null && recorder.IsRecording && input.ConsumeStopRecordDown())
        {
            recorder.StopRecording();
        }

        HandleCameraYDamping(motor);
    }
    #endregion

    // ───────────── CAMERA ───────────────

    #region Camera
    private void HandleCameraYDamping(PlayerMovement motor)
    {
        if (CameraManager.instance == null) return;

        float velY = motor.RB.linearVelocity.y;

        // enables stronger vertical damping when the player falls fast enough
        if (velY < CameraManager.instance.fallSpeedYDampingChangeThreshold
            && !CameraManager.instance.isLerpingYDamping
            && !CameraManager.instance.lerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpYDamping(true);
        }

        // restores normal vertical damping after falling ends
        if (velY >= 0f
            && !CameraManager.instance.isLerpingYDamping
            && CameraManager.instance.lerpedFromPlayerFalling)
        {
            CameraManager.instance.lerpedFromPlayerFalling = false;
            CameraManager.instance.LerpYDamping(false);
        }
    }
    #endregion
}