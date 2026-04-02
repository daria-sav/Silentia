using UnityEngine;

public class MoveAbility : BaseAbility
{
    [SerializeField] private float speed;

    private string walkAnimParameterName = "Walk";
    private int walkParameterID;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    protected override void Initialization()
    {
        base.Initialization();
        walkParameterID = Animator.StringToHash(walkAnimParameterName);
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(walkParameterID, linkedStateMachine.currentState == PlayerStates.State.Walk);
    }
}
