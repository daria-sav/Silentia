using UnityEngine;
using UnityEngine.InputSystem;

public class TerminalInput : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Action Maps")]
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string terminalMapName = "Terminal";

    [Header("Player actions")]
    [SerializeField] private string interactActionName = "Interact";

    [Header("Terminal actions")]
    [SerializeField] private string exitActionName = "Exit";
    [SerializeField] private string playActionName = "Play";
    [SerializeField] private string profile1ActionName = "Profile1";
    [SerializeField] private string profile2ActionName = "Profile2";
    [SerializeField] private string profile3ActionName = "Profile3";
    [SerializeField] private string profile4ActionName = "Profile4";
    [SerializeField] private string profile5ActionName = "Profile5";
    [SerializeField] private string profile6ActionName = "Profile6";

    private InputActionMap playerMap;
    private InputActionMap terminalMap;

    private InputAction aInteract;
    private InputAction aExit, aPlay;
    private InputAction aP1, aP2, aP3, aP4, aP5, aP6;

    private bool initialized;

    // ---- zone arming (fix "pressed earlier" bug) ----
    private bool inZone;
    private bool armed; // becomes true only after release inside zone

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
            Debug.LogError("TerminalInput: PlayerInput not found (expected on Player).");
            enabled = false;
            return;
        }

        Init();

        // Terminal map should be off by default (only during TerminalPaused)
        if (terminalMap != null) terminalMap.Disable();
    }

    private void Init()
    {
        if (initialized) return;

        var asset = playerInput.actions;
        if (asset == null)
        {
            Debug.LogError("TerminalInput: PlayerInput.actions is null.");
            return;
        }

        playerMap = asset.FindActionMap(playerMapName, true);
        terminalMap = asset.FindActionMap(terminalMapName, true);

        aInteract = playerMap.FindAction(interactActionName, true);

        aExit = terminalMap.FindAction(exitActionName, true);
        aPlay = terminalMap.FindAction(playActionName, true);

        aP1 = terminalMap.FindAction(profile1ActionName, true);
        aP2 = terminalMap.FindAction(profile2ActionName, true);
        aP3 = terminalMap.FindAction(profile3ActionName, true);
        aP4 = terminalMap.FindAction(profile4ActionName, true);
        aP5 = terminalMap.FindAction(profile5ActionName, true);
        aP6 = terminalMap.FindAction(profile6ActionName, true);

        initialized = true;
    }

    // called from TerminalController each frame (paused state)
    public void SetTerminalPaused(bool paused)
    {
        Init();
        if (!initialized) return;

        if (paused) terminalMap.Enable();
        else terminalMap.Disable();
    }

    // called from TerminalController trigger
    public void NotifyEnteredZone()
    {
        inZone = true;

        // IMPORTANT: do NOT arm immediately.
        // If E was pressed earlier, we wait for a release inside zone.
        armed = false;
    }

    public void NotifyExitedZone()
    {
        inZone = false;
        armed = false;
    }

    // called from TerminalController.Update when inZone && not paused
    public void UpdateArming()
    {
        if (!inZone) { armed = false; return; }

        Init();
        if (!initialized) return;

        // Arm only after E is NOT held while inside zone
        // (prevents "pressed earlier triggers later")
        if (!aInteract.IsPressed())
            armed = true;
    }

    // ===== Player (live) =====
    public bool InteractDown()
    {
        Init();
        if (!initialized) return false;
        if (!inZone || !armed) return false;

        return aInteract.WasPressedThisFrame();
    }

    // ===== Terminal (paused) =====
    public bool ExitDown() { Init(); return initialized && aExit.WasPressedThisFrame(); }
    public bool PlayDown() { Init(); return initialized && aPlay.WasPressedThisFrame(); }

    public int ProfileDown()
    {
        Init();
        if (!initialized) return -1;

        if (aP1.WasPressedThisFrame()) return 1;
        if (aP2.WasPressedThisFrame()) return 2;
        if (aP3.WasPressedThisFrame()) return 3;
        if (aP4.WasPressedThisFrame()) return 4;
        if (aP5.WasPressedThisFrame()) return 5;
        if (aP6.WasPressedThisFrame()) return 6;

        return -1;
    }
}