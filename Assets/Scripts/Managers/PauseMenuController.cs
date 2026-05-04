using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Handles pause menu: open/close, restart, main menu, quit.
/// Uses the Menu action from PlayerControls Input Actions.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Input")]
    [SerializeField] private InputActionReference menuActionRef;
    [SerializeField] private GatherInput gatherInput;

    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string restartSpawnKey = "Start";

    private bool isPaused;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void OnEnable()
    {
        if (menuActionRef != null)
        {
            menuActionRef.action.Enable();
            menuActionRef.action.started += OnMenuPressed;
        }
    }

    private void OnDisable()
    {
        if (menuActionRef != null)
            menuActionRef.action.started -= OnMenuPressed;
    }

    private void Update()
    {
        if (!isPaused) return;
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null && firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }
    #endregion

    // ─────────────── INPUT ───────────────────

    #region Input
    private void OnMenuPressed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void Resume()
    {
        SetPaused(false);
    }

    public void RestartLevel()
    {
        SetPaused(false);

        if (SaveLoadManager.Instance != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name;

            SaveLoadManager.Instance.SaveSpawnData(currentScene, restartSpawnKey, true);
        }

        if (LevelManager.Instance != null)
            LevelManager.Instance.RestartLevel();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        SetPaused(false);

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(mainMenuScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

    // ─────────────── HELPERS ─────────────────

    #region Helpers
    private void TogglePause()
    {
        if (!isPaused && TerminalSession.Instance != null &&
            TerminalSession.Instance.State != TerminalSession.TerminalState.Normal &&
            TerminalSession.Instance.State != TerminalSession.TerminalState.Playback)
            return;

        SetPaused(!isPaused);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var inputModule = EventSystem.current?
                .GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                inputModule.enabled = false;
                inputModule.enabled = true;
            }
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (gatherInput != null)
        {
            if (paused)
                gatherInput.DisablePlayerMap();
            else
                gatherInput.EnablePlayerMap();
        }

        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(paused);

        if (paused && firstSelectedButton != null)
            StartCoroutine(SelectFirstButton());
    }

    private IEnumerator SelectFirstButton()
    {
        yield return null;
        yield return null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null); 
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }
    #endregion
}