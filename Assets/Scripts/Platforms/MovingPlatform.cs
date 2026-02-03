using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private int startIndex;
    [SerializeField] private Transform[] points;

    private int targetIndex;

    private Player player;
    void Start()
    {
        targetIndex = startIndex;
        transform.position = points[targetIndex].position;
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, points[targetIndex].position, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, points[targetIndex].position) < 0.05f)
        {
            targetIndex++;
            if (targetIndex == points.Length)
            {
                targetIndex = 0;
            }
        }
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
