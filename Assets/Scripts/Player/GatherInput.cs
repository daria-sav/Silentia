using UnityEngine;
using UnityEngine.InputSystem;

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

    private InputActionMap playerMap;
    //private InputActionMap uiMap;

    public InputActionReference moveActionRef;
    public InputActionReference jumpActionRef;
    public InputActionReference dashActionRef;
    public InputActionReference interactActionRef;

    [Header("Mode")]
    [SerializeField] private InputMode mode = InputMode.Live;

    // Values used by gameplay THIS fixed tick
    public float horizontalInput { get; private set; }

    public bool jumpHeld { get; private set; }
    public bool dashHeld { get; private set; }

    public bool jumpDownTick { get; private set; }
    public bool jumpUpTick { get; private set; }
    public bool dashDownTick { get; private set; }
    public bool dashUpTick { get; private set; }

    // Raw events (accumulate between fixed ticks)
    private bool jumpDownRaw;
    private bool jumpUpRaw;
    private bool dashDownRaw;
    private bool dashUpRaw;
    private bool interactDownFrameRaw;

    private void OnEnable()
    {
        if (playerInput == null)
            return;

        EnableActions();

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
    }

    private void OnDisable()
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

        playerMap?.Disable();
    }

    void Start()
    {
        if (playerInput == null)
            return;

        if (playerInput.actions == null)
            return;

        playerMap = playerInput.actions.FindActionMap("Player");
        playerMap?.Enable();

        EnableActions();
        //    uiMap = playerInput.actions.FindActionMap("UI");

        //    // also good stuff
        //    //playerInput.actions.Enable();
        //    //jumpActionRef.action.Disable();
    }

    private void FixedUpdate()
    {
        if (mode != InputMode.Live)
            return;

        horizontalInput = moveActionRef != null ? moveActionRef.action.ReadValue<float>() : 0f;

        jumpDownTick = jumpDownRaw;
        jumpUpTick = jumpUpRaw;
        dashDownTick = dashDownRaw;
        dashUpTick = dashUpRaw;

        jumpDownRaw = jumpUpRaw = false;
        dashDownRaw = dashUpRaw = false;
    }

    private void EnableActions()
    {
        moveActionRef?.action?.Enable();
        jumpActionRef?.action?.Enable();
        dashActionRef?.action?.Enable();
        interactActionRef?.action?.Enable();
    }

    // ================== LIVE INPUT CALLBACKS ==================
    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) return;
        jumpDownRaw = true;
        jumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) return;
        jumpUpRaw = true;
        jumpHeld = false;
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) return;
        dashDownRaw = true;
        dashHeld = true;
    }

    private void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        if (mode != InputMode.Live) return;
        dashUpRaw = true;
        dashHeld = false;
    }

    private void OnInteractStarted(InputAction.CallbackContext _)
    {
        if (mode != InputMode.Live) return;
        interactDownFrameRaw = true;

#if UNITY_EDITOR
        // Debug.Log("[GatherInput] Interact started");
#endif
    }

    public bool ConsumeInteractDown()
    {
        if (!interactDownFrameRaw) return false;
        interactDownFrameRaw = false;
        return true;
    }

    public void DisablePlayerMap()
    {
        if (playerMap == null && playerInput != null)
            playerMap = playerInput.actions.FindActionMap("Player");

        playerMap?.Disable();
    }

    public void EnablePlayerMap()
    {
        if (playerMap == null && playerInput != null)
            playerMap = playerInput.actions.FindActionMap("Player");

        playerMap?.Enable();

        // reset transient inputs
        horizontalInput = 0f;
        jumpHeld = dashHeld = false;
        jumpDownTick = jumpUpTick = false;
        dashDownTick = dashUpTick = false;
        jumpDownRaw = jumpUpRaw = false;
        dashDownRaw = dashUpRaw = false;
        interactDownFrameRaw = false;

        mode = InputMode.Live;
    }

    // ================== SNAPSHOT FOR A FIXED TICK ==================
    public InputFrame CaptureFrame(int tick)
    {
        return new InputFrame
        {
            tick = tick,

            moveX = horizontalInput,

            jumpDown = jumpDownTick,
            jumpUp = jumpUpTick,
            jumpHeld = jumpHeld,

            dashDown = dashDownTick,
            dashUp = dashUpTick,
            dashHeld = dashHeld
        };
    }

    // ================== REPLAY MODE ==================
    public void SetMode(InputMode newMode)
    {
        mode = newMode;

        horizontalInput = 0f;
        jumpHeld = false;
        dashHeld = false;

        jumpDownTick = jumpUpTick = false;
        dashDownTick = dashUpTick = false;

        jumpDownRaw = jumpUpRaw = false;
        dashDownRaw = dashUpRaw = false;
    }

    public void ApplyReplayFrame(InputFrame frame)
    {
        mode = InputMode.Replay;

        horizontalInput = frame.moveX;

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
}