using UnityEngine;

[DefaultExecutionOrder(-120)]
public class MotorStateDriver : MonoBehaviour
{
    [SerializeField] private float moveDeadzone = 0.01f;

    private Player player;
    private GatherInput input;

    private void Awake()
    {
        player = GetComponent<Player>();
        input = GetComponent<GatherInput>();
    }

    private void Update()
    {
        if (player == null || player.motor == null || input == null) return;

        var sm = player.stateMachine;

        if (sm.currentState == PlayerStates.State.Death || sm.currentState == PlayerStates.State.KnockBack)
            return;

        var m = player.motor;

        PlayerStates.State target;

        if (m.IsDashing)
            target = PlayerStates.State.Dash;
        else if (m.IsSliding)
            target = PlayerStates.State.WallSlide;
        else if (m.LastOnGroundTime > 0 && Mathf.Abs(m.RB.linearVelocity.y) < 0.05f)
            target = Mathf.Abs(input.move.x) > moveDeadzone ? PlayerStates.State.Walk : PlayerStates.State.Idle;
        else
            target = PlayerStates.State.Jump;

        if (target != sm.currentState)
            sm.ChangeState(target);
    }
}