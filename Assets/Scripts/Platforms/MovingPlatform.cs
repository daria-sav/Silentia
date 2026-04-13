using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the platform between predefined points and carries characters standing on top of it.
///
/// The platform cycles through all assigned waypoints in order. When a character lands on the
/// platform from above, that character is parented to the platform so it moves together with it.
/// Multiple characters can stand on the platform at the same time, so each rider is tracked
/// independently and detached only when that specific character leaves the collision.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private int startIndex;
    [SerializeField] private Transform[] points;

    private int targetIndex;
    private bool isInitialized;

    // all characters currently riding this platform
    private readonly HashSet<Player> riders = new HashSet<Player>();

    // original parent is remembered so it can be restored correctly on exit
    private readonly Dictionary<Player, Transform> previousParents = new Dictionary<Player, Transform>();

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    void Start()
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning($"{nameof(MovingPlatform)}: No points assigned.");
            enabled = false;
            return;
        }

        if (startIndex < 0 || startIndex >= points.Length)
        {
            Debug.LogWarning($"{nameof(MovingPlatform)}: Start index is out of range. Using 0.");
            startIndex = 0;
        }

        if (points[startIndex] == null)
        {
            Debug.LogWarning($"{nameof(MovingPlatform)}: Start point is missing.");
            enabled = false;
            return;
        }

        targetIndex = startIndex;
        transform.position = points[targetIndex].position;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
            return;

        Transform targetPoint = points[targetIndex];

        if (targetPoint == null)
            return;

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

    private void OnDisable()
    {
        ReleaseAllRiders();
    }
    #endregion

    // ───────────── COLLISION LOGIC ─────────────

    #region Collision Handling
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player rider = collision.transform.GetComponentInParent<Player>();

        if (rider == null)
            return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // contact normal points from this platform to the colliding object
            // a negative Y normal means the character is standing on top of the platform
            if (contact.normal.y < -0.5f)
            {
                if (!riders.Contains(rider))
                {
                    riders.Add(rider);

                    if (!previousParents.ContainsKey(rider))
                        previousParents[rider] = rider.transform.parent;

                    rider.transform.SetParent(transform);

                    if (rider.motor != null)
                        rider.motor.SetPlatformInterpolation();
                }

                break;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Player rider = collision.transform.GetComponentInParent<Player>();

        if (rider == null) 
            return;

        if (!riders.Contains(rider))
            return;

        riders.Remove(rider);

        Transform previousParent = null;
        previousParents.TryGetValue(rider, out previousParent);
        rider.transform.SetParent(previousParent);

        if (rider.motor != null)
            rider.motor.SetDefaultInterpolation();

        previousParents.Remove(rider);
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    /// <summary>
    /// Detaches all riders from the platform and restores their interpolation settings.
    /// This is used when the platform is disabled or destroyed.
    /// </summary>
    private void ReleaseAllRiders()
    {
        foreach (Player rider in riders)
        {
            if (rider == null)
                continue;

            Transform previousParent = null;
            previousParents.TryGetValue(rider, out previousParent);
            rider.transform.SetParent(previousParent);

            if (rider.motor != null)
                rider.motor.SetDefaultInterpolation();
        }

        riders.Clear();
        previousParents.Clear();
    }
    #endregion
}