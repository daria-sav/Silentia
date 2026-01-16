using UnityEngine;
using UnityEngine.InputSystem;

public class WallJumpAbility : BaseAbility
{
    public InputActionReference wallJumpActionRef;
    [SerializeField] private Vector2 wallJumpForce;
    [SerializeField] private float wallJumpMaxTime; 
    private float wallJumpMinimumTime;
    private float wallJumpTimer;

    private void OnEnable()
    {
        wallJumpActionRef.action.performed += TryToWallJump;
    }

    private void OnDisable()
    {
        wallJumpActionRef.action.performed -= TryToWallJump;
    }

    protected override void Initialization()
    {
        base.Initialization();
        wallJumpTimer = wallJumpMaxTime;
    }

    private void TryToWallJump(InputAction.CallbackContext value) 
    { 
        if (!isPermitted) 
            return;

        if (EvaluateWallJumpConditions()) 
        {
            linkedStateMachine.ChangeState(PlayerStates.State.WallJump);
            wallJumpTimer = wallJumpMaxTime;
            wallJumpMinimumTime = 0.15f;
            // flip the player and then add the velocity
            player.ForceFlip();
            if(player.facingRight)
            {
                linkedPhysics.rb.linearVelocity = new Vector2(wallJumpForce.x, wallJumpForce.y);
            }
            else
            {
                 linkedPhysics.rb.linearVelocity = new Vector2(-wallJumpForce.x, wallJumpForce.y);
            }
        }
    }

    public override void ProcessAbility()
    {
        wallJumpTimer -= Time.deltaTime;
        wallJumpMinimumTime -= Time.deltaTime;

        if (wallJumpTimer <= 0)
        {
            if (linkedPhysics.isGrounded)
            {
                linkedStateMachine.ChangeState(PlayerStates.State.Idle);
            }
            else
            {
                linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            }
            return;
        }

        if (wallJumpMinimumTime <= 0 && linkedPhysics.isTouchingWall)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.WallSlide);
            wallJumpTimer = -1;
        }
    }

    private bool EvaluateWallJumpConditions() 
    { 
        if (linkedPhysics.isGrounded || !linkedPhysics.isTouchingWall) 
            return false;

        return true;
    }
}