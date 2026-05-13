using UnityEngine;

/// <summary>
/// Stores movement, jump, wall, and dash tuning values for the player motor.
/// </summary>
[CreateAssetMenu(menuName = "Player Data")]
public class PlayerData : ScriptableObject
{
    // ───────────── GRAVITY AND FALLING ─────────────

    [Header("Gravity and Falling")]
    [HideInInspector] public float calculatedGravityStrength;
    [HideInInspector] public float calculatedGravityScale;
    
    [Space(5)]
    public float fallGravityMultiplier = 1f;
    public float maxNormalFallSpeed; 

    [Space(5)]
    public float fastFallGravityMultiplier = 1.5f;
    public float maxFastFallSpeed;

    // ───────────── HORIZONTAL MOVEMENT ─────────────

    [Space(20)]
    [Header("Horizontal Movement")]

    public float maxRunSpeed;
    public float groundAcceleration;
    
    [HideInInspector] public float calculatedGroundAccelerationForce;
    public float groundDeceleration;
    
    [HideInInspector] public float calculatedGroundDecelerationForce;

    [Space(5)]
    [Range(0f, 1)] public float airAccelerationMultiplier = 1f;
    [Range(0f, 1)] public float airDecelerationMultiplier = 1f;
    

    [Space(5)]
    public bool preserveMomentum = true;

    // ───────────── JUMP SHAPE ─────────────

    [Space(20)]
    [Header("Jump Shape")]

    public float desiredJumpHeight;
    public float timeToJumpApex;
    public int maxAirJumps = 0;

    [HideInInspector] public float calculatedJumpForce;

    // ───────────── JUMP BEHAVIOUR ─────────────

    [Header("Jump Behaviour")]

    public float jumpReleaseGravityMultiplier;
    [Range(0f, 1)] public float apexHangGravityMultiplier;
    public float apexHangVelocityThreshold;
    

    [Space(1f)]
    public float apexHangAccelerationMultiplier = 1f;
    public float apexHangMaxSpeedMultiplier = 1f;

    // ───────────── WALL JUMP ─────────────

    [Header("Wall Jump")]

    public Vector2 wallJumpForce;

    [Space(5)]
    [Range(0f, 1f)] public float wallJumpMovementLerp;
    [Range(0f, 1.5f)] public float wallJumpMovementLockTime;

    // ───────────── WALL SLIDE ─────────────

    [Space(20)]
    [Header("Wall Slide")]

    public float wallSlideSpeed;
    public float wallSlideAcceleration;

    // ───────────── INPUT ASSISTS ─────────────

    [Header("Input Assists")]
    [Range(0.01f, 0.5f)] public float coyoteTime;
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime;

    // ───────────── DASH ─────────────

    [Space(20)]
    [Header("Dash")]

    public int maxDashCount;
    public float dashSpeed;
    public float dashFreezeTime;

    [Space(5)]
    public float dashActiveTime;

    [Space(5)]
    public float dashRecoveryTime;
    public Vector2 dashRecoverySpeed;

    [Space(5)]
    public float dashRefillTime;

    [Space(5)]
    [Range(0.01f, 0.5f)] public float dashInputBufferTime;

    private const float FIXED_UPDATES_PER_SECOND = 50f;

    // ───────────── LIFECYCLE ─────────────

    #region Lifecycle
    private void OnValidate()
    {
        Recalculate();
    }

    private void OnEnable()
    {
        Recalculate();
    }
    #endregion

    // ───────────── CALCULATIONS ─────────────

    #region Calculations
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
    #endregion
}