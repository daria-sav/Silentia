using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private SpawnIdentifier[] spawnCheckPoints;
    private SpawnData spawnData = new SpawnData();
    private CheckpointData checkpointData = new CheckpointData();
    private bool canLoadFromCheckpoint = false;

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

        string loadPath = Path.Combine(Application.persistentDataPath, SaveLoadManager.Instance.folderName, SaveLoadManager.Instance.fileCheckpoint);

        if (File.Exists(loadPath))
        {
            SaveLoadManager.Instance.Load(checkpointData, SaveLoadManager.Instance.folderName, SaveLoadManager.Instance.fileCheckpoint);

            Debug.Log($"{nameof(SpawnControl)}: Checkpoint data loaded. Scene to load: {checkpointData.sceneToLoad}, ActiveScene: {SceneManager.GetActiveScene().name}, Checkpoint key: {checkpointData.checkPointKey}");
            if (checkpointData.sceneToLoad == SceneManager.GetActiveScene().name)
            {
                canLoadFromCheckpoint = true;
            }
        }
        if (SpawnMode.spawnFromCheckPoint == true && canLoadFromCheckpoint)
        {
            foreach (SpawnIdentifier spawnID in spawnCheckPoints)
            {
                if (spawnID.spawnKey == checkpointData.checkPointKey)
                {
                    player.transform.position = spawnID.transform.position;
                    break;
                }
            }
            if(checkpointData.facingRight == false)
            {
                player.ForceFlip();
            }

            SpawnMode.spawnFromCheckPoint = false;
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