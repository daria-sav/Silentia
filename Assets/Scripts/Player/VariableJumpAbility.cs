using UnityEngine;
using UnityEngine.InputSystem;

public class VariableJumpAbility : BaseAbility
{
    public InputActionReference jumpActionRef;
    [SerializeField] private float jumpForce;
    [SerializeField] private float airSpeed;
    [SerializeField] private float minimumAirTime;
    private float startMinimumAirTime;

    [SerializeField] private float setMaxJumpTime;
    private float jumpTimer;
    private bool isJumping;
    [SerializeField] private float gravityDivider;

    private string jumpAnimParameterName = "Jump";
    private string ySpeedAnimParameterName = "ySpeed";
    private int jumpParameterID;
    private int ySpeedParameterID;

    protected override void Initialization()
    {
        base.Initialization();
        startMinimumAirTime = minimumAirTime;
        jumpParameterID = Animator.StringToHash(jumpAnimParameterName);
        ySpeedParameterID = Animator.StringToHash(ySpeedAnimParameterName);
    }

    private void OnEnable()
    {
        jumpActionRef.action.performed += TryToJump;
        jumpActionRef.action.canceled += StopJump;
    }

    private void OnDisable()
    {
        jumpActionRef.action.performed -= TryToJump;
        jumpActionRef.action.canceled -= StopJump;
    }

    public override void ProcessAbility()
    {
        player.Flip();
        minimumAirTime -= Time.deltaTime;

        if (isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                isJumping = false;
            }
        }

        if (linkedPhysics.isGrounded && minimumAirTime <= 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
        if (!linkedPhysics.isGrounded && linkedPhysics.isTouchingWall)
        {
            if (linkedPhysics.rb.linearVelocityY < 0)
            {
                linkedStateMachine.ChangeState(PlayerStates.State.WallSlide);
            }
        }
    }

    public override void ProcessFixedAbility()
    {
        if (!linkedPhysics.isGrounded)
        {
            if (isJumping)
                linkedPhysics.rb.linearVelocity = new Vector2(airSpeed * linkedInput.horizontalInput, jumpForce);
            else
                linkedPhysics.rb.linearVelocity = new Vector2(airSpeed * linkedInput.horizontalInput, Mathf.Clamp(linkedPhysics.rb.linearVelocityY, -10, jumpForce));

        }

        if (linkedPhysics.rb.linearVelocityY < 0)
        {
            linkedPhysics.rb.gravityScale = linkedPhysics.GetGravity() / gravityDivider;
        }
    }

    private void TryToJump(InputAction.CallbackContext value)
    {
        if (!isPermitted)
            return;

        if (linkedPhysics.coyoteTimer > 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            linkedPhysics.rb.linearVelocity = new Vector2(airSpeed * linkedInput.horizontalInput, jumpForce);
            minimumAirTime = startMinimumAirTime;
            linkedPhysics.coyoteTimer = -1;

            isJumping = true;
            jumpTimer = setMaxJumpTime;
        }
    }

    private void StopJump(InputAction.CallbackContext value)
    {
        isJumping = false;
    }

    public override void ExitAbility()
    {
        linkedPhysics.EnableGravity();
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(jumpParameterID, linkedStateMachine.currentState == PlayerStates.State.Jump || linkedStateMachine.currentState == PlayerStates.State.WallJump);
        linkedAnimator.SetFloat(ySpeedParameterID, linkedPhysics.rb.linearVelocityY);
    }
}
