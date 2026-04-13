using UnityEngine;

/// <summary>
/// Reads the motor's physical state every frame and translates it
/// into the ability state machine. This drives ability Enter/Exit
/// callbacks and animator parameter updates.
/// </summary>
[DefaultExecutionOrder(-120)]
public class MotorStateDriver : MonoBehaviour
{
    [SerializeField] private float velocityDeadzone = 0.01f;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player == null || player.motor == null) return;

        var stateMachine = player.stateMachine;
        var motor = player.motor;

        // don't override states that are managed by their own abilities
        if (stateMachine.currentState == PlayerStates.State.Death || stateMachine.currentState == PlayerStates.State.KnockBack)
            return;

        PlayerStates.State target;

        if (motor.IsDashing)
            target = PlayerStates.State.Dash;
        else if (motor.IsSliding)
            target = PlayerStates.State.WallSlide;
        else if (motor.LastOnGroundTime > 0)
            target = Mathf.Abs(motor.RB.linearVelocity.x) > velocityDeadzone
                ? PlayerStates.State.Walk
                : PlayerStates.State.Idle;
        else
            target = PlayerStates.State.Jump;

        if (target != stateMachine.currentState)
            stateMachine.ChangeState(target);
    }
}