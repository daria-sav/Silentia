using UnityEngine;

/// <summary>
/// Handles all body-dependent setup in one place:
/// reconnects visual/motor/stats references from BodyMarkers
/// and applies ability permissions from CharacterProfile.
/// </summary>
public class BodySetup : MonoBehaviour
{
    [SerializeField] private CharacterProfile startingProfile;
    [SerializeField] private bool applyOnStart = true;

    private Player player;
    private BaseAbility[] abilities;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        player = GetComponent<Player>();
        abilities = GetComponents<BaseAbility>();

        // connect the initial body that already exists in the hierarchy
        var initialBody = GetComponentInChildren<BodyMarkers>(true);
        if (initialBody != null)
            ConnectBody(initialBody);
    }

    private void Start()
    {
        if (applyOnStart && startingProfile != null)
            ApplyPermissions(startingProfile);
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API

    // full body setup: reconnect references + apply profile permissions
    public void ApplyBodyAndProfile(BodyMarkers body, CharacterProfile profile)
    {
        ConnectBody(body);
        ApplyPermissions(profile);
    }

    // prevents the starting profile from being applied in Start()
    public void DisableAutoApplyOnStart()
    {
        applyOnStart = false;
    }
    #endregion

    // ──────────── BODY CONNECTION ────────────

    #region Body Connection
    private void ConnectBody(BodyMarkers body)
    {
        if (body == null)
        {
            Debug.LogError("[BodySetup] BodyMarkers is null!");
            return;
        }

        // visual and animator
        player.visual = body.flipRoot;

        if (body.animator != null)
            player.anim = body.animator;

        // motor collision checks
        var motor = GetComponentInChildren<PlayerMovement>(true);
        if (motor != null)
            motor.SetChecks(body.groundCheckPoint, body.frontWallCheckPoint, body.backWallCheckPoint);

        // refresh cached references on Player and all abilities
        player.RefreshMotorFromChildren();
        player.RefreshStatsFromChildren();
    }
    #endregion

    // ────────── ABILITY PERMISSIONS ──────────

    #region Permissions
    private void ApplyPermissions(CharacterProfile profile)
    {
        if (profile == null) return;

        foreach (var ab in abilities)
            ab.isPermitted = profile.permittedStates.Contains(ab.thisAbilityState);

        // fall back to Idle if current state is no longer permitted
        if (player.stateMachine != null
            && !profile.permittedStates.Contains(player.stateMachine.currentState))
        {
            player.stateMachine.ForceChange(PlayerStates.State.Idle);
        }
    }
    #endregion
}