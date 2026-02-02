using UnityEngine;

[DefaultExecutionOrder(-250)]
public class PlayerBrain : MonoBehaviour
{
    private GatherInput input;
    private MultipleJumpAbility jump;
    private DashAbility dash;
    private Player player;

    private void Awake()
    {
        input = GetComponent<GatherInput>();
        jump = GetComponent<MultipleJumpAbility>();
        dash = GetComponent<DashAbility>();
        player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        if (input == null) return;

        if (input.jumpDownTick && jump != null)
        {
            jump.TryToJump();
            input.ClearJumpDownTick(); 
        }

        if (input.dashDownTick && dash != null)
        {
            dash.TryStartDash();
            input.ClearDashDownTick(); 
        }

        if (input.jumpDownTick)
        {
            Debug.Log($"GHOST BRAIN {gameObject.name}: saw jumpDownTick");
            Debug.Log($"[BRAIN] state={player.stateMachine.currentState} grounded={player.physicsControl.isGrounded} coyote={player.physicsControl.coyoteTimer:F2} num={jump.DebugNumJumps()} canAdd={jump.DebugCanAdd()}");
        }
    }
}