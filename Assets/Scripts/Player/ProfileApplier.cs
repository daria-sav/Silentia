using System.Linq;
using UnityEngine;

public class ProfileApplier : MonoBehaviour
{
    [SerializeField] private CharacterProfile startingProfile;
    [SerializeField] private bool applyOnStart = true;

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
        if (!applyOnStart) return;
        if (startingProfile != null)
            ApplyProfile(startingProfile);
    }

    public void DisableAutoApplyOnStart()  
    {
        applyOnStart = false;
    }

    public void ApplyProfile(CharacterProfile profile)
    {
        if (profile == null) return;

        // movement parameters
        if (move != null) move.SetSpeed(profile.walkSpeed);
        if (physicsControl != null) physicsControl.SetBaseGravity(profile.baseGravity);

        // jump
        if (jump != null)
        {
            jump.SetJumpForce(profile.jumpForce);
            jump.SetMaxJumpNumber(profile.maxJumps);
            jump.SetAirSpeed(profile.airSpeed);
            jump.SetGravityDivider(profile.gravityDivider);
        }

        // abilities permits
        foreach (var ab in abilities)
        {
            ab.isPermitted = profile.permittedStates.Contains(ab.thisAbilityState);
        }

        if (player != null && player.stateMachine != null && !profile.permittedStates.Contains(player.stateMachine.currentState))
        {
            player.stateMachine.ForceChange(PlayerStates.State.Idle);
        }
    }
}
