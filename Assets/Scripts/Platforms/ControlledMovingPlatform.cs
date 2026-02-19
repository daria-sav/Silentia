using UnityEngine;

public class ControlledMovingPlatform : MonoBehaviour, IButtonTarget
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int startIndex = 0;
    [SerializeField] private Transform[] points;

    private int targetIndex;
    private bool isMoving;

    private Player player;

    private void Start()
    {
        targetIndex = Mathf.Clamp(startIndex, 0, points.Length - 1);
        transform.position = points[targetIndex].position;
    }

    private void Update()
    {
        if (!isMoving) return;
        if (points == null || points.Length == 0) return;

        transform.position = Vector2.MoveTowards(transform.position, points[targetIndex].position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, points[targetIndex].position) < 0.05f)
        {
            targetIndex++;
            if (targetIndex == points.Length)
                targetIndex = 0;
        }
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    public void SetPressed(bool pressed)
    {
        SetMoving(pressed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player p = collision.transform.GetComponentInParent<Player>();

        if (p != null)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    p.transform.SetParent(transform);
                    player = p;
                    player.physicsControl.SetExtrapolate();
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Player p = collision.transform.GetComponentInParent<Player>();

        if (p != null)
        {
            p.transform.SetParent(null);

            if (player != null)
                player.physicsControl.SetInterpolate();
            player = null;
        }
    }
}