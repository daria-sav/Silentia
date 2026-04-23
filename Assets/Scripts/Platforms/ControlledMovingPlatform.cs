using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves between predefined points only while externally activated through
/// <see cref="IButtonTarget"/>.
///
/// Characters standing on top of the platform are parented to it so they move
/// together with the platform. Multiple characters can ride the platform at
/// the same time, so each rider is tracked independently.
/// </summary>
public class ControlledMovingPlatform : MonoBehaviour, IButtonTarget
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int startIndex = 0;
    [SerializeField] private Transform[] points;

    private int targetIndex;
    private bool isMoving;
    private bool isInitialized;

    private readonly HashSet<Player> riders = new HashSet<Player>();
    private readonly Dictionary<Player, Transform> previousParents = new Dictionary<Player, Transform>();

    private const float PixelsPerUnit = 16f;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Start()
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning($"{nameof(ControlledMovingPlatform)}: No points assigned.");
            enabled = false;
            return;
        }

        if (startIndex < 0 || startIndex >= points.Length)
        {
            Debug.LogWarning($"{nameof(ControlledMovingPlatform)}: Start index out of range. Using 0.");
            startIndex = 0;
        }

        if (points[startIndex] == null)
        {
            Debug.LogWarning($"{nameof(ControlledMovingPlatform)}: Start point is missing.");
            enabled = false;
            return;
        }

        foreach (Transform point in points)
            if (point != null)
                point.position = (Vector3)SnapToPixel(point.position);

        targetIndex = startIndex;
        transform.position = SnapToPixel(points[targetIndex].position);
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || !isMoving)
            return;

        Transform targetPoint = points[targetIndex];
        if (targetPoint == null)
            return;

        Vector2 rawPos = Vector2.MoveTowards(
            transform.position, targetPoint.position, moveSpeed * Time.fixedDeltaTime);

        if (Vector2.Distance(rawPos, targetPoint.position) < 0.05f)
        {
            transform.position = targetPoint.position;

            targetIndex++;
            if (targetIndex >= points.Length)
                targetIndex = 0;
        }
        else
        {
            transform.position = SnapToPixel(rawPos);
        }

        foreach (Player rider in riders)
        {
            if (rider?.motor?.RB == null) continue;
            rider.motor.RB.position = (Vector2)rider.motor.transform.position;
        }
    }

    private void OnDisable()
    {
        foreach (Player rider in riders)
        {
            if (rider == null) continue;
            previousParents.TryGetValue(rider, out Transform prev);
            rider.transform.SetParent(prev);
        }

        riders.Clear();
        previousParents.Clear();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void SetMoving(bool moving) => isMoving = moving;
    public void SetPressed(bool pressed) => SetMoving(pressed);
    #endregion

    // ───────────── COLLISION LOGIC ─────────────

    #region Collision Handling
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player rider = collision.transform.GetComponentInParent<Player>();
        if (rider == null) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                if (!riders.Contains(rider))
                {
                    riders.Add(rider);
                    if (!previousParents.ContainsKey(rider))
                        previousParents[rider] = rider.transform.parent;
                    rider.transform.SetParent(transform);
                }
                break;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Player rider = collision.transform.GetComponentInParent<Player>();
        if (rider == null || !riders.Contains(rider)) return;

        riders.Remove(rider);
        previousParents.TryGetValue(rider, out Transform prev);
        rider.transform.SetParent(prev);
        previousParents.Remove(rider);
    }
    #endregion

    private Vector2 SnapToPixel(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x * PixelsPerUnit) / PixelsPerUnit,
            Mathf.Round(pos.y * PixelsPerUnit) / PixelsPerUnit
        );
    }
}