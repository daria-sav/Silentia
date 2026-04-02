using UnityEngine;

public class BaseAbility : MonoBehaviour
{
    protected Player player;

    protected GatherInput linkedInput;
    protected StateMachine linkedStateMachine;
    protected PhysicsControl linkedPhysics;
    protected Animator linkedAnimator;

    public PlayerStates.State thisAbilityState;
    public bool isPermitted = true;

    protected PlayerMovement linkedMotor;

    protected virtual void Awake()
    {
        Initialization();
    }

    public void RefreshLinks()
    {
        Initialization();
    }

    public virtual void EnterAbility()
    {

    }

    public virtual void ExitAbility()
    {

    }

    public virtual void ProcessAbility()
    {

    }

    public virtual void ProcessFixedAbility()
    {

    }

    public virtual void UpdateAnimator()
    {

    }

    protected virtual void Initialization()
    {
        player = GetComponent<Player>();
        if (player == null) return;

        linkedInput = player.gatherInput;
        linkedStateMachine = player.stateMachine;
        linkedPhysics = player.physicsControl;
        linkedAnimator = player.anim;

        linkedMotor = player.motor;

        if (linkedMotor == null)
            linkedMotor = player.GetComponentInChildren<PlayerMovement>(true);

        if (linkedMotor == null)
            Debug.LogError($"[{GetType().Name}] linkedMotor not found (player.motor is null and none in children).");
    }
}
