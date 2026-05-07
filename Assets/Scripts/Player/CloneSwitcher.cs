using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages switching between character profiles (clones).
/// Handles the destroy/instantiate cycle for the body prefab
/// and delegates all reconnection to BodySetup.
/// </summary>
public class CloneSwitcher : MonoBehaviour
{
    [Header("Profiles in order")]
    public List<CharacterProfile> profiles;

    [Header("Body Parent (where Body instances will live)")]
    public Transform bodyParent;

    public CharacterProfile CurrentProfile { get; private set; }
    public int CurrentProfileIndex { get; private set; } = 0;

    private BodySetup bodySetup;
    private Player player;
    private BaseAbility[] abilities;
    private BodyMarkers currentBody;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        bodySetup = GetComponent<BodySetup>();
        player = GetComponent<Player>();
        abilities = GetComponents<BaseAbility>();

        if (bodyParent == null)
        {
            var found = GetComponentInChildren<BodyMarkers>(true);
            if (found != null) bodyParent = found.transform.parent;
        }

        currentBody = GetComponentInChildren<BodyMarkers>(true);

        if (profiles != null && profiles.Count > 0)
            CurrentProfile = profiles[0];
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void SwitchTo(int index)
    {
        Debug.Log($"[CS] SwitchTo(index={index}) profilesCount={(profiles != null ? profiles.Count : -1)}");
        if (profiles == null || index < 0 || index >= profiles.Count)
        {
            Debug.LogWarning($"[CS] SwitchTo aborted: invalid index. profiles={(profiles == null ? "null" : profiles.Count.ToString())}");
            return;
        }
        CurrentProfileIndex = index;

        CharacterProfile profile = profiles[index];
        if (profile == null)
        {
            Debug.LogError($"[CS] SwitchTo: profiles[{index}] is NULL! Body NOT swapped, but CurrentProfileIndex set to {index}");
            return;
        }

        if (profile.bodyPrefab == null)
        {
            Debug.LogError($"[CS] SwitchTo: profiles[{index}].bodyPrefab is NULL (id={profile.id})! Body NOT swapped");
            return;
        }

        // save state that must survive the body swap ????????
        Vector2 savedVelocity = (player.motor != null) ? player.motor.RB.linearVelocity : Vector2.zero;
        bool savedFacingRight = player.facingRight;

        // destroy old body immediately
        if (currentBody != null)
        {
            DestroyImmediate(currentBody.gameObject);
        }

        // instantiate new body
        GameObject bodyObj = Instantiate(profile.bodyPrefab, bodyParent);
        bodyObj.transform.localPosition = Vector3.zero;
        bodyObj.transform.localRotation = Quaternion.identity;
        bodyObj.transform.localScale = Vector3.one;

        SetLayerRecursively(bodyObj, gameObject.layer);

        BodyMarkers newMarkers = bodyObj.GetComponent<BodyMarkers>();
        if (newMarkers == null)
        {
            Debug.LogError("CloneSwitcher: There is no BodyMarkers component on bodyPrefab!");
            return;
        }

        currentBody = newMarkers;

        // reconnect visual, animator, checks, motor, stats
        bodySetup.ApplyBodyAndProfile(newMarkers, profile);
        CurrentProfile = profile;

        // restore saved state
        RestoreState(savedVelocity, savedFacingRight);

        Debug.Log($"[CS] SwitchTo: SUCCESS body swapped to id={profile.id}, currentBody={currentBody.name}");
    }
    #endregion

    // ──────────── HELPERS ────────────────────

    #region Helpers
    private void RestoreState(Vector2 velocity, bool facingRight)
    {
        // facing direction
        player.facingRight = facingRight;

        if (player.motor != null)
            player.motor.UpdateFacingDirection(facingRight);

        if (player.visual != null)
        {
            var s = player.visual.localScale;
            s.x = facingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            player.visual.localScale = s;
        }

        // velocity
        if (player.motor != null)
            player.motor.RB.linearVelocity = velocity;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj.GetComponent<PlayerStats>() != null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    #endregion
}