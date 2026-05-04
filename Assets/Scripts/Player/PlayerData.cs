using UnityEngine;

// Stores all tunable movement values for the PlayerMovement controller
[CreateAssetMenu(menuName = "Player Data")]
public class PlayerData : ScriptableObject
{
    // ───────── Gravity and Falling ─────────

    [Header("Gravity and Falling")]
    [HideInInspector] public float calculatedGravityStrength;
    [HideInInspector] public float calculatedGravityScale;
    
    [Space(5)]
    // extra gravity multiplier applied while the player is falling normally
    public float fallGravityMultiplier = 1f;
    // maximum downward speed during a normal fall (terminal velocity)
    public float maxNormalFallSpeed; 

    [Space(5)]
    // extra gravity multiplier applied when the player holds down to fall faster
    public float fastFallGravityMultiplier = 1.5f;
    // maximum downward speed during a fast fall
    public float maxFastFallSpeed;

    // ───────── Horizontal Movement ─────────

    [Space(20)]
    [Header("Horizontal Movement")]

    // maximum horizontal speed the player tries to reach
    public float maxRunSpeed;
    // how quickly the player speeds up on the ground
    public float groundAcceleration;
    
    [HideInInspector] public float calculatedGroundAccelerationForce;
    // how quickly the player slows down on the ground
    public float groundDeceleration;
    
    [HideInInspector] public float calculatedGroundDecelerationForce;

    [Space(5)]
    // multiplier applied to ground acceleration while airborne
    [Range(0f, 1)] public float airAccelerationMultiplier = 1f;
    // multiplier applied to ground deceleration while airborne
    [Range(0f, 1)] public float airDecelerationMultiplier = 1f;
    

    [Space(5)]
    // keeps some existing horizontal momentum in situations where forcing the controller back to the target speed would feel too stiff
    public bool preserveMomentum = true;

    // ───────────── Jump Shape ──────────────
    
    [Space(20)]
    [Header("Jump Shape")]

    // target jump height used to derive gravity and initial jump force
    public float desiredJumpHeight;
    // time from jump start to the highest point of the jump
    public float timeToJumpApex;
    // extra jumps available in the air (0 = single jump, 1 = double jump, etc.)
    public int maxAirJumps = 0;

    [HideInInspector] public float calculatedJumpForce;

    // ──────────── Jump Behaviour ───────────
    
    [Header("Jump Behaviour")]

    // extra gravity used when the player releases jump early, which creates a shorter jump instead of always forcing full height
    public float jumpReleaseGravityMultiplier;
    // reduced gravity near the jump apex to create a brief "hang" feeling
    [Range(0f, 1)] public float apexHangGravityMultiplier;
    // vertical speed threshold below which the player is considered near the apex
    public float apexHangVelocityThreshold;
    

    [Space(1f)]
    // horizontal acceleration bonus while near the jump apex
    public float apexHangAccelerationMultiplier = 1f;
    // horizontal max-speed bonus while near the jump apex
    public float apexHangMaxSpeedMultiplier = 1f;

    // ────────────── Wall Jump ──────────────

    [Header("Wall Jump")]

    // horizontal and vertical impulse applied during a wall jump
    public Vector2 wallJumpForce;

    [Space(5)]
    // how strongly normal input affects movement right after a wall jump (lower = wall jump direction preserved longer)
    [Range(0f, 1f)] public float wallJumpMovementLerp;
    // duration after a wall jump during which movement control is partially limited
    [Range(0f, 1.5f)] public float wallJumpMovementLockTime;

    // ───────────── Wall Slide ──────────────

    [Space(20)]
    [Header("Wall Slide")]

    // target downward speed while sliding on a wall
    public float wallSlideSpeed;
    // how quickly the player reaches the wall slide speed
    public float wallSlideAcceleration;

    // ──────────── Input Assists ────────────

    [Header("Input Assists")]
    // grace period after leaving the ground during which jump is still allowed
    [Range(0.01f, 0.5f)] public float coyoteTime;
    // grace period after pressing jump early — the input is remembered and fires when possible
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime;

    // ───────────────── Dash ────────────────

    [Space(20)]
    [Header("Dash")]

    // number of dashes available before they need to be refilled
    public int maxDashCount;
    // speed during the active dash phase
    public float dashSpeed;
    // short freeze before the dash moves — gives the player a moment to commit direction
    public float dashFreezeTime;

    [Space(5)]
    // duration of the main dash movement
    public float dashActiveTime;

    [Space(5)]
    // short recovery window after the active dash ends
    public float dashRecoveryTime;
    // velocity used while easing out of the dash
    public Vector2 dashRecoverySpeed;

    [Space(5)]
    // time needed before a dash charge can be restored
    public float dashRefillTime;

    [Space(5)]
    // grace period after pressing dash early during which the input is remembered
    [Range(0.01f, 0.5f)] public float dashInputBufferTime;

    private const float FIXED_UPDATES_PER_SECOND = 50f;
    private void OnValidate()
    {
        Recalculate();
    }

    private void OnEnable()
    {
        Recalculate();
    }
    private void Recalculate()
    {
        #region Sanitize user-facing values
        desiredJumpHeight = Mathf.Max(0.01f, desiredJumpHeight);
        timeToJumpApex = Mathf.Max(0.01f, timeToJumpApex);

        maxRunSpeed = Mathf.Max(0.01f, maxRunSpeed);
        groundAcceleration = Mathf.Clamp(groundAcceleration, 0.01f, maxRunSpeed);
        groundDeceleration = Mathf.Clamp(groundDeceleration, 0.01f, maxRunSpeed);

        maxNormalFallSpeed = Mathf.Max(0.01f, maxNormalFallSpeed);
        maxFastFallSpeed = Mathf.Max(maxNormalFallSpeed, maxFastFallSpeed);

        wallSlideAcceleration = Mathf.Max(0.01f, wallSlideAcceleration);

        maxDashCount = Mathf.Max(0, maxDashCount);
        dashSpeed = Mathf.Max(0.01f, dashSpeed);
        dashFreezeTime = Mathf.Max(0f, dashFreezeTime);
        dashActiveTime = Mathf.Max(0.01f, dashActiveTime);
        dashRecoveryTime = Mathf.Max(0f, dashRecoveryTime);
        dashRefillTime = Mathf.Max(0f, dashRefillTime);
        #endregion

        // gravity = - (2 * height) / time^2
        calculatedGravityStrength = -(2f * desiredJumpHeight) / (timeToJumpApex * timeToJumpApex);

        // convert to Rigidbody2D.gravityScale (relative to project-wide Physics2D.gravity.y)
        calculatedGravityScale = calculatedGravityStrength / Physics2D.gravity.y;

        // initial jump velocity = |gravity| * time
        calculatedJumpForce = Mathf.Abs(calculatedGravityStrength) * timeToJumpApex;

        // acceleration force = (fixedUpdatesPerSecond * acceleration) / maxSpeed
        calculatedGroundAccelerationForce = (FIXED_UPDATES_PER_SECOND * groundAcceleration) / maxRunSpeed;
        calculatedGroundDecelerationForce = (FIXED_UPDATES_PER_SECOND * groundDeceleration) / maxRunSpeed;
    }
}