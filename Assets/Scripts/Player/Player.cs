using UnityEngine;

public class Player : MonoBehaviour
{
    public GatherInput gatherInput;
    public StateMachine stateMachine;
    public PhysicsControl physicsControl;
    public Animator anim;

    private BaseAbility[] playerAbilities;
    public bool facingRight = true;

    private void Awake()
    {
        stateMachine = new StateMachine();
        playerAbilities = GetComponents<BaseAbility>();
        stateMachine.abilitiesArr = playerAbilities;
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
        Debug.Log("Current state is: " + stateMachine.currentState);
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

    public void Flip()
    {
        if (facingRight==true && gatherInput.horizontalInput < 0)
        {
            transform.Rotate(0f, 180f, 0f);
            facingRight = !facingRight;
        }
        else if (facingRight==false && gatherInput.horizontalInput > 0)
        {
            transform.Rotate(0f, 180f, 0f);
            facingRight = !facingRight;
        }
    }

    public void ForceFlip()
    {
        transform.Rotate(0f, 180f, 0f);
        facingRight = !facingRight;
    }
}
