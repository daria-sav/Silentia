using UnityEngine;

/// <summary>
/// Applies saved spawn information to the player when the scene starts.
///
/// This component loads the saved spawn key and facing direction,
/// finds the matching spawn point in the current scene and moves the
/// player there. It is used to restore the correct spawn position after
/// level restart, including terminal-driven restarts.
/// </summary>
public class SpawnControl : MonoBehaviour
{
    private Player player;

    [Header("Scene Spawn Points")]
    [SerializeField] private SpawnIdentifier[] spawnPoints;
    private SpawnData spawnData = new SpawnData();

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    void Start()
    {
        player = FindAnyObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning($"{nameof(SpawnControl)}: Player was not found in the scene.");
            return;
        }

        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(SpawnControl)}: {nameof(SaveLoadManager)} instance was not found. Spawn data could not be loaded.");
            return;
        }

        SaveLoadManager.Instance.LoadDefault(spawnData);

        SpawnIdentifier targetSpawn = FindSpawnPointByKey(spawnData.spawnPintKey);

        if (targetSpawn == null)
        {
            Debug.LogWarning($"{nameof(SpawnControl)}: No spawn point with key '{spawnData.spawnPintKey}' was found.");
            return;
        }

        player.transform.position = targetSpawn.transform.position;

        if (spawnData.facingRight == false)
        {
            player.ForceFlip();
        }
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    /// <summary>
    /// Returns the spawn point with the given key, or null if none matches.
    /// </summary>
    private SpawnIdentifier FindSpawnPointByKey(string spawnKey)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        foreach (SpawnIdentifier spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            if (spawnPoint.spawnKey == spawnKey)
                return spawnPoint;
        }

        return null;
    }
    #endregion
}