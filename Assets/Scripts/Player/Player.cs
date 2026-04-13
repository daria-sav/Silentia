using UnityEngine;

/// <summary>
/// Main player coordinator.
/// Holds shared player references, updates the current ability,
/// and keeps the visual facing direction in sync with the motor.
/// </summary>
public class Player : MonoBehaviour
{
    public GatherInput gatherInput;
    public StateMachine stateMachine;
    public PlayerStats playerStats;
    public Animator anim;
    public Transform visual;

    private BaseAbility[] playerAbilities;
    public bool facingRight = true;

    public PlayerMovement motor { get; private set; }

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        stateMachine = new StateMachine();
        playerAbilities = GetComponents<BaseAbility>();
        stateMachine.abilitiesArr = playerAbilities;
        
        RefreshStatsFromChildren();
        RefreshMotorFromChildren();

        stateMachine.ForceChange(PlayerStates.State.Idle);
    }

    private void Update()
    {
        foreach (BaseAbility ability in playerAbilities)
        {
            if (ability.thisAbilityState == stateMachine.currentState)
            {
                ability.ProcessAbility();
            }
            ability.UpdateAnimator();
        }
    }

    private void FixedUpdate()
    {
        foreach (BaseAbility ability in playerAbilities)
        {
            if (ability.thisAbilityState == stateMachine.currentState)
            {
                ability.ProcessFixedAbility();
            }
        }
    }

    private void LateUpdate()
    {
        if (motor == null || visual == null) return;

        var s = visual.localScale;
        s.x = motor.IsFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        visual.localScale = s;

        facingRight = motor.IsFacingRight;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void ForceFlip()
    {
        facingRight = !facingRight;

        if (motor != null)
            motor.UpdateFacingDirection(facingRight);

        if (visual != null)
        {
            var s = visual.localScale;
            s.x = facingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            visual.localScale = s;
        }
    }

    public void RefreshStatsFromChildren()
    {
        playerStats = GetComponentInChildren<PlayerStats>(true);
    }

    public void RefreshMotorFromChildren()
    {
        motor = GetComponentInChildren<PlayerMovement>(true);
        if (motor == null)
            Debug.LogError("[Player] PlayerMovement not found in children!");

        foreach (var a in GetComponents<BaseAbility>())
            a.RefreshLinks();
    }
    #endregion
}