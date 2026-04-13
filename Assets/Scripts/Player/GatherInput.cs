using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Collects player input for the current fixed tick.
///
/// Supports two input sources:
/// - Live   : reads input from Unity Input System
/// - Replay : receives pre-recorded InputFrame data
///
/// Raw input callbacks are buffered between fixed ticks and then
/// converted into tick-local flags in FixedUpdate so gameplay,
/// recording and replay all observe the same input snapshot.
/// </summary>
[DefaultExecutionOrder(-350)]
public class GatherInput : MonoBehaviour
{
    public enum InputMode
    {
        Live,   
        Replay 
    }

    [Header("Input System (Live mode)")]
    public PlayerInput playerInput;
    public InputActionReference moveActionRef;
    public InputActionReference jumpActionRef;
    public InputActionReference dashActionRef;
    public InputActionReference interactActionRef;
    public InputActionReference stopRecordActionRef;

    [Header("Mode")]
    [SerializeField] private InputMode mode = InputMode.Live;

    private InputActionMap playerMap;

    // public read-only input state for THIS fixed tick
    public float horizontalInput { get; private set; }
    public Vector2 move { get; private set; }

    public bool jumpHeld { get; private set; }
    public bool dashHeld { get; private set; }

    public bool jumpDownTick { get; private set; }
    public bool jumpUpTick { get; private set; }
    public bool dashDownTick { get; private set; }
    public bool dashUpTick { get; private set; }

    // raw events accumulated between fixed ticks
    private bool jumpDownRaw;
    private bool jumpUpRaw;
    private bool dashDownRaw;
    private bool dashUpRaw;
    private bool interactDownFrameRaw;
    private bool stopRecordDownRaw;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Start()
    {
        if (playerInput == null)
            return;

        if (playerInput.actions == null)
            return;

        playerMap = playerInput.actions.FindActionMap("Player");
        playerMap?.Enable();

        EnableActions();
    }

    private void OnEnable()
    {
        if (playerInput == null)
            return;

        EnableActions();
        SubscribeLiveCallbacks();
    }

    private void OnDisable()
    {
        UnsubscribeLiveCallbacks();
        playerMap?.Disable();
    }
    #endregion

    // ───────── FIXED-STEP INPUT SYNC ────────

    #region Fixed-step Input Sync
    private void FixedUpdate()
    {
        if (mode != InputMode.Live)
            return;

        move = moveActionRef != null ? moveActionRef.action.ReadValue<Vector2>() : Vector2.zero;
        horizontalInput = move.x;

        PromoteRawEventsToTickFlags();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public bool ConsumeInteractDown()
    {
        if (!interactDownFrameRaw) 
            return false;

        interactDownFrameRaw = false;
        return true;
    }

    public bool ConsumeStopRecordDown()
    {
        if (!stopRecordDownRaw) return false;
        stopRecordDownRaw = false;
        return true;
    }

    public void DisablePlayerMap()
    {
        CachePlayerMap();

        playerMap?.Disable();
    }

    public void EnablePlayerMap()
    {
        CachePlayerMap();

        playerMap?.Enable();

        // reset transient inputs
        ResetInputState();
        mode = InputMode.Live;
    }

    public InputFrame CaptureFrame(int tick)
    {
        return new InputFrame
        {
            tick = tick,

            moveX = move.x,
            moveY = move.y,

            jumpDown = jumpDownTick,
            jumpUp = jumpUpTick,
            jumpHeld = jumpHeld,

            dashDown = dashDownTick,
            dashUp = dashUpTick,
            dashHeld = dashHeld
        };
    }

    public void SetMode(InputMode newMode)
    {
        mode = newMode;

        ResetInputState();
    }

    public void ApplyReplayFrame(InputFrame frame)
    {
        mode = InputMode.Replay;

        ResetInputState();

        move = new Vector2(frame.moveX, frame.moveY);
        horizontalInput = move.x;

        jumpHeld = frame.jumpHeld;
        dashHeld = frame.dashHeld;

        jumpDownTick = frame.jumpDown;
        jumpUpTick = frame.jumpUp;

        dashDownTick = frame.dashDown;
        dashUpTick = frame.dashUp;
    }

    public void ClearJumpDownTick() => jumpDownTick = false;
    public void ClearDashDownTick() => dashDownTick = false;
    public void ClearInteractBuffered() => interactDownFrameRaw = false;
    public void ClearJumpUpTick() => jumpUpTick = false;
    public void ClearDashUpTick() => dashUpTick = false;
    #endregion

    // ───────────── LIVE INPUT ────────────────

    #region Live Input Callbacks
    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) 
            return;

        jumpDownRaw = true;
        jumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) 
            return;

        jumpUpRaw = true;
        jumpHeld = false;
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) 
            return;

        dashDownRaw = true;
        dashHeld = true;
    }

    private void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) 
            return;

        dashUpRaw = true;
        dashHeld = false;
    }

    private void OnInteractStarted(InputAction.CallbackContext _)
    {
        if (mode != InputMode.Live) 
            return;

        interactDownFrameRaw = true;
    }

    private void OnStopRecordStarted(InputAction.CallbackContext _)
    {
        if (mode != InputMode.Live) return;
        stopRecordDownRaw = true;
    }
    #endregion

    // ───────────── SETUP HELPERS ─────────────

    #region Setup Helpers
    private void CachePlayerMap()
    {
        if (playerMap != null)
            return;

        if (playerInput == null || playerInput.actions == null)
            return;

        playerMap = playerInput.actions.FindActionMap("Player");
    }

    private void EnableActions()
    {
        moveActionRef?.action?.Enable();
        jumpActionRef?.action?.Enable();
        dashActionRef?.action?.Enable();
        interactActionRef?.action?.Enable();
        stopRecordActionRef?.action?.Enable();
    }

    private void SubscribeLiveCallbacks()
    {
        if (jumpActionRef != null)
        {
            jumpActionRef.action.started += OnJumpStarted;
            jumpActionRef.action.canceled += OnJumpCanceled;
        }

        if (dashActionRef != null)
        {
            dashActionRef.action.started += OnDashStarted;
            dashActionRef.action.canceled += OnDashCanceled;
        }

        if (interactActionRef != null)
        {
            interactActionRef.action.started += OnInteractStarted;
        }

        if (stopRecordActionRef != null)
        {
            stopRecordActionRef.action.started += OnStopRecordStarted;
        }
    }

    private void UnsubscribeLiveCallbacks()
    {
        if (jumpActionRef != null)
        {
            jumpActionRef.action.started -= OnJumpStarted;
            jumpActionRef.action.canceled -= OnJumpCanceled;
        }

        if (dashActionRef != null)
        {
            dashActionRef.action.started -= OnDashStarted;
            dashActionRef.action.canceled -= OnDashCanceled;
        }

        if (interactActionRef != null)
        {
            interactActionRef.action.started -= OnInteractStarted;
        }

        if (stopRecordActionRef != null)
        {
            stopRecordActionRef.action.started -= OnStopRecordStarted;
        }
    }
    #endregion

    // ───────────── INPUT HELPERS ─────────────

    #region Input State Helpers
    private void PromoteRawEventsToTickFlags()
    {
        jumpDownTick = jumpDownRaw;
        jumpUpTick = jumpUpRaw;
        dashDownTick = dashDownRaw;
        dashUpTick = dashUpRaw;

        ClearRawEvents();
    }

    private void ClearRawEvents()
    {
        jumpDownRaw = false;
        jumpUpRaw = false;
        dashDownRaw = false;
        dashUpRaw = false;
    }

    private void ResetInputState()
    {
        move = Vector2.zero;
        horizontalInput = 0f;

        jumpHeld = false;
        dashHeld = false;

        jumpDownTick = false;
        jumpUpTick = false;
        dashDownTick = false;
        dashUpTick = false;

        jumpDownRaw = false;
        jumpUpRaw = false;
        dashDownRaw = false;
        dashUpRaw = false;

        interactDownFrameRaw = false;
        stopRecordDownRaw = false;
    }
    #endregion
}