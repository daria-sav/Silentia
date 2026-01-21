using UnityEngine;

public class MoveAbility : BaseAbility
{
    [SerializeField] private float speed;

    private string walkAnimParameterName = "Walk";
    private int walkParameterID;

    protected override void Initialization()
    {
        base.Initialization();
        walkParameterID = Animator.StringToHash(walkAnimParameterName);
    }

    public override void EnterAbility()
    {
        player.Flip();
    }
    public override void ProcessAbility()
    {
        if (linkedPhysics.isGrounded && linkedInput.horizontalInput == 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
        if (!linkedPhysics.isGrounded)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
        }
    }

    public override void ProcessFixedAbility()
    {
        linkedPhysics.rb.linearVelocity = new Vector2(linkedInput.horizontalInput * speed, linkedPhysics.rb.linearVelocityY);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(walkParameterID, linkedStateMachine.currentState == PlayerStates.State.Walk);
    }
}
