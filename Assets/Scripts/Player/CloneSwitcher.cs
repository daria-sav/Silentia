using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CloneSwitcher : MonoBehaviour
{
    [Header("Profiles in order")]
    public List<CharacterProfile> profiles;

    [Header("Body Parent (where Body instances will live)")]
    public Transform bodyParent;

    private BodyConnector bodyConnector;
    private ProfileApplier profileApplier;
    private Player player;
    private PhysicsControl physicsControl;
    public CharacterProfile CurrentProfile { get; private set; }

    private BaseAbility[] abilities;

    private BodyMarkers currentBody;

    private MultipleJumpAbility jumpAbility;

    private void Awake()
    {
        bodyConnector = GetComponent<BodyConnector>();
        profileApplier = GetComponent<ProfileApplier>();
        player = GetComponent<Player>();
        physicsControl = GetComponent<PhysicsControl>();
        abilities = GetComponents<BaseAbility>();

        // if there is not separate bodyParent, use the current Body_Current
        if (bodyParent == null)
        {
            var found = GetComponentInChildren<BodyMarkers>(true);
            if (found != null) bodyParent = found.transform.parent;
        }

        currentBody = GetComponentInChildren<BodyMarkers>(true);
        if (profiles != null && profiles.Count > 0)
            CurrentProfile = profiles[0];

        jumpAbility = GetComponent<MultipleJumpAbility>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit0Key.wasPressedThisFrame) SwitchTo(0);
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchTo(1);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchTo(2);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchTo(3);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SwitchTo(4);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SwitchTo(5);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SwitchTo(6);
    }

    public void SwitchTo(int index)
    {
        if (profiles == null || index < 0 || index >= profiles.Count) return;

        CharacterProfile profile = profiles[index];
        if (profile == null || profile.bodyPrefab == null) return;

        // save the state of motion
        Vector2 savedVelocity = physicsControl.rb.linearVelocity;
        bool savedFacingRight = player.facingRight;

        // delete the current body
        if (currentBody != null)
        {
            Destroy(currentBody.gameObject);
        }

        // create a new body
        GameObject bodyObj = Instantiate(profile.bodyPrefab, bodyParent);
        bodyObj.transform.localPosition = Vector3.zero;
        bodyObj.transform.localRotation = Quaternion.identity;
        bodyObj.transform.localScale = Vector3.one;

        BodyMarkers newMarkers = bodyObj.GetComponent<BodyMarkers>();
        if (newMarkers == null)
        {
            Debug.LogError("CloneSwitcher: There is no BodyMarkers component on bodyPrefab!");
            return;
        }

        currentBody = newMarkers;

        // connect the dots/visual/anim
        bodyConnector.ApplyBody(newMarkers);

        // Ensure visual faces the same direction as before switching
        Vector3 s = player.visual.localScale;
        s.x = savedFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        player.visual.localScale = s;

        // Make sure the flag matches the visual
        player.facingRight = savedFacingRight;

        // apply profile (stats + permissions)
        profileApplier.ApplyProfile(profile);
        CurrentProfile = profile;
        jumpAbility.ResetJumpState();

        // update the links in the abilities (so that linkedAnimator becomes new)
        foreach (var ab in abilities)
            ab.RefreshLinks();

        // restore velocity
        physicsControl.rb.linearVelocity = savedVelocity;

        Debug.Log(CurrentProfile.id);

        Debug.Log($"[SWITCH RESULT {gameObject.name}] profile={CurrentProfile.id} maxJumps={profile.maxJumps}");
        Debug.Log($"[SWITCH RESULT {gameObject.name}] jump.max={jumpAbility.DebugMaxJumps()} jump.num={jumpAbility.DebugNumJumps()}");
    }
}
