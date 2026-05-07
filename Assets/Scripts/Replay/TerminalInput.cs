using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    private InputAction aExit, aPlay, aDelete, aConfirm;
    private InputAction[] profileActions;

    private bool initialized;

    private int enableFrame = -1000;
    private const int InputGuardFrames = 3;

    private readonly HashSet<InputAction> blockedUntilRelease = new HashSet<InputAction>();

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
        aConfirm = terminalMap.FindAction(confirmActionName, false);

        profileActions = new InputAction[]
        {
            terminalMap.FindAction(profile1ActionName, true),
            terminalMap.FindAction(profile2ActionName, true),
            terminalMap.FindAction(profile3ActionName, true),
            terminalMap.FindAction(profile4ActionName, true),
            terminalMap.FindAction(profile5ActionName, true),
            terminalMap.FindAction(profile6ActionName, true),
        };

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

            blockedUntilRelease.Clear();
            foreach (var a in terminalMap.actions)
                blockedUntilRelease.Add(a);
        }
        else
        {
            if (!terminalMap.enabled) return;

            foreach (var a in terminalMap.actions)
                a.Reset();
            terminalMap.Disable();
            blockedUntilRelease.Clear();
        }
    }

    private bool InputGuardActive => Time.frameCount - enableFrame < InputGuardFrames;

    private bool IsActionAllowed(InputAction action)
    {
        if (action == null) return false;
        if (InputGuardActive) return false;

        if (blockedUntilRelease.Contains(action))
        {
            if (!action.IsPressed())
                blockedUntilRelease.Remove(action);
            return false;
        }
        return true;
    }

    public bool ExitDown()
        => initialized && IsActionAllowed(aExit) && aExit.WasPressedThisFrame();

    public bool PlayDown()
        => initialized && IsActionAllowed(aPlay) && aPlay.WasPressedThisFrame();

    public bool DeleteDown()
        => initialized && aDelete != null && IsActionAllowed(aDelete) && aDelete.WasPressedThisFrame();

    public int ProfileDown()
    {
        if (!initialized) return -1;
        if (InputGuardActive) return -1;

        for (int i = 0; i < profileActions.Length; i++)
        {
            if (!IsActionAllowed(profileActions[i])) continue;
            if (profileActions[i].WasPressedThisFrame())
            {
                var s = TerminalSession.Instance;
                Debug.Log($"[TI] ProfileDown={i + 1} frame={Time.frameCount} " +
                          $"enableFrame={enableFrame} ready={s?.IsTerminalInputReady} " +
                          $"state={s?.State} unscaledT={Time.unscaledTime:F3} " +
                          $"readyAt={(s != null ? "yes" : "no")}");
                return i + 1;
            }
        }
        return -1;
    }

    public bool ConfirmDown()
        => initialized && IsActionAllowed(aConfirm) && aConfirm.WasPressedThisFrame();
    #endregion
}