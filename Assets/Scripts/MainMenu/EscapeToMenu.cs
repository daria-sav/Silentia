using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeToMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string menuActionName = "Menu";

    private InputAction menuAction;

    private void Awake()
    {
        if (playerInput == null)
        {
            var hero = FindFirstObjectByType<Player>();
            if (hero != null) playerInput = hero.GetComponent<PlayerInput>();
            if (playerInput == null) playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("EscapeToMenu: PlayerInput/actions not found.");
            enabled = false;
            return;
        }

        var map = playerInput.actions.FindActionMap(playerMapName, true);
        menuAction = map.FindAction(menuActionName, true);
    }

    private void OnEnable()
    {
        if (menuAction != null)
            menuAction.performed += OnMenu;
    }

    private void OnDisable()
    {
        if (menuAction != null)
            menuAction.performed -= OnMenu;
    }

    private void OnMenu(InputAction.CallbackContext _)
    {
        if (TerminalSession.Instance != null && TerminalSession.Instance.State == TerminalSession.TerminalState.TerminalPaused)
            return;

        Time.timeScale = 1f;
        LevelManager.instance.LoadLevelString(mainMenuSceneName);
    }
}