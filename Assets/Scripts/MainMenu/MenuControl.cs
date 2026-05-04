using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
    [SerializeField] private GameObject newGameButton;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Start()
    {
        var inputModule = EventSystem.current?
            .GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            inputModule.enabled = false;
            inputModule.enabled = true;
        }

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

        GameObject firstButton = hasSave ? continueButton : newGameButton;
        if (firstButton != null)
            StartCoroutine(SelectFirstButton(firstButton));
    }
    #endregion

    // ─────────────── HELPERS ─────────────────

    #region Helpers
    private IEnumerator SelectFirstButton(GameObject button)
    {
        yield return null;
        yield return null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button);
        }
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
        SaveLoadManager.Instance.SaveSpawnData("Tutorial1", "Start", true);
        MovementHintUI.ResetHint();

        LevelManager.Instance.LoadLevel("Intro");
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

            string sceneToLoad = string.IsNullOrEmpty(spawnData.sceneName) ? "Tutorial1" : spawnData.sceneName;
            LevelManager.Instance.LoadLevel(sceneToLoad);
        }
        else
        {
            SaveLoadManager.Instance.SaveSpawnData("Tutorial1", "Start", true);
            LevelManager.Instance.LoadLevel("Tutorial1");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion
}