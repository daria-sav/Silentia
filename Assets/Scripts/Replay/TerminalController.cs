using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles terminal proximity, interaction, and paused terminal input.
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
    [SerializeField] private GameObject terminalBackground;

    [Header("Tutorial Restrictions")]
    [SerializeField] private int maxProfilesAllowed = -1; // -1 = no limit

    [Header("Tutorial Hints")]
    [SerializeField] private PillarTutorialHints tutorialHints;

    [Header("Character Intro")]
    [SerializeField] private CharacterIntroSequence introSequence;
    [SerializeField] private CharacterIntroUI characterIntroUI;

    private static bool _introShown;

    private TerminalSession session;
    private bool subscribed;

    private GatherInput gatherInput;
    private Player playerInZone;

    // presence polling
    private Collider2D triggerCol;
    private ContactFilter2D contactFilter;
    private static readonly Collider2D[] overlapBuffer = new Collider2D[8];

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

        triggerCol = GetComponent<Collider2D>();

        contactFilter = ContactFilter2D.noFilter;
        contactFilter.useTriggers = true;
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
        // keeps the scene-local terminal connected to the persistent TerminalSession
        TryBindSession();

        // trigger exit can be missed during reloads or object changes, so presence is polled manually
        PollPlayerInZone();

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

    // ──────────── PRESENCE POLLING ───────────

    #region Presence Polling
    private void PollPlayerInZone()
    {
        if (triggerCol == null) return;

        Player live = FindLivePlayerInTrigger();
        if (ReferenceEquals(playerInZone, live)) return;

        if (playerInZone != null)
            HandlePlayerExited();

        if (live != null)
            HandlePlayerEntered(live);
    }

    private Player FindLivePlayerInTrigger()
    {
        int count = triggerCol.Overlap(contactFilter, overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (col == null) continue;
            if (!col.CompareTag("Player")) continue;

            var p = col.GetComponentInParent<Player>();
            if (p == null) continue;

            // replay ghosts may share the Player tag, but they must not open the terminal
            var playback = p.GetComponent<ReplayPlayback>();
            if (playback != null && playback.IsPlaying) continue;

            return p;
        }

        return null;
    }

    private void HandlePlayerEntered(Player p)
    {
        playerInZone = p;
        gatherInput = p.gatherInput != null ? p.gatherInput : p.GetComponent<GatherInput>();
        gatherInput?.ClearInteractBuffered();

        tutorialHints?.OnPlayerEntered();
        UpdateSlotUiVisibility();

        TerminalSession.Instance?.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void HandlePlayerExited()
    {
        playerInZone = null;
        gatherInput = null;
        tutorialHints?.OnPlayerExited();
        UpdateSlotUiVisibility();
    }
    #endregion

    // ──────────── INPUT HANDLING ─────────────

    #region Input Handling
    private void HandlePausedInput()
    {
        // blocks terminal controls while the character intro dialog is active
        if (characterIntroUI != null && characterIntroUI.IsPlaying)
            return;

        if (session != null && !session.IsTerminalInputReady)
            return;

        if (terminalInput == null || session == null)
            return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame)
            {
                int next = Mathf.Max(0, session.SelectedSlot - 1);
                session.SelectSlot(next);
                return;
            }

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                Keyboard.current.dKey.wasPressedThisFrame)
            {
                int lastAvailableSlot = session.ActiveSlotCount - 1;
                int next = Mathf.Min(lastAvailableSlot, session.SelectedSlot + 1);

                session.SelectSlot(next);
                return;
            }
        }

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
            // tutorial guard
            if (maxProfilesAllowed > 0 && profileIndex > maxProfilesAllowed)
            {
                return;
            }

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
        if (playerInZone == null || session == null || gatherInput == null)
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

    private void HandleTerminalStateChanged(TerminalSession.TerminalState state)
    {
        UpdateSlotUiVisibility();

        // shows the character intro only once per runtime session
        if (state == TerminalSession.TerminalState.TerminalPaused
        && !_introShown
        && introSequence != null
        && characterIntroUI != null)
        {
            _introShown = true;
            characterIntroUI.Play(introSequence, onComplete: null);
        }
    }
    #endregion

    // ───────────────── UI ────────────────────

    #region UI
    private void UpdateSlotUiVisibility()
    {
        if (slotUiPanel == null)
            return;

        bool paused = session != null && session.State == TerminalSession.TerminalState.TerminalPaused;

        slotUiPanel.SetActive(paused);
        if (terminalHintsPanel != null) 
            terminalHintsPanel.SetActive(paused);

        if (terminalBackground != null)
        {
            // background stays visible during planning and recording-related terminal states
            bool showBg = session != null && session.State is
                TerminalSession.TerminalState.TerminalPaused or
                TerminalSession.TerminalState.EnteringRecord or
                TerminalSession.TerminalState.Recording;

            terminalBackground.SetActive(showBg);
        }
    }

    public static void ResetIntroShown()
    {
        _introShown = false;
    }
    #endregion
}