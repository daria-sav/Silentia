using UnityEngine;

public class MultipleJumpAbility : BaseAbility
{
    //public InputActionReference jumpActionRef;

    [SerializeField] private int maxNumberOfJumps;
    private int numberOfJumps;
    private bool canActivateAdditionalJumps;

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

    private bool leftGroundAfterJump;

    public void SetJumpForce(float value) => jumpForce = value;
    public void SetAirSpeed(float value) => airSpeed = value;
    public void SetGravityDivider(float value) => gravityDivider = value;

    protected override void Initialization()
    {
        base.Initialization();
        startMinimumAirTime = minimumAirTime;
        numberOfJumps = maxNumberOfJumps;
        jumpParameterID = Animator.StringToHash(jumpAnimParameterName);
        ySpeedParameterID = Animator.StringToHash(ySpeedAnimParameterName);
    }

    //private void OnEnable()
    //{
    //    jumpActionRef.action.performed += TryToJump;
    //    jumpActionRef.action.canceled += StopJump;
    //}

    //private void OnDisable()
    //{
    //    jumpActionRef.action.performed -= TryToJump;
    //    jumpActionRef.action.canceled -= StopJump;
    //}

    public override void ProcessAbility()
    {
        player.Flip();
        //minimumAirTime -= Time.deltaTime;
        if (!linkedPhysics.isGrounded)
            leftGroundAfterJump = true;

        if (isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                isJumping = false;
            }
        }

        if (!linkedInput.jumpHeld)
            isJumping = false;

        if (linkedPhysics.isGrounded && leftGroundAfterJump && minimumAirTime <= 0)
        {
            if (linkedInput.horizontalInput != 0)
                linkedStateMachine.ChangeState(PlayerStates.State.Walk);
            else
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

        minimumAirTime -= Time.fixedDeltaTime;
    }

    public bool TryToJump()
    {
        Debug.Log($"[TRY JUMP {gameObject.name}] grounded={linkedPhysics.isGrounded} coyote={linkedPhysics.coyoteTimer:F2} num={numberOfJumps} canAdd={canActivateAdditionalJumps}");

        if (!isPermitted || linkedStateMachine.currentState == PlayerStates.State.KnockBack)
            return false;

        if (linkedPhysics.isGrounded || linkedPhysics.coyoteTimer > 0)
        {
            var before = linkedStateMachine.currentState;
            var beforeVel = linkedPhysics.rb.linearVelocity;

            bool changed = linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            leftGroundAfterJump = false;
            minimumAirTime = startMinimumAirTime;


            linkedPhysics.rb.linearVelocity = new Vector2(airSpeed * linkedInput.horizontalInput, jumpForce);
            minimumAirTime = startMinimumAirTime;
            linkedPhysics.coyoteTimer = -1;

            isJumping = true;
            jumpTimer = setMaxJumpTime;
            numberOfJumps = maxNumberOfJumps;

            canActivateAdditionalJumps = true;
            numberOfJumps -= 1;

            return true;
        }

        if (numberOfJumps > 0 && canActivateAdditionalJumps)
        {
            linkedPhysics.EnableGravity();
            linkedPhysics.rb.linearVelocity = new Vector2(airSpeed * linkedInput.horizontalInput, jumpForce); // change force for second jump?
            minimumAirTime = startMinimumAirTime;
            linkedPhysics.coyoteTimer = -1;

            isJumping = true;
            jumpTimer = setMaxJumpTime;

            canActivateAdditionalJumps = true;
            numberOfJumps -= 1;
            return true;
        }
        //canActivateAdditionalJumps = false;
        return false;
    }

    //private void StopJump(InputAction.CallbackContext value)
    //{
    //    isJumping = false;
    //}

    public override void ExitAbility()
    {
        linkedPhysics.EnableGravity();
        canActivateAdditionalJumps = false;
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(jumpParameterID, linkedStateMachine.currentState == PlayerStates.State.Jump || linkedStateMachine.currentState == PlayerStates.State.WallJump);
        linkedAnimator.SetFloat(ySpeedParameterID, linkedPhysics.rb.linearVelocityY);
    }

    public void SetMaxJumpNumber(int maxJumps)
    { 
        maxNumberOfJumps = maxJumps;
    }

    public void ResetJumpState()
    {
        numberOfJumps = maxNumberOfJumps;
        canActivateAdditionalJumps = false;
        isJumping = false;
        minimumAirTime = startMinimumAirTime;
    }

    public int DebugMaxJumps() => maxNumberOfJumps;
    public int DebugNumJumps() => numberOfJumps;
    public bool DebugCanAdd() => canActivateAdditionalJumps;
}
