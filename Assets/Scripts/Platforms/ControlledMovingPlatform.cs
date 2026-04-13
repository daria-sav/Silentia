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
            Debug.LogWarning($"{nameof(ControlledMovingPlatform)}: Start index is out of range. Using 0.");
            startIndex = 0;
        }

        if (points[startIndex] == null)
        {
            Debug.LogWarning($"{nameof(ControlledMovingPlatform)}: Start point is missing.");
            enabled = false;
            return;
        }

        targetIndex = startIndex;
        transform.position = points[targetIndex].position;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || !isMoving)
            return;

        Transform targetPoint = points[targetIndex];

        if (targetPoint == null)
            return;

        transform.position = Vector2.MoveTowards(transform.position, points[targetIndex].position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, points[targetIndex].position) < 0.05f)
        {
            targetIndex++;

            if (targetIndex >= points.Length)
                targetIndex = 0;
        }
    }
    private void OnDisable()
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

    // ────────────────── API ──────────────────

    #region Public API
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    public void SetPressed(bool pressed)
    {
        SetMoving(pressed);
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

        if (rider == null || !riders.Contains(rider))
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
}