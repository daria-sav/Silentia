using System.Linq;
using UnityEngine;

public class ProfileApplier : MonoBehaviour
{
    [SerializeField] private CharacterProfile startingProfile;

    private Player player;
    private PhysicsControl physicsControl;

    private MoveAbility move;
    private MultipleJumpAbility jump; 
    private BaseAbility[] abilities;

    private void Awake()
    {
        player = GetComponent<Player>();
        physicsControl = GetComponent<PhysicsControl>();

        abilities = GetComponents<BaseAbility>();

        move = GetComponent<MoveAbility>();
        jump = GetComponent<MultipleJumpAbility>();
    }

    private void Start()
    {
        if (startingProfile != null)
            ApplyProfile(startingProfile);
    }

    public void ApplyProfile(CharacterProfile profile)
    {
        // 1) Movement parameters
        if (move != null) move.SetSpeed(profile.walkSpeed);
        if (physicsControl != null) physicsControl.SetBaseGravity(profile.baseGravity);

        // 2) Jump
        if (jump != null)
        {
            jump.SetJumpForce(profile.jumpForce);
            jump.SetMaxJumpNumber(profile.maxJumps);
            jump.SetAirSpeed(profile.airSpeed);
            jump.SetGravityDivider(profile.gravityDivider);
        }

        // 3) Abilities permits
        foreach (var ab in abilities)
        {
            ab.isPermitted = profile.permittedStates.Contains(ab.thisAbilityState);
        }

        // 4) If the current state is disabled, go to Idle.
        if (!profile.permittedStates.Contains(player.stateMachine.currentState))
        {
            player.stateMachine.ForceChange(PlayerStates.State.Idle);
        }
    }
}
