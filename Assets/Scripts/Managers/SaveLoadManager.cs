using System.IO;
using UnityEngine;

/// <summary>
/// Provides simple JSON-based save and load operations inside
/// <see cref="Application.persistentDataPath"/>.
///
/// This manager is used as a global persistence service for small serializable
/// data containers, such as spawn information that must survive scene reloads.
/// It creates target folders automatically and supports save, load, file delete
/// and folder delete operations.
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

    // ────────────────── API ──────────────────

    #region Public Save / Load API
    /// <summary>
    /// Saves the given serializable object as formatted JSON.
    /// The target directory is created automatically if needed.
    /// </summary>
    /// <typeparam name="T"> serializable data type </typeparam>
    /// <param name="dataToSave"> object to serialize and write to disk </param>
    /// <param name="folderName"> folder inside persistent data path </param>
    /// <param name="fileName"> target file name </param>
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

    /// <summary>
    /// Loads JSON data from disk and overwrites fields of the provided object.
    /// The target object must already exist before calling this method.
    /// </summary>
    /// <typeparam name="T"> serializable data type </typeparam>
    /// <param name="dataToLoadInto"> existing object that receives loaded values </param>
    /// <param name="folderName"> folder inside persistent data path </param>
    /// <param name="fileName"> source file name </param>
    public void Load<T>(T dataToLoadInto, string folderName, string fileName)
    {
        string loadPath = Path.Combine(Application.persistentDataPath, folderName, fileName);
        if (File.Exists(loadPath))
        {
            string loadDataString = File.ReadAllText(loadPath);
            JsonUtility.FromJsonOverwrite(loadDataString, dataToLoadInto);
        }
    }

    /// <summary>
    /// Saves spawn data through a single consistent entry point.
    /// Every spawn save must provide scene name, spawn key and facing direction together.
    /// </summary>
    public void SaveSpawnData(string sceneName, string spawnPintKey, bool facingRight)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Scene name is empty. Using Level1 as fallback.");
            sceneName = "Level1";
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
            Debug.LogWarning($"{nameof(SaveLoadManager)}: Scene name is empty. Using Level1 as fallback.");
            sceneToLoad = "Level1";
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

    /// <summary>
    /// Loads data from the default save file configured in the inspector.
    /// </summary>
    public void LoadDefault<T>(T dataToLoadInto)
    {
        Load(dataToLoadInto, folderName, fileName);
    }

    /// <summary>
    /// Returns the full path to the default save file.
    /// </summary>
    public string GetDefaultSavePath()
    {
        return Path.Combine(Application.persistentDataPath, folderName, fileName);
    }

    /// <summary>
    /// Returns true if the default save file exists.
    /// </summary>
    public bool HasDefaultSaveFile()
    {
        return File.Exists(GetDefaultSavePath());
    }

    /// <summary>
    /// Deletes a specific save file if it exists
    /// </summary>
    /// <param name="folderName"> folder inside persistent data path </param>
    /// <param name="fileName"> file to delete </param>
    public void DeleteSaveFile(string folderName, string fileName)
    {
        string deletePath = Path.Combine(Application.persistentDataPath, folderName, fileName);
        if (File.Exists(deletePath))
        {
            File.Delete(deletePath);
        }
    }

    /// <summary>
    /// Deletes an entire save folder and all files inside it
    /// </summary>
    /// <param name="folderName"> folder inside persistent data path </param>
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