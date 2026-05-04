using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads terminal-only input from the Terminal action map.
/// Used by TerminalController while the terminal is paused.
/// </summary>
public class TerminalInput : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Action Maps")]
    [SerializeField] private string terminalMapName = "Terminal";

    [Header("Terminal actions")]
    [SerializeField] private string exitActionName = "Exit";
    [SerializeField] private string playActionName = "Play";
    [SerializeField] private string deleteActionName = "Delete";
    [SerializeField] private string profile1ActionName = "Profile1";
    [SerializeField] private string profile2ActionName = "Profile2";
    [SerializeField] private string profile3ActionName = "Profile3";
    [SerializeField] private string profile4ActionName = "Profile4";
    [SerializeField] private string profile5ActionName = "Profile5";
    [SerializeField] private string profile6ActionName = "Profile6";
    [SerializeField] private string confirmActionName = "Confirm";

    private InputActionMap terminalMap;
    private InputAction aExit, aPlay, aDelete;
    private InputAction aP1, aP2, aP3, aP4, aP5, aP6;

    private bool initialized;

    private int enableFrame = -1;
    private const int InputGuardFrames = 3;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (playerInput == null)
        {
            var hero = FindFirstObjectByType<Player>();
            if (hero != null) playerInput = hero.GetComponent<PlayerInput>();
            if (playerInput == null) playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput == null)
        {
            Debug.LogError("TerminalInput: PlayerInput not found.");
            enabled = false;
            return;
        }

        InitializeOnce();
        if (terminalMap != null) terminalMap.Disable();
    }
    #endregion

    // ───────────── INITIALIZATION ────────────

    #region Initialization
    private void InitializeOnce()
    {
        if (initialized) return;

        var asset = playerInput.actions;
        if (asset == null) { Debug.LogError("TerminalInput: PlayerInput.actions is null."); return; }

        terminalMap = asset.FindActionMap(terminalMapName, true);

        aExit = terminalMap.FindAction(exitActionName, true);
        aPlay = terminalMap.FindAction(playActionName, true);
        aDelete = terminalMap.FindAction(deleteActionName, true);

        aP1 = terminalMap.FindAction(profile1ActionName, true);
        aP2 = terminalMap.FindAction(profile2ActionName, true);
        aP3 = terminalMap.FindAction(profile3ActionName, true);
        aP4 = terminalMap.FindAction(profile4ActionName, true);
        aP5 = terminalMap.FindAction(profile5ActionName, true);
        aP6 = terminalMap.FindAction(profile6ActionName, true);

        initialized = true;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void SetTerminalPaused(bool paused)
    {
        if (!initialized) return;

        if (paused)
        {
            if (terminalMap.enabled) return;
            foreach (var a in terminalMap.actions)
                a.Reset();

            terminalMap.Enable();
            enableFrame = Time.frameCount;
        }
        else
        {
            if (!terminalMap.enabled) return;

            foreach (var a in terminalMap.actions)
                a.Reset();
            terminalMap.Disable();
        }
    }

    private bool InputGuardActive => Time.frameCount - enableFrame < InputGuardFrames;

    public bool ExitDown() => initialized && !InputGuardActive && aExit.WasPressedThisFrame();
    public bool PlayDown() => initialized && !InputGuardActive && aPlay.WasPressedThisFrame();
    public bool DeleteDown() => initialized && !InputGuardActive && aDelete != null && aDelete.WasPressedThisFrame();

    public int ProfileDown()
    {
        if (!initialized) return -1;
        if (InputGuardActive) return -1;

        if (aP1.WasPressedThisFrame()) return 1;
        if (aP2.WasPressedThisFrame()) return 2;
        if (aP3.WasPressedThisFrame()) return 3;
        if (aP4.WasPressedThisFrame()) return 4;
        if (aP5.WasPressedThisFrame()) return 5;
        if (aP6.WasPressedThisFrame()) return 6;

        return -1;
    }

    public bool ConfirmDown()
    {
        if (!initialized) return false;
        if (InputGuardActive) return false;
        var confirm = terminalMap.FindAction(confirmActionName, false);
        return confirm != null && confirm.WasPressedThisFrame();
    }
    #endregion
}