using UnityEngine;
using UnityEngine.InputSystem;

//[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    public GatherInput gatherInput;
    public StateMachine stateMachine;
    public PhysicsControl physicsControl;
    public PlayerStats playerStats;
    public Animator anim;
    public Transform visual;

    private BaseAbility[] playerAbilities;
    public bool facingRight = true;

    public bool restartLevelOnDeath = true;

    public PlayerMovement motor { get; private set; }

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
        //Flip();
        //Debug.Log("Current state is: " + stateMachine.currentState);
    }

    private void LateUpdate()
    {
        if (motor == null || visual == null) return;

        var s = visual.localScale;
        s.x = motor.IsFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        visual.localScale = s;

        facingRight = motor.IsFacingRight;
    }

    private void FixedUpdate()
    {
        if (gameObject.name.Contains("GhostRoot"))
            //Debug.Log($"[GHOST FIXED] state={stateMachine.currentState} v={physicsControl.rb.linearVelocity}");
        foreach (BaseAbility ability in playerAbilities)
        {
            if (ability.thisAbilityState == stateMachine.currentState)
            {
                ability.ProcessFixedAbility();
            }
        }
    }

    public void Flip()
    {
        if (facingRight==true && gatherInput.horizontalInput < 0)
        {
            Vector3 s = visual.localScale;
            s.x = -Mathf.Abs(s.x);
            visual.localScale = s;
            facingRight = !facingRight;
        }
        else if (facingRight==false && gatherInput.horizontalInput > 0)
        {
            Vector3 s = visual.localScale;
            s.x = Mathf.Abs(s.x);
            visual.localScale = s;
            facingRight = !facingRight;
        }
    }

    public void ForceFlip()
    {
        Vector3 s = visual.localScale;
        s.x *= -1f;
        visual.localScale = s;
        facingRight = !facingRight;
    }

    public void SetCurrentStats(PlayerStats stats)
    {
        playerStats = stats;
    }

    public void RefreshStatsFromChildren()
    {
        playerStats = GetComponentInChildren<PlayerStats>(true);
    }

    public void RefreshMotorFromChildren()
    {
        var found = GetComponentInChildren<PlayerMovement>(true);
        Debug.Log($"[Player.RefreshMotor] found={found != null} on '{(found != null ? found.gameObject.name : "NULL")}' caller={new System.Diagnostics.StackTrace()}");
        motor = found;
        //motor = GetComponentInChildren<PlayerMovement>(true);
        if (motor == null)
            Debug.LogError("[Player] PlayerMovement not found in children!");

        foreach (var a in GetComponents<BaseAbility>())
            a.RefreshLinks();
    }

    void Start()
    {
        var allPI = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        Debug.Log($"[Player] PlayerInput count in scene: {allPI.Length}");
        foreach (var pi in allPI) Debug.Log($"  -> PlayerInput on '{pi.gameObject.name}'");
    }
}