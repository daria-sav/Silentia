using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Manages the main menu flow, including starting a new game,
/// continuing from saved progress, and quitting the application.
///
/// The class prepares the menu UI based on save availability and delegates
/// scene loading to <see cref="LevelManager"/>. Saved spawn data is handled
/// separately by <see cref="SaveLoadManager"/> and later applied by
/// <see cref="SpawnControl"/>.
/// </summary>
public class MenuControl : MonoBehaviour
{
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject newGameButton;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // refreshes the UI input module to ensure menu navigation works correctly after scene changes
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

        // selects the most relevant first button for keyboard/controller navigation
        GameObject firstButton = hasSave ? continueButton : newGameButton;
        if (firstButton != null)
            StartCoroutine(SelectFirstButton(firstButton));
    }
    #endregion

    // ─────────────── HELPERS ─────────────────

    #region Helpers
    private IEnumerator SelectFirstButton(GameObject button)
    {
        // waits for the UI system to finish initialization before selecting the button
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

        // clears previous progress before creating a fresh default spawn state
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

            // falls back to the first tutorial scene if the saved scene name is missing.
            string sceneToLoad = string.IsNullOrEmpty(spawnData.sceneName) ? "Tutorial1" : spawnData.sceneName;
            LevelManager.Instance.LoadLevel(sceneToLoad);
        }
        else
        {
            // creates a default save state if the save file is missing unexpectedly
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