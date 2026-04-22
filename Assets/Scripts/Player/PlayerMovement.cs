using UnityEngine;
/// <summary>
/// Tick-based movement motor. Receives input through public API methods
/// (SetMoveInput, PressJump, PressDash) and simulates all physics
/// in FixedUpdate for replay-deterministic behaviour
///
/// An input-agnostic motor with a fixed-step dash state machine
/// and external lock/freeze support for knockback, death and cutscenes
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Data")]
    public PlayerData data;

    [Header("Collision checks (assigned via BodyConnector)")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform frontWallCheckPoint;
    [SerializeField] private Transform backWallCheckPoint;

    [SerializeField] private Vector2 groundCheckSize = new(0.49f, 0.03f);
    [SerializeField] private Vector2 wallCheckSize = new(0.5f, 1f);

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    // public read-only state 
    public Rigidbody2D RB { get; private set; }

    [Header("Interpolation (for moving platforms)")]
    [SerializeField] private RigidbodyInterpolation2D defaultInterpolation = RigidbodyInterpolation2D.Interpolate;
    [SerializeField] private RigidbodyInterpolation2D platformInterpolation = RigidbodyInterpolation2D.Extrapolate;

    public bool IsFacingRight { get; private set; } = true;
    public bool IsJumping { get; private set; }
    public bool IsWallJumping { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public int DashesLeft { get; private set; }
    public int AirJumpsLeft { get; private set; }

    // timers
    public float LastOnGroundTime { get; private set; } // coyote timer
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }

    public float LastPressedJumpTime { get; set; } // jump input buffer timer
    public float LastPressedDashTime { get; private set; } // dash input buffer timer

    public bool IsExternallyLocked { get { return externalLockTimer > 0f; } }

    // internals 
    private Vector2 moveInput;

    private bool isJumpCut; // variable jump height
    private bool isJumpFalling; // apex hang
    private float wallJumpTimeLeft;
    private int lastWallJumpDir;

    // dash (fixed-step state machine)
    private enum DashPhase { None, Freeze, Active, Recovery }
    private DashPhase dashPhase = DashPhase.None;

    private Vector2 dashDir;
    private float dashPhaseTimer;
    private bool dashRefillActive;
    private float dashRefillTimer;

    // external control (knockback / death / cutscenes)
    private float externalLockTimer;
    private bool externalFrozen;
    private RigidbodyConstraints2D savedConstraints;
    private float savedGravityScale;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (data == null) return;

        SetGravityScale(data.calculatedGravityScale);
        DashesLeft = data.maxDashCount;
        AirJumpsLeft = data.maxAirJumps;
        IsFacingRight = true;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
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

        if (!IsDashing && Mathf.Abs(moveInput.x) > 0.01f) // 0.01f - deadzone
            UpdateFacingDirection(moveInput.x > 0);
    }

    public void PressJump()
    {
        if (data == null) return;
        LastPressedJumpTime = data.jumpInputBufferTime;
    }

    public void ReleaseJump()
    {
        if (CanJumpCut() || CanWallJumpCut()) // if the jump is released while the character is still rising,
            isJumpCut = true; // isJumpCut is enabled - gravity will increase and the jump will be "cut off"
    }

    public void PressDash()
    {
        if (data != null)
            LastPressedDashTime = data.dashInputBufferTime;
    }

    public void UpdateFacingDirection(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            IsFacingRight = isMovingRight;
    }

    public void SetDefaultInterpolation() => RB.interpolation = defaultInterpolation;
    public void SetPlatformInterpolation() => RB.interpolation = platformInterpolation;

    // Resets the entire motor to a clean state
    public void ResetMotorState()
    {
        IsJumping = false;
        IsWallJumping = false;
        IsSliding = false;

        IsDashing = false;
        dashPhase = DashPhase.None;

        isJumpCut = false;
        isJumpFalling = false;

        wallJumpTimeLeft = 0f;
        lastWallJumpDir = 0;

        dashRefillActive = false;
        dashRefillTimer = 0f;

        LastOnGroundTime = 0f;
        LastOnWallTime = 0f;
        LastOnWallRightTime = 0f;
        LastOnWallLeftTime = 0f;

        LastPressedJumpTime = 0f;
        LastPressedDashTime = 0f;

        externalLockTimer = 0f;
        externalFrozen = false;

        if (data != null)
        {
            DashesLeft = data.maxDashCount;
            AirJumpsLeft = data.maxAirJumps;
            SetGravityScale(data.calculatedGravityScale);
        }
    }
    #endregion

    // ─────────── EXTERNAL CONTROL ────────────

    #region External Control
    // applies an instant velocity and locks the motor for a duration (used by KnockBackAbility)
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

        if (data != null) SetGravityScale(data.calculatedGravityScale);

        RB.linearVelocity = velocity;
        LastPressedJumpTime = 0f;
        LastPressedDashTime = 0f;
    }

    public void ClearExternalLock()
    {
        externalLockTimer = 0f;
    }

    // freezes/unfreezes the rigidbody completely (used by DeathAbility and Terminal pause)
    public void ExternalFreeze(bool freeze)
    {
        if (freeze && !externalFrozen)
        {
            externalFrozen = true;

            savedConstraints = RB.constraints;
            savedGravityScale = RB.gravityScale;

            RB.linearVelocity = Vector2.zero;
            RB.angularVelocity = 0f;
            RB.gravityScale = 0f;
            RB.constraints = RigidbodyConstraints2D.FreezeAll;

            externalLockTimer = float.MaxValue;
        }
        else if (!freeze && externalFrozen)
        {
            externalFrozen = false;

            RB.constraints = savedConstraints;
            RB.gravityScale = savedGravityScale;
            externalLockTimer = 0f;
        }
    }
    #endregion

    // ───────── FIXED-STEP SIMULATION ─────────

    #region Fixed-step Simulation
    private void FixedUpdate()
    {
        if (data == null) return;

        float dt = Time.fixedDeltaTime;

        // external lock: count down and skip all simulation
        if (externalLockTimer > 0f)
        {
            externalLockTimer -= dt;
            if (externalLockTimer < 0f) externalLockTimer = 0f;

            return;
        }

        // decrement environment timers  
        LastOnGroundTime -= dt;
        LastOnWallTime -= dt;
        LastOnWallRightTime -= dt;
        LastOnWallLeftTime -= dt;

        // subsystems 
        UpdateCollisions();
        UpdateDash(dt); // depends on collisions
        UpdateJump(dt); // depends on collisions and dash state
        UpdateSlide(); // depends on walls and dash/jump state

        if (!IsDashing)
        {
            ApplyGravity();

            Run(IsWallJumping ? data.wallJumpMovementLerp : 1f);

            if (IsSliding) ApplySlideForce();
        }

        // input buffer timers (decremented last, so the full buffer window is available on the tick the input arrives)
        LastPressedJumpTime = Mathf.Max(0f, LastPressedJumpTime - dt);
        LastPressedDashTime = Mathf.Max(0f, LastPressedDashTime - dt);
    }
    #endregion

    // ────────── COLLISION DETECTION ──────────

    #region Collision Detection
    private void UpdateCollisions()
    {
        if (groundCheckPoint == null || frontWallCheckPoint == null || backWallCheckPoint == null)
        {
            Debug.LogWarning($"[PlayerMovement] Null check point: ground={groundCheckPoint}, front={frontWallCheckPoint}, back={backWallCheckPoint}");
            return;
        }

        // ground — skip during upward phase of a jump to prevent instant re-grounding from an overlap at launch
        if (!IsDashing && !(IsJumping && RB.linearVelocity.y > 0))
        {
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
                LastOnGroundTime = data.coyoteTime;
        }

        // walls — always checked (needed for wall-jump and wall-slide)
        bool aHit = Physics2D.OverlapBox(frontWallCheckPoint.position, wallCheckSize, 0, groundLayer);
        bool bHit = Physics2D.OverlapBox(backWallCheckPoint.position, wallCheckSize, 0, groundLayer);

        // map front/back to world-space left/right
        bool frontIsRight = frontWallCheckPoint.position.x >= backWallCheckPoint.position.x;
        bool rightHit = frontIsRight ? aHit : bHit;
        bool leftHit = frontIsRight ? bHit : aHit;

        if (rightHit && !IsWallJumping) LastOnWallRightTime = data.coyoteTime;
        if (leftHit && !IsWallJumping) LastOnWallLeftTime = data.coyoteTime;

        LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
    }
    #endregion

    // ───────────────── DASH ──────────────────

    #region Dash
    private void UpdateDash(float dt)
    {
        // refill one charge while grounded 
        if (!IsDashing && !dashRefillActive && DashesLeft < data.maxDashCount && LastOnGroundTime > 0)
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
                DashesLeft = Mathf.Min(data.maxDashCount, DashesLeft + 1);
            }
        }

        // advance active dash
        if (IsDashing)
        {
            AdvanceDashPhase(dt);
            return;
        }

        // try to start a new dash
        if (LastPressedDashTime > 0 && DashesLeft > 0)
        {
            BeginDash();
        }
    }

    private void BeginDash()
    {
        LastPressedDashTime = 0f;
        DashesLeft--;

        // cancel all other movement states
        IsJumping = false;
        IsWallJumping = false;
        IsSliding = false;
        isJumpCut = false;
        isJumpFalling = false;

        // clear environment timers so coyote/wall jump can't fire mid-dash
        LastOnGroundTime = 0;
        LastOnWallTime = 0;
        LastOnWallLeftTime = 0;
        LastOnWallRightTime = 0;
        LastPressedJumpTime = 0;

        IsDashing = true;
        dashPhase = DashPhase.Freeze;
        dashPhaseTimer = data.dashFreezeTime;
        dashDir = Vector2.zero;

        SetGravityScale(0);
        RB.linearVelocity = Vector2.zero;
    }

    private void AdvanceDashPhase(float dt)
    {
        dashPhaseTimer -= dt;

        switch (dashPhase)
        {
            case DashPhase.Freeze:
                // player is frozen; direction is committed when the freeze ends
                if (dashPhaseTimer > 0f) break;

                dashDir = (moveInput != Vector2.zero)
                    ? moveInput.normalized
                    : (IsFacingRight ? Vector2.right : Vector2.left);

                if (Mathf.Abs(dashDir.x) > 0.01f)
                    IsFacingRight = dashDir.x > 0;

                dashPhase = DashPhase.Active;
                dashPhaseTimer = data.dashActiveTime;
                RB.linearVelocity = dashDir * data.dashSpeed;
                break;

            case DashPhase.Active:
                SetGravityScale(0);
                RB.linearVelocity = dashDir * data.dashSpeed;

                if (dashPhaseTimer > 0f) break;

                dashPhase = DashPhase.Recovery;
                dashPhaseTimer = data.dashRecoveryTime;
                RB.linearVelocity = new Vector2(
                    data.dashRecoverySpeed.x * dashDir.x,
                    data.dashRecoverySpeed.y * dashDir.y);
                break;

            case DashPhase.Recovery:
                if (dashPhaseTimer > 0f) break;

                dashPhase = DashPhase.None;
                IsDashing = false;
                break;
        }
    }
    #endregion

    // ──────────── JUMP / WALL JUMP ───────────

    #region Jump and Wall Jump
    private void UpdateJump(float dt)
    {
        if (IsDashing)
            return;

        // detect transition from rising to falling
        if (IsJumping && RB.linearVelocity.y < 0f)
        {
            IsJumping = false;
            if (!IsWallJumping) isJumpFalling = true;
        }

        // wall-jump movement lock countdown
        if (IsWallJumping)
        {
            wallJumpTimeLeft -= dt;
            if (wallJumpTimeLeft <= 0f)
                IsWallJumping = false;
        }

        // reset jump flags and air jumps when safely grounded
        if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
            isJumpCut = false;
            isJumpFalling = false;
            AirJumpsLeft = data.maxAirJumps;
        }

        // try ground jump (coyote time)
        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            IsWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
            ExecuteJump();
        }
        // try wall jump
        else if (CanWallJump() && LastPressedJumpTime > 0)
        {
            IsWallJumping = true;
            IsJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            wallJumpTimeLeft = data.wallJumpMovementLockTime;

            lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

            ExecuteWallJump(lastWallJumpDir);
        }
        // try air jump (double jump, triple jump, etc.)
        else if (CanAirJump() && LastPressedJumpTime > 0)
        {
            AirJumpsLeft--;
            IsJumping = true;
            IsWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
            ExecuteJump();
        }
    }

    // jump impulse
    private void ExecuteJump()
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = data.calculatedJumpForce;

        // compensate existing downward velocity so the jump always reaches the configured height
        if (RB.linearVelocity.y < 0) force -= RB.linearVelocity.y;

        RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void ExecuteWallJump(int dir)
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;

        Vector2 force = new Vector2(data.wallJumpForce.x * dir, data.wallJumpForce.y);

        // compensate existing velocity so the impulse is consistent
        if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= RB.linearVelocity.x;

        if (RB.linearVelocity.y < 0)
            force.y -= RB.linearVelocity.y;

        RB.AddForce(force, ForceMode2D.Impulse);
    }

    #endregion

    // ───────────────── SLIDE ─────────────────

    #region Slide
    private void UpdateSlide()
    {
        if (IsDashing) { IsSliding = false; return; }

        bool pushingIntoLeftWall = LastOnWallLeftTime > 0 && moveInput.x < 0;
        bool pushingIntoRightWall = LastOnWallRightTime > 0 && moveInput.x > 0;

        IsSliding = CanSlide() && (pushingIntoLeftWall || pushingIntoRightWall);
    }

    private void ApplySlideForce()
    {
        // kill any upward velocity immediately
        if (RB.linearVelocity.y > 0)
            RB.AddForce(-RB.linearVelocity.y * Vector2.up, ForceMode2D.Impulse);

        float speedDif = data.wallSlideSpeed - RB.linearVelocity.y;
        float force = speedDif * data.wallSlideAcceleration;

        // clamp so the applied force never overshoots the target speed in one tick
        float maxForce = Mathf.Abs(speedDif) / Time.fixedDeltaTime;
        force = Mathf.Clamp(force, -maxForce, maxForce);

        RB.AddForce(force * Vector2.up);
    }

    #endregion

    // ──────────────── GRAVITY ────────────────

    #region Gravity
    private void ApplyGravity()
    {
        if (IsSliding)
        {
            SetGravityScale(0);
            return;
        }

        float velY = RB.linearVelocity.y;
        bool nearApex = (IsJumping || IsWallJumping || isJumpFalling)
                        && Mathf.Abs(velY) < data.apexHangVelocityThreshold;

        if (velY < 0 && moveInput.y < 0)
        {
            // fast fall (player holding down)
            SetGravityScale(data.calculatedGravityScale * data.fastFallGravityMultiplier);
            ClampFallSpeed(data.maxFastFallSpeed);
        }
        else if (isJumpCut)
        {
            // short hop (jump released early)
            SetGravityScale(data.calculatedGravityScale * data.jumpReleaseGravityMultiplier);
            ClampFallSpeed(data.maxNormalFallSpeed);
        }
        else if (nearApex)
        {
            // apex hang
            SetGravityScale(data.calculatedGravityScale * data.apexHangGravityMultiplier);
        }
        else if (velY < 0)
        {
            // normal fall
            SetGravityScale(data.calculatedGravityScale * data.fallGravityMultiplier);
            ClampFallSpeed(data.maxNormalFallSpeed);
        }
        else
        {
            // rising normally
            SetGravityScale(data.calculatedGravityScale);
        }
    }

    private void ClampFallSpeed(float maxSpeed)
    {
        if (RB.linearVelocity.y < -maxSpeed)
            RB.linearVelocity = new Vector2(RB.linearVelocity.x, -maxSpeed);
    }

    #endregion

    // ────────── HORIZONTAL MOVEMENT ──────────

    #region Horizontal Movement
    private void Run(float lerpAmount)
    {
        float targetSpeed = moveInput.x * data.maxRunSpeed;
        targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

        // pick acceleration or deceleration rate
        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate;

        if (LastOnGroundTime > 0)
            accelRate = accelerating ? data.calculatedGroundAccelerationForce : data.calculatedGroundDecelerationForce;
        else
            accelRate = accelerating
                ? data.calculatedGroundAccelerationForce * data.airAccelerationMultiplier
                : data.calculatedGroundDecelerationForce * data.airDecelerationMultiplier;

        // apex hang: boost horizontal control near the peak of a jump
        bool nearApex = (IsJumping || IsWallJumping || isJumpFalling)
                        && Mathf.Abs(RB.linearVelocity.y) < data.apexHangVelocityThreshold;
        if (nearApex)
        {
            accelRate *= data.apexHangAccelerationMultiplier;
            targetSpeed *= data.apexHangMaxSpeedMultiplier;
        }

        // preserve momentum: don't slow down if already faster than target
        if (data.preserveMomentum 
            && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed)
            && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed)
            && accelerating
            && LastOnGroundTime <= 0)
        {
            accelRate = 0;
        }

        float speedDif = targetSpeed - RB.linearVelocity.x;

        RB.AddForce(speedDif * accelRate * Vector2.right, ForceMode2D.Force);
    }

    #endregion

    // ──────────────── HELPERS ────────────────

    #region Helpers
    public void SetGravityScale(float scale)
    {
        RB.gravityScale = scale;
    }

    // condition checks

    private bool CanJump() => LastOnGroundTime > 0 && !IsJumping;
    private bool CanAirJump() => AirJumpsLeft > 0 && !IsJumping && LastOnGroundTime <= 0;
    private bool CanJumpCut() => IsJumping && RB.linearVelocity.y > 0;
    private bool CanWallJumpCut() => IsWallJumping && RB.linearVelocity.y > 0;

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0
               && LastOnWallTime > 0
               && LastOnGroundTime <= 0
               && (!IsWallJumping
                || (LastOnWallRightTime > 0 && lastWallJumpDir == 1)
                || (LastOnWallLeftTime > 0 && lastWallJumpDir == -1));
    }

    public bool CanSlide()
    {
        return LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0;
    }

    #endregion

    // ──────────────── EDITOR ─────────────────

    #region Editor
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
    #endregion
}