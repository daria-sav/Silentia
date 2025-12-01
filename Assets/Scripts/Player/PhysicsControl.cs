using UnityEngine;

public class PhysicsControl : MonoBehaviour
{
    public Rigidbody2D rb;
    [Header("Ground")]
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private Transform leftGroundPoint;
    [SerializeField] private Transform rightGroundPoint;
    [SerializeField] private LayerMask whatToDetect;
    public bool isGrounded;
    private RaycastHit2D leftGroundHit;
    private RaycastHit2D rightGroundHit;

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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGround();
    }
}
