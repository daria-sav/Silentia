using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Data")]
    public PlayerData data;

    [Header("Checks (assigned from BodyMarkers via BodyConnector)")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform frontWallCheckPoint;
    [SerializeField] private Transform backWallCheckPoint;

    [SerializeField] private Vector2 groundCheckSize = new(0.49f, 0.03f);
    [SerializeField] private Vector2 wallCheckSize = new(0.5f, 1f);

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    public Rigidbody2D RB { get; private set; }

    // Public state
    public bool IsFacingRight { get; private set; } = true;

    public bool IsJumping { get; private set; }
    public bool IsWallJumping { get; private set; }
    public bool IsSliding { get; private set; }

    public bool IsDashing { get; private set; }
    public int DashesLeft { get; private set; }

    // Timers
    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }

    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }

    // Internals
    private Vector2 moveInput;

    private bool isJumpCut;
    private bool isJumpFalling;

    private float wallJumpTimeLeft;
    private int lastWallJumpDir;

    // Dash internals (fixed-step state machine)
    private enum DashPhase { None, Sleep, Attack, End }
    private DashPhase dashPhase = DashPhase.None;

    private Vector2 dashDir;
    private float dashSleepTimer;
    private float dashAttackTimer;
    private float dashEndTimer;

    private bool dashRefillActive;
    private float dashRefillTimer;

    // External control (KnockBack / Death / Cutscenes) 
    public bool IsExternallyLocked => externalLockTimer > 0f;

    private float externalLockTimer;

    // External freeze (Death / cutscenes)
    private bool externalFrozen;
    private RigidbodyConstraints2D savedConstraints;
    private float savedGravityScale;

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (data != null)
        {
            SetGravityScale(data.gravityScale);
            DashesLeft = Mathf.Max(0, data.dashAmount);
        }

        IsFacingRight = true;
    }

    // --------- API ----------
    public void SetChecks(Transform ground, Transform frontWall, Transform backWall)
    {
        groundCheckPoint = ground;
        frontWallCheckPoint = frontWall;
        backWallCheckPoint = backWall;
    }

    public void SetMoveInput(float x, float y = 0f)
    {
        moveInput.x = Mathf.Clamp(x, -1f, 1f);
        moveInput.y = Mathf.Clamp(y, -1f, 1f);

        if (!IsDashing && Mathf.Abs(moveInput.x) > 0.01f)
            CheckDirectionToFace(moveInput.x > 0);
    }

    public void PressJump()
    {
        if (data == null) return;
        LastPressedJumpTime = data.jumpInputBufferTime;
    }

    public void ReleaseJump()
    {
        if (CanJumpCut() || CanWallJumpCut())
            isJumpCut = true;
    }

    public void PressDash(Vector2 _)
    {
        if (data == null) return;
        LastPressedDashTime = data.dashInputBufferTime;
    }

    // --------- Fixed-step simulation ----------
    private void FixedUpdate()
    {
        if (data == null) return;

        //Debug.Log($"[PM] object='{gameObject.name}' moveInput={moveInput} lockTimer={externalLockTimer} frozen={externalFrozen}");

        float dt = Time.fixedDeltaTime;

        if (externalLockTimer > 0f)
        {
            externalLockTimer -= dt;
            if (externalLockTimer < 0f) externalLockTimer = 0f;

            return;
        }

        // decrement environment timers first 
        LastOnGroundTime -= dt;
        LastOnWallTime -= dt;
        LastOnWallRightTime -= dt;
        LastOnWallLeftTime -= dt;

        DoDashChecks(dt);

        // Collision checks 
        DoCollisionChecks();

        // Jump/WallJump
        DoJumpChecks(dt);

        // Slide
        DoSlideChecks();

        // Gravity
        ApplyGravity();

        // Movement forces
        if (IsDashing)
        {
            DecrementPressTimers(dt);
            return;
        }

        if (IsWallJumping) Run(data.wallJumpRunLerp);
        else Run(1f);

        if (IsSliding) Slide();

        DecrementPressTimers(dt);
    }

    private void DecrementPressTimers(float dt)
    {
        LastPressedJumpTime -= dt;
        LastPressedDashTime -= dt;

        if (LastPressedJumpTime < 0) LastPressedJumpTime = 0;
        if (LastPressedDashTime < 0) LastPressedDashTime = 0;
    }

    // ---------------- DASH ----------------
    private void DoDashChecks(float dt)
    {
        // Refill +1 
        if (!IsDashing && !dashRefillActive && DashesLeft < data.dashAmount && LastOnGroundTime > 0)
        {
            dashRefillActive = true;
            dashRefillTimer = data.dashRefillTime;
        }

        if (dashRefillActive)
        {
            dashRefillTimer -= dt;
            if (dashRefillTimer <= 0f)
            {
                dashRefillActive = false;
                DashesLeft = Mathf.Min(data.dashAmount, DashesLeft + 1);
            }
        }

        if (IsDashing)
        {
            switch (dashPhase)
            {
                case DashPhase.Sleep:
                    dashSleepTimer -= dt;
                    SetGravityScale(0);
                    RB.linearVelocity = Vector2.zero;

                    if (dashSleepTimer <= 0f)
                    {
                        // DIR AFTER SLEEP 
                        dashDir = (moveInput != Vector2.zero) ? moveInput : (IsFacingRight ? Vector2.right : Vector2.left);
                        dashDir.Normalize();

                        if (Mathf.Abs(dashDir.x) > 0.01f)
                            IsFacingRight = dashDir.x > 0;

                        dashPhase = DashPhase.Attack;
                        dashAttackTimer = data.dashAttackTime;
                        RB.linearVelocity = dashDir * data.dashSpeed;
                    }
                    break;

                case DashPhase.Attack:
                    dashAttackTimer -= dt;
                    SetGravityScale(0);
                    RB.linearVelocity = dashDir * data.dashSpeed;

                    if (dashAttackTimer <= 0f)
                    {
                        dashPhase = DashPhase.End;
                        dashEndTimer = data.dashEndTime;

                        // dashEndSpeed * dir.normalized (component-wise)
                        RB.linearVelocity = new Vector2(
                            data.dashEndSpeed.x * dashDir.x,
                            data.dashEndSpeed.y * dashDir.y
                        );
                    }
                    break;

                case DashPhase.End:
                    dashEndTimer -= dt;
                    if (dashEndTimer <= 0f)
                    {
                        dashPhase = DashPhase.None;
                        IsDashing = false;
                    }
                    break;
            }

            return;
        }

        if (LastPressedDashTime > 0 && DashesLeft > 0)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        LastPressedDashTime = 0f;
        DashesLeft = Mathf.Max(0, DashesLeft - 1);

        IsJumping = false;
        IsWallJumping = false;
        IsSliding = false;

        isJumpCut = false;
        isJumpFalling = false;

        LastOnGroundTime = 0;
        LastOnWallTime = 0;
        LastOnWallLeftTime = 0;
        LastOnWallRightTime = 0;
        LastPressedJumpTime = 0;

        IsDashing = true;
        dashPhase = DashPhase.Sleep;
        dashSleepTimer = data.dashSleepTime;

        dashDir = Vector2.zero;

        SetGravityScale(0);
        RB.linearVelocity = Vector2.zero;
    }

    // ---------------- COLLISIONS (variant B) ----------------
    private void DoCollisionChecks()
    {
        if (groundCheckPoint == null || frontWallCheckPoint == null || backWallCheckPoint == null)
            return;

        if (IsDashing || IsJumping)
            return;

        // Ground
        if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            LastOnGroundTime = data.coyoteTime;

        bool aHit = Physics2D.OverlapBox(frontWallCheckPoint.position, wallCheckSize, 0, groundLayer);
        bool bHit = Physics2D.OverlapBox(backWallCheckPoint.position, wallCheckSize, 0, groundLayer);

        bool frontIsRight = frontWallCheckPoint.position.x >= backWallCheckPoint.position.x;

        bool rightHit = frontIsRight ? aHit : bHit;
        bool leftHit = frontIsRight ? bHit : aHit;

        if (rightHit && !IsWallJumping) LastOnWallRightTime = data.coyoteTime;
        if (leftHit && !IsWallJumping) LastOnWallLeftTime = data.coyoteTime;

        LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
    }

    // ---------------- JUMP ----------------
    private void DoJumpChecks(float dt)
    {
        if (IsDashing)
            return;

        if (IsJumping && RB.linearVelocity.y < 0)
        {
            IsJumping = false;
            if (!IsWallJumping) isJumpFalling = true;
        }

        if (IsWallJumping)
        {
            wallJumpTimeLeft -= dt;
            if (wallJumpTimeLeft <= 0f)
                IsWallJumping = false;
        }

        if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
            isJumpCut = false;
            isJumpFalling = false;
        }

        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            IsWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
            Jump();
        }
        else if (CanWallJump() && LastPressedJumpTime > 0)
        {
            IsWallJumping = true;
            IsJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            wallJumpTimeLeft = data.wallJumpTime;

            lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

            WallJump(lastWallJumpDir);
        }
    }

    // ---------------- SLIDE ----------------
    private void DoSlideChecks()
    {
        if (IsDashing) { IsSliding = false; return; }

        if (CanSlide() && ((LastOnWallLeftTime > 0 && moveInput.x < 0) || (LastOnWallRightTime > 0 && moveInput.x > 0)))
            IsSliding = true;
        else
            IsSliding = false;
    }

    // ---------------- GRAVITY ----------------
    private void ApplyGravity()
    {
        if (IsDashing && dashPhase != DashPhase.End)
        {
            SetGravityScale(0);
            return;
        }

        if (IsSliding)
        {
            SetGravityScale(0);
            return;
        }

        if (RB.linearVelocity.y < 0 && moveInput.y < 0)
        {
            SetGravityScale(data.gravityScale * data.fastFallGravityMult);
            RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -data.maxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            SetGravityScale(data.gravityScale * data.jumpCutGravityMult);
            RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -data.maxFallSpeed));
        }
        else if ((IsJumping || IsWallJumping || isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            SetGravityScale(data.gravityScale * data.jumpHangGravityMult);
        }
        else if (RB.linearVelocity.y < 0)
        {
            SetGravityScale(data.gravityScale * data.fallGravityMult);
            RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -data.maxFallSpeed));
        }
        else
        {
            SetGravityScale(data.gravityScale);
        }
    }

    // ---------------- RUN ----------------
    private void Run(float lerpAmount)
    {
        float targetSpeed = moveInput.x * data.runMaxSpeed;
        targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

        float accelRate;
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.runAccelAmount * data.accelInAir : data.runDeccelAmount * data.deccelInAir;

        if ((IsJumping || IsWallJumping || isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            targetSpeed *= data.jumpHangMaxSpeedMult;
        }

        if (data.doConserveMomentum &&
            Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) &&
            Mathf.Abs(targetSpeed) > 0.01f &&
            LastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        float speedDif = targetSpeed - RB.linearVelocity.x;
        float movement = speedDif * accelRate;

        RB.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    // ---------------- SLIDE FORCE ----------------
    private void Slide()
    {
        if (RB.linearVelocity.y > 0)
            RB.AddForce(-RB.linearVelocity.y * Vector2.up, ForceMode2D.Impulse);

        float speedDif = data.slideSpeed - RB.linearVelocity.y;
        float movement = speedDif * data.slideAccel;

        movement = Mathf.Clamp(
            movement,
            -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime),
            Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime)
        );

        RB.AddForce(movement * Vector2.up);
    }

    // ---------------- JUMP IMPULSE ----------------
    private void Jump()
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = data.jumpForce;
        if (RB.linearVelocity.y < 0) force -= RB.linearVelocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void WallJump(int dir)
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;

        Vector2 force = new Vector2(data.wallJumpForce.x, data.wallJumpForce.y);
        force.x *= dir;

        if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= RB.linearVelocity.x;

        if (RB.linearVelocity.y < 0)
            force.y -= RB.linearVelocity.y;

        RB.AddForce(force, ForceMode2D.Impulse);
    }

    // ---------------- HELPERS ----------------
    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            IsFacingRight = isMovingRight;
    }

    private bool CanJump() => LastOnGroundTime > 0 && !IsJumping;

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0 &&
               LastOnWallTime > 0 &&
               LastOnGroundTime <= 0 &&
               (!IsWallJumping ||
                (LastOnWallRightTime > 0 && lastWallJumpDir == 1) ||
                (LastOnWallLeftTime > 0 && lastWallJumpDir == -1));
    }

    private bool CanJumpCut() => IsJumping && RB.linearVelocity.y > 0;
    private bool CanWallJumpCut() => IsWallJumping && RB.linearVelocity.y > 0;

    public bool CanSlide()
    {
        return LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0;
    }
    public void ExternalImpulse(Vector2 velocity, float lockSeconds)
    {
        externalLockTimer = Mathf.Max(externalLockTimer, lockSeconds);

        IsDashing = false;
        dashPhase = DashPhase.None;
        IsJumping = false;
        IsWallJumping = false;
        IsSliding = false;

        isJumpCut = false;
        isJumpFalling = false;

        if (data != null) SetGravityScale(data.gravityScale);

        RB.linearVelocity = velocity;

        LastPressedJumpTime = 0f;
        LastPressedDashTime = 0f;
    }

    public void ClearExternalLock()
    {
        externalLockTimer = 0f;
    }

    public void ExternalFreeze(bool freeze)
    {
        if (freeze)
        {
            if (externalFrozen) return;
            externalFrozen = true;

            savedConstraints = RB.constraints;
            savedGravityScale = RB.gravityScale;

            RB.linearVelocity = Vector2.zero;
            RB.angularVelocity = 0f;
            RB.gravityScale = 0f;

            RB.constraints = RigidbodyConstraints2D.FreezeAll;

            externalLockTimer = 999999f;
        }
        else
        {
            if (!externalFrozen) return;
            externalFrozen = false;

            RB.constraints = savedConstraints;
            RB.gravityScale = savedGravityScale;

            externalLockTimer = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }

        if (frontWallCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(frontWallCheckPoint.position, wallCheckSize);
        }

        if (backWallCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(backWallCheckPoint.position, wallCheckSize);
        }
    }
#endif
}