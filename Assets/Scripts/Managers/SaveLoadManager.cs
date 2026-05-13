using System.IO;
using UnityEngine;

/// <summary>
/// Provides a global JSON-based persistence service for small serializable data objects.
///
/// The manager stores save files inside <see cref="Application.persistentDataPath"/>,
/// creates folders when needed, and supports saving, loading, deleting files,
/// and deleting entire save folders. It is used for spawn and checkpoint data
/// that must persist between scene loads.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Default Spawn Save Path")]
    public string folderName = "SaveFiles";
    public string fileName = "SpawnPoint.json";

    [Header("Checkpoint Save Path")]
    public string fileCheckpoint = "Checkpoint.json";

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    // ─────────────── SAVE / LOAD API ───────────────

    #region Public Save / Load API
    public void Save<T>(T dataToSave, string folderName, string fileName)
    {
        if (dataToSave == null)
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Cannot save null data.");
            return;
        }

        string savePath = Path.Combine(Application.persistentDataPath, folderName, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
        File.WriteAllText(savePath, JsonUtility.ToJson(dataToSave, true));
    }

    public void Load<T>(T dataToLoadInto, string folderName, string fileName)
    {
        string loadPath = Path.Combine(Application.persistentDataPath, folderName, fileName);
        if (File.Exists(loadPath))
        {
            string loadDataString = File.ReadAllText(loadPath);
            JsonUtility.FromJsonOverwrite(loadDataString, dataToLoadInto);
        }
    }

    public void SaveSpawnData(string sceneName, string spawnPintKey, bool facingRight)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Scene name is empty. Using Tutorial1 as fallback.");
            sceneName = "Tutorial1";
        }

        if (string.IsNullOrWhiteSpace(spawnPintKey))
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Spawn key is empty. Using Start as fallback.");
            spawnPintKey = "Start";
        }

        SpawnData data = new SpawnData
        {
            sceneName = sceneName,
            spawnPintKey = spawnPintKey,
            facingRight = facingRight
        };

        Save(data, folderName, fileName);
    }

    public void SaveCheckpointData(string sceneToLoad, string checkPointKey, bool facingRight)
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Scene name is empty. Using Tutorial1 as fallback.");
            sceneToLoad = "Tutorial1";
        }

        if (string.IsNullOrWhiteSpace(checkPointKey))
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Checkpoint key is empty. Using Check1 as fallback.");
            checkPointKey = "Check1";
        }

        CheckpointData data = new CheckpointData
        {
            sceneToLoad = sceneToLoad,
            checkPointKey = checkPointKey,
            facingRight = facingRight
        };

        Save(data, folderName, fileCheckpoint);
    }

    public void LoadDefault<T>(T dataToLoadInto)
    {
        Load(dataToLoadInto, folderName, fileName);
    }

    public string GetDefaultSavePath()
    {
        return Path.Combine(Application.persistentDataPath, folderName, fileName);
    }

    public bool HasDefaultSaveFile()
    {
        return File.Exists(GetDefaultSavePath());
    }

    public void DeleteSaveFile(string folderName, string fileName)
    {
        string deletePath = Path.Combine(Application.persistentDataPath, folderName, fileName);
        if (File.Exists(deletePath))
        {
            File.Delete(deletePath);
        }
    }

    public void DeleteFolder(string folderName)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }
    #endregion
}