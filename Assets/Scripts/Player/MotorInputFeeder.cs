using UnityEngine;

[DefaultExecutionOrder(-200)]
public class MotorInputFeeder : MonoBehaviour
{
    private GatherInput input;
    private Player player;

    private void Awake()
    {
        input = GetComponent<GatherInput>();
        player = GetComponent<Player>();
    }

    private void FixedUpdate()
    {
        if (input == null || player == null) return;

        var motor = player.motor;
        if (motor == null) return;

        motor.SetMoveInput(input.move.x, input.move.y);
    }
}