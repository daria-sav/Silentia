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
        jumpParameterID = Animator.StringToHash(jumpAnimParameterName);
        ySpeedParameterID = Animator.StringToHash(ySpeedAnimParameterName);
    }

    public bool TryToJump()
    {
        if (!isPermitted) return false;
        if (linkedMotor == null) return false;

        linkedMotor.PressJump();
        return true;
    }

    public void OnJumpReleased()
    {
        if (linkedMotor == null) return;
        linkedMotor.ReleaseJump();
    }

    public override void UpdateAnimator()
    {
        linkedAnimator.SetBool(jumpParameterID, linkedStateMachine.currentState == PlayerStates.State.Jump || linkedStateMachine.currentState == PlayerStates.State.WallJump);
        //linkedAnimator.SetFloat(ySpeedParameterID, linkedPhysics.rb.linearVelocityY);
        if (linkedMotor != null)
            linkedAnimator.SetFloat(ySpeedParameterID, linkedMotor.RB.linearVelocity.y);
    }
}
