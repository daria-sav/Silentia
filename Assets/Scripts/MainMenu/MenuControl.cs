using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the main menu actions for starting a new game,
/// continuing from the last saved spawn state, and quitting the application.
///
/// This menu does not place the player inside a scene directly.
/// Instead, it decides which scene should be loaded, while
/// <see cref="SpawnControl"/> later applies the saved spawn point and facing direction.
/// </summary>
public class MenuControl : MonoBehaviour
{
    [SerializeField] private GameObject continueButton;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Start()
    {
        if (continueButton == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: Continue button is not assigned.");
            return;
        }

        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: {nameof(SaveLoadManager)} instance was not found.");
            continueButton.SetActive(false);
            return;
        }

        bool hasSave = SaveLoadManager.Instance.HasDefaultSaveFile();

        continueButton.SetActive(hasSave);

        if (hasSave && EventSystem.current != null)
            EventSystem.current.firstSelectedGameObject = continueButton;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void NewGame()
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: {nameof(SaveLoadManager)} instance was not found.");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: {nameof(LevelManager)} instance was not found.");
            return;
        }

        SaveLoadManager.Instance.DeleteFolder(SaveLoadManager.Instance.folderName);
        SaveLoadManager.Instance.SaveSpawnData("Level1", "Start", true);

        LevelManager.Instance.LoadLevel("Level1");
    }

    public void ContinueGame()
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: {nameof(SaveLoadManager)} instance was not found.");
            return;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MenuControl)}: {nameof(LevelManager)} instance was not found.");
            return;
        }

        if (SaveLoadManager.Instance.HasDefaultSaveFile())
        {
            SpawnData spawnData = new SpawnData();
            SaveLoadManager.Instance.LoadDefault(spawnData);

            string sceneToLoad = string.IsNullOrEmpty(spawnData.sceneName) ? "Level1" : spawnData.sceneName;
            LevelManager.Instance.LoadLevel(sceneToLoad);
        }
        else
        {
            SaveLoadManager.Instance.SaveSpawnData("Level1", "Start", true);
            LevelManager.Instance.LoadLevel("Level1");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion
}