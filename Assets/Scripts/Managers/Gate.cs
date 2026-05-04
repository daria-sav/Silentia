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

    [Header("Main Hero Restriction")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private string mainHeroTag = "Player";
    [SerializeField] private string blockedMessage = "Only the main hero can pass through here.";

    private Collider2D gateCollider;
    private bool isTriggered;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        gateCollider = GetComponent<Collider2D>();

        if (playerLayer.value == 0)
            playerLayer = LayerMask.GetMask("Player");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered)
            return;

        if (!IsInLayerMask(collision.gameObject, playerLayer))
            return;

        BodyMarkers markers = collision.GetComponentInParent<BodyMarkers>();
        Player player = collision.GetComponentInParent<Player>();

        bool isMainHero =
            collision.CompareTag(mainHeroTag) ||
            (markers != null && markers.CompareTag(mainHeroTag)) ||
            (player != null && player.CompareTag(mainHeroTag));

        if (!isMainHero)
        {
            GateHintUI.Instance?.Show(blockedMessage);
            return;
        }

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

        SpawnMode.spawnFromCheckPoint = false;
        TerminalController.ResetIntroShown();

        LevelManager.Instance.LoadLevel(levelToLoad);
    }
    #endregion

    private static bool IsInLayerMask(GameObject objectToCheck, LayerMask layerMask)
    {
        return (layerMask.value & (1 << objectToCheck.layer)) != 0;
    }
}
