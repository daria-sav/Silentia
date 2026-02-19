using UnityEngine;
using UnityEngine.InputSystem;

public class DashAbility : BaseAbility
{
    //public InputActionReference dashActionRef;
    [SerializeField] private float dashForce;
    [SerializeField] private float maxDashDuration;
    private float dashTimer;

    private string dashAnimParameterName = "Dash";
    private int dashParameterID;

    protected override void Initialization()
    {
        base.Initialization();
        dashParameterID = Animator.StringToHash(dashAnimParameterName);
    }

    //private void OnEnable()
    //{
    //    dashActionRef.action.performed += TryToDash;
    //}

    //private void OnDisable()
    //{
    //    dashActionRef.action.performed -= TryToDash;
    //}

    public override void ExitAbility()
    {
        linkedPhysics.EnableGravity();
        // optional 
        linkedPhysics.ResetVelocity();
    }

    public bool TryStartDash()
    {
        if (!isPermitted || linkedStateMachine.currentState == PlayerStates.State.KnockBack)
            return false;
        // other conditions
        if (linkedStateMachine.currentState == PlayerStates.State.Dash || linkedPhysics.isTouchingWall)
            return false;

        linkedStateMachine.ChangeState(PlayerStates.State.Dash);
        linkedPhysics.DisableGravity();
        linkedPhysics.ResetVelocity();

        if (player.facingRight)
        {
            linkedPhysics.rb.linearVelocityX = dashForce;
        }
        else
        {
            linkedPhysics.rb.linearVelocityX -= dashForce;
        }

        dashTimer = maxDashDuration;
        return true;
    }

    public override void ProcessAbility()
    {
        dashTimer -= Time.deltaTime;
        if (linkedPhysics.isTouchingWall)
            dashTimer = -1; 
        if (dashTimer <= 0)
        {
            if (linkedPhysics.isGrounded)
            {
                linkedStateMachine.ChangeState(PlayerStates.State.Idle);
            }
            else
            {
                linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            }
        }
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(dashParameterID, linkedStateMachine.currentState == PlayerStates.State.Dash);
    }
}
