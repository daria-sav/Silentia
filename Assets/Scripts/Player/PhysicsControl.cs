using UnityEngine;

public class PhysicsControl : MonoBehaviour
{
    public Rigidbody2D rb;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteSetTime;
    public float coyoteTimer;

    [Header("Ground")]
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private Transform leftGroundPoint;
    [SerializeField] private Transform rightGroundPoint;
    [SerializeField] private LayerMask whatToDetect;
    public bool isGrounded;
    private RaycastHit2D leftGroundHit;
    private RaycastHit2D rightGroundHit;

    [Header("Wall")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private Transform wallCheckPointUpper;
    [SerializeField] private Transform wallCheckPointLower;
    public bool isTouchingWall;
    private RaycastHit2D wallHitUpper;
    private RaycastHit2D wallHitLower;

    private float gravityValue;
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }
    public void SetCheckPoints(Transform leftGround, Transform rightGround, Transform wallUpper, Transform wallLower)
    {
        leftGroundPoint = leftGround;
        rightGroundPoint = rightGround;
        wallCheckPointUpper = wallUpper;
        wallCheckPointLower = wallLower;
    }

    public float GetGravity()
    {
        return gravityValue;
    }

    public void SetBaseGravity(float newGravity)
    {
        gravityValue = newGravity;
        rb.gravityScale = newGravity;
    }

    void Start()
    {
        gravityValue = rb.gravityScale;
        coyoteTimer = coyoteSetTime;
    }

    private bool CheckWall()
    {
        Vector2 dir = player != null && player.facingRight ? Vector2.right : Vector2.left;

        wallHitUpper = Physics2D.Raycast(wallCheckPointUpper.position, dir, wallCheckDistance, whatToDetect);
        wallHitLower = Physics2D.Raycast(wallCheckPointLower.position, dir, wallCheckDistance, whatToDetect);

        Debug.DrawRay(wallCheckPointUpper.position, (Vector3)dir * wallCheckDistance, Color.blue);
        Debug.DrawRay(wallCheckPointLower.position, (Vector3)dir * wallCheckDistance, Color.blue);

        if (wallHitUpper || wallHitLower)
        {
            Debug.Log("WALL");
            return true;
        }
        return false;
    }

    private bool CheckGround()
    {
        leftGroundHit = Physics2D.Raycast(leftGroundPoint.position, Vector2.down, groundCheckRadius, whatToDetect);
        rightGroundHit = Physics2D.Raycast(rightGroundPoint.position, Vector2.down, groundCheckRadius, whatToDetect);

        Debug.DrawRay(leftGroundPoint.position, new Vector3(0, -groundCheckRadius, 0), Color.red);
        Debug.DrawRay(rightGroundPoint.position, new Vector3(0, -groundCheckRadius, 0), Color.red);

        if (leftGroundHit || rightGroundHit)
            return true;
        
        return false;
    }

    public void DisableGravity()
    {
        rb.gravityScale = 0;
    }

    public void EnableGravity()
    {
        rb.gravityScale = gravityValue;
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else
        {
            coyoteTimer = coyoteSetTime;
        }
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGround();
        isTouchingWall = CheckWall();
    }
}
