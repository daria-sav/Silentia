using UnityEngine;

/// <summary>
/// Scene-local terminal object. Detects player proximity,
/// handles interact input to open the terminal menu,
/// and routes paused-mode input (slot selection, record, play, exit)
/// to TerminalSession.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TerminalController : MonoBehaviour
{
    [Header("Terminal spawn control")]
    [SerializeField] private string terminalSpawnKey = "Terminal";
    [SerializeField] private bool facingRightAtTerminal = true;

    [Header("Input")]
    [SerializeField] private TerminalInput terminalInput;

    [Header("UI")]
    [SerializeField] private GameObject slotUiPanel;
    [SerializeField] private GameObject terminalHintsPanel;
    [SerializeField] private TerminalToast terminalToast;

    private TerminalSession session;
    private bool subscribed;

    private GatherInput gatherInput;
    private bool playerInZone;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        if (terminalInput == null) 
            terminalInput = GetComponent<TerminalInput>();
    }

    private void OnEnable()
    {
        TryBindSession();
        UpdateSlotUiVisibility();
    }

    private void OnDisable()
    {
        UnbindSession();
    }
    #endregion

    // ──────────── UPDATE LOOP ────────────────

    #region Update Loop
    private void Update()
    {
        TryBindSession();

        if (session == null)
            return;

        bool paused = session.State == TerminalSession.TerminalState.TerminalPaused;

        if (terminalInput != null)
            terminalInput.SetTerminalPaused(paused);

        if (paused)
            HandlePausedInput();
        else
            HandleNormalInput();
    }
    #endregion

    // ──────────── INPUT HANDLING ─────────────

    #region Input Handling
    private void HandlePausedInput()
    {
        if (terminalInput == null || session == null)
            return;

        if (terminalInput.ExitDown())
        {
            session.ExitTerminalPaused();
            return;
        }

        if (terminalInput.DeleteDown())
        {
            if (session.HasClip(session.SelectedSlot))
                session.ClearSelectedSlot();

            return;
        }

        int profileIndex = terminalInput.ProfileDown();
        if (profileIndex != -1)
        {
            if (session.CanStartRecordingWithProfile(profileIndex, out string msg))
            {
                session.RequestRestartAndStartRecording(profileIndex);
            }
            else if (terminalToast != null)
                terminalToast.Show(msg);
            return;
        }

        if (terminalInput.PlayDown())
        {
            session.PlayAllClips();
            session.UnfreezeAfterTerminalPlay();
        }
    }

    private void HandleNormalInput()
    {
        if (!playerInZone || session == null || gatherInput == null)
            return;

        if (gatherInput.ConsumeInteractDown())
        {
            if (slotUiPanel != null)
                slotUiPanel.SetActive(false);

            session.RequestRestartAndEnterTerminal();
        }
    }
    #endregion

    // ──────────── SESSION BINDING ────────────

    #region Session Binding
    private void TryBindSession()
    {
        if (session != null)
            return;

        session = TerminalSession.Instance;
        if (session == null)
            return;

        if (!subscribed)
        {
            session.OnStateChanged += HandleTerminalStateChanged;
            UpdateSlotUiVisibility();
            subscribed = true;
        }
    }

    private void UnbindSession()
    {
        if (session != null && subscribed)
        {
            session.OnStateChanged -= HandleTerminalStateChanged;
            subscribed = false;
        }

        session = null;
    }

    private void HandleTerminalStateChanged(TerminalSession.TerminalState _)
    {
        UpdateSlotUiVisibility();
    }
    #endregion

    // ───────────────── UI ────────────────────

    #region UI
    private void UpdateSlotUiVisibility()
    {
        if (slotUiPanel == null)
            return;

        bool show = session != null && session.State == TerminalSession.TerminalState.TerminalPaused;

        slotUiPanel.SetActive(show);
        if (terminalHintsPanel != null)
            terminalHintsPanel.SetActive(show);
    }
    #endregion

    // ──────────── TRIGGER ZONE ───────────────

    #region Trigger Zone
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        playerInZone = true;

        gatherInput = player.gatherInput != null ? player.gatherInput : player.GetComponent<GatherInput>();
        gatherInput?.ClearInteractBuffered();

        UpdateSlotUiVisibility();

        TerminalSession.Instance?.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        playerInZone = false;
        gatherInput = null;
        UpdateSlotUiVisibility();
    }
    #endregion
}