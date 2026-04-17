using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles pause menu: open/close, restart, main menu, quit.
/// Uses the Menu action from PlayerControls Input Actions.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Input")]
    [SerializeField] private InputActionReference menuActionRef;

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


        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(paused);
        }
    }
    #endregion
}