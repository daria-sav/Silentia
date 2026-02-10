using UnityEngine;

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

    private void Awake()
    {
        stateMachine = new StateMachine();
        playerAbilities = GetComponents<BaseAbility>();
        stateMachine.abilitiesArr = playerAbilities;
        RefreshStatsFromChildren();
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
        Flip();
        Debug.Log("Current state is: " + stateMachine.currentState);
    }

    private void FixedUpdate()
    {
        if (gameObject.name.Contains("GhostRoot"))
            Debug.Log($"[GHOST FIXED] state={stateMachine.currentState} v={physicsControl.rb.linearVelocity}");
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
}
