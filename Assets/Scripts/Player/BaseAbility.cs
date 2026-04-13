using UnityEngine;

/// <summary>
/// Base class for all player abilities.
///
/// Provides shared references to the main player systems:
/// input, state machine, animator and movement motor.
/// Also defines the common ability lifecycle:
/// enter, exit, update, fixed update and animator update.
/// </summary>
public class BaseAbility : MonoBehaviour
{
    // shared owner
    protected Player player;

    // shared systems
    protected GatherInput linkedInput;
    protected StateMachine linkedStateMachine;
    protected Animator linkedAnimator;
    protected PlayerMovement linkedMotor;

    [Header("Ability Settings")]
    public PlayerStates.State thisAbilityState;
    public bool isPermitted = true;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    protected virtual void Awake()
    {
        InitializeLinks();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void RefreshLinks()
    {
        InitializeLinks();
    }
    #endregion

    // ───────────── ABILITY FLOW ──────────────

    #region Ability Flow
    public virtual void EnterAbility() { }

    public virtual void ExitAbility() { }

    public virtual void ProcessAbility() { }

    public virtual void ProcessFixedAbility() { }

    public virtual void UpdateAnimator() { }
    #endregion

    // ──────────────── HELPERS ────────────────

    #region Helpers
    // rebuilds cached references after player/body reconfiguration
    protected virtual void InitializeLinks()
    {
        player = GetComponent<Player>();

        if (player == null)
        {
            Debug.LogError($"[{GetType().Name}] Player component not found on the same GameObject.");
            return;
        }

        linkedInput = player.gatherInput;
        linkedStateMachine = player.stateMachine;
        linkedAnimator = player.anim;

        linkedMotor = player.motor;

        if (linkedMotor == null)
            linkedMotor = player.GetComponentInChildren<PlayerMovement>(true);

        if (linkedMotor == null)
            Debug.LogError($"[{GetType().Name}] linkedMotor not found (player.motor is null and none in children).");
    }
    #endregion
}