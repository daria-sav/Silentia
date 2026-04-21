using System.Collections;
using UnityEngine;

/// <summary>
/// Follows the active player motor's position and smoothly rotates
/// on the Y axis when the player changes facing direction.
///
/// Call SetTarget(player) after a body-swap to re-bind the correct motor.
/// On scene restart, Awake re-initializes automatically.
/// </summary>
[DefaultExecutionOrder(-150)]
public class CameraFollowTarget : MonoBehaviour
{
    [Header("Flip Rotation Stats")]
    [SerializeField] private float flipYRotationTime = 0.5f;

    private Coroutine turnCoroutine;

    [SerializeField] private Player player;

    private bool isFacingRight;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();

        if (player == null)
        {
            Debug.LogError("[CameraFollowTarget] Player not found! Assign it in the Inspector or place this object as a child of Player.");
            return;
        }

        SyncFacingState();
    }

    private void LateUpdate()
    {
        // guard: player or motor missing (e.g. destroyed during body-swap)
        if (player == null || player.motor == null) return;

        transform.position = player.motor.transform.position;
    }

    #endregion

    // ────────────────── API ──────────────────

    #region API

    public void SetTarget(Player newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogWarning("[CameraFollowTarget] SetTarget called with null player.");
            return;
        }

        player = newPlayer;
        SyncFacingState();
    }

    public void CallTurn()
    {
        if (turnCoroutine != null)
            StopCoroutine(turnCoroutine);

        turnCoroutine = StartCoroutine(FlipYLerp());
    }

    #endregion

    // ─────────────── INTERNALS ───────────────

    #region Internals

    private void SyncFacingState()
    {
        if (player.motor == null) return;

        isFacingRight = player.motor.IsFacingRight;
        // snap rotation immediately so there is no initial lerp from the wrong side
        float targetAngle = isFacingRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
    }

    private IEnumerator FlipYLerp()
    {
        float startRotation = transform.localEulerAngles.y;
        float endRotationAmount = DetermineEndRotation();
        float yRotation = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < flipYRotationTime)
        {
            elapsedTime += Time.deltaTime;

            // lerp the y rotation
            yRotation = Mathf.Lerp(startRotation, endRotationAmount, (elapsedTime / flipYRotationTime));
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, endRotationAmount, 0f);
        turnCoroutine = null;

    }

    private float DetermineEndRotation()
    {
        // read ground truth from motor, not a local toggle
        isFacingRight = player != null && player.motor != null && player.motor.IsFacingRight;
        return isFacingRight ? 0f : 180f;
    }
    #endregion
}