using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class JumpingSpike : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float pauseOnGround = 0.4f;
    [SerializeField] private float pauseOnCeiling = 0.4f;

    [Header("Detection")]
    [SerializeField] private LayerMask whatToDetect;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private float checkDistance = 0.1f;
    [SerializeField] private float approachDistance = 0.35f;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;     
    [SerializeField] private Sprite moveSprite;     
    [SerializeField] private Sprite approachSprite; 

    [Header("Damage")]
    [SerializeField] private float spikeDamage = 1f;
    [SerializeField] private float knockBackDuration = 0.2f;
    [SerializeField] private Vector2 knockBackForce = new Vector2(10f, 15f);

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private enum State { PausedOnGround, MovingUp, PausedOnCeiling, MovingDown }
    private State state;

    private Coroutine routine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void Start()
    {
        state = State.PausedOnGround;
        ApplyVisuals(idle: true, onCeiling: false);

        routine = StartCoroutine(StateLoop());
    }

    private IEnumerator StateLoop()
    {
        while (true)
        {
            switch (state)
            {
                case State.PausedOnGround:
                    rb.linearVelocity = Vector2.zero;
                    ApplyVisuals(idle: true, onCeiling: false);
                    yield return new WaitForSeconds(pauseOnGround);
                    state = State.MovingUp;
                    break;

                case State.MovingUp:
                    while (true)
                    {
                        // moving up
                        rb.linearVelocity = Vector2.up * moveSpeed;

                        // visual: move/approach
                        bool nearCeiling = IsNearCeiling();
                        bool touchingCeiling = IsTouchingCeiling();

                        ApplyVisuals(idle: false, onCeiling: false, approaching: nearCeiling);

                        if (touchingCeiling)
                        {
                            state = State.PausedOnCeiling;
                            break;
                        }
                        yield return null;
                    }
                    break;

                case State.PausedOnCeiling:
                    rb.linearVelocity = Vector2.zero;
                    ApplyVisuals(idle: true, onCeiling: true);
                    yield return new WaitForSeconds(pauseOnCeiling);
                    state = State.MovingDown;
                    break;

                case State.MovingDown:
                    while (true)
                    {
                        // moving down
                        rb.linearVelocity = Vector2.down * moveSpeed;

                        bool nearGround = IsNearGround();
                        bool touchingGround = IsTouchingGround();

                        ApplyVisuals(idle: false, onCeiling: true, approaching: nearGround);

                        if (touchingGround)
                        {
                            state = State.PausedOnGround;
                            break;
                        }
                        yield return null;
                    }
                    break;
            }

            yield return null;
        }
    }

    private bool IsTouchingGround()
    {
        if (groundCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, whatToDetect);
        Debug.DrawRay(groundCheck.position, Vector3.down * checkDistance, Color.red);
        return hit.collider != null;
    }

    private bool IsTouchingCeiling()
    {
        if (ceilingCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(ceilingCheck.position, Vector2.up, checkDistance, whatToDetect);
        Debug.DrawRay(ceilingCheck.position, Vector3.up * checkDistance, Color.magenta);
        return hit.collider != null;
    }

    private bool IsNearGround()
    {
        if (groundCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, approachDistance, whatToDetect);
        return hit.collider != null;
    }

    private bool IsNearCeiling()
    {
        if (ceilingCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(ceilingCheck.position, Vector2.up, approachDistance, whatToDetect);
        return hit.collider != null;
    }

    private void ApplyVisuals(bool idle, bool onCeiling, bool approaching = false)
    {
        if (sr == null) return;

        if (idle)
            sr.sprite = idleSprite;
        else
            sr.sprite = approaching ? approachSprite : moveSprite;

        sr.flipY = onCeiling;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerStats playerStats = collision.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        KnockBackAbility knockBackAbility = collision.GetComponentInParent<KnockBackAbility>();
        if (knockBackAbility != null)
            knockBackAbility.StartKnockBack(knockBackDuration, knockBackForce, transform);

        playerStats.DamagePlayer(spikeDamage);
    }
}