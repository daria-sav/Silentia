using UnityEngine;

/// <summary>
/// Handles transition to another level when the player enters the gate trigger.
///
/// Before loading the target scene, this component saves spawn data for the
/// next level, disables player input, stops player movement and then requests
/// a scene transition through <see cref="LevelManager"/>.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Gate : MonoBehaviour
{
    [Header("Target Level")]
    [SerializeField] private string levelToLoad;

    [Header("Spawn Data For Target Level")]
    [SerializeField] private SpawnData spawnDataForOtherLevel;

    private Collider2D gateCollider;
    private bool isTriggered;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        gateCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered)
            return;

        BodyMarkers markers = collision.GetComponentInParent<BodyMarkers>();
        Player player = collision.GetComponentInParent<Player>();

        if (markers == null)
        {
                Debug.LogWarning("Gate: No BodyMarkers found in parent of collider: " + collision.name);
                return;
        }

        if (player == null)
        {
            Debug.LogWarning($"{nameof(Gate)}: No {nameof(Player)} found in parent of collider '{collision.name}'.");
            return;
        }

        if (spawnDataForOtherLevel == null)
        {
            Debug.LogWarning($"{nameof(Gate)}: Spawn data for other level is not assigned.");
            return;
        }

        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(Gate)}: SaveLoadManager instance was not found.");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(Gate)}: LevelManager instance was not found.");
            return;
        }

        if (markers.CompareTag("Player"))
        {
            isTriggered = true;
            gateCollider.enabled = false;

            SaveLoadManager.Instance.SaveSpawnData(
                levelToLoad,
                spawnDataForOtherLevel.spawnPintKey,
                spawnDataForOtherLevel.facingRight);

            if (player.gatherInput != null)
                player.gatherInput.DisablePlayerMap();

            if (player.motor != null)
                player.motor.RB.linearVelocity = Vector2.zero;

            LevelManager.Instance.LoadLevel(levelToLoad);
        }
    }
    #endregion
}
