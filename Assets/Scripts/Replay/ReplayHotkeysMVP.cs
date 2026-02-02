using UnityEngine;
using UnityEngine.InputSystem;

public class ReplayHotkeysMVP : MonoBehaviour
{
    [Header("Hero (the currently controlled Player object)")]
    public Player hero;
    public ReplayRecorder recorder;

    [Header("Ghost")]
    public GameObject ghostRootPrefab;
    public Transform ghostSpawnPoint;

    private ReplayClip lastClip;

    private void Awake()
    {
        if (hero == null) hero = FindFirstObjectByType<Player>();
        if (recorder == null && hero != null) recorder = hero.GetComponent<ReplayRecorder>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Z - start recording
        if (kb.zKey.wasPressedThisFrame)
        {
            if (recorder == null)
            {
                Debug.LogError("ReplayHotkeysMVP: Recorder not found on hero.");
                return;
            }
            recorder.StartRecording();
        }

        // X - stop recording
        if (kb.xKey.wasPressedThisFrame)
        {
            if (recorder == null) return;

            recorder.StopRecording();
            lastClip = recorder.CurrentClip;

            Debug.Log($"REPLAY: Saved clip. Frames={lastClip?.FrameCount ?? 0}, Profile={lastClip?.profileId}");
        }

        // C - spawn ghost + play
        if (kb.cKey.wasPressedThisFrame)
        {
            if (lastClip == null)
            {
                Debug.LogWarning("REPLAY: No clip recorded yet (press F1 then F2).");
                return;
            }

            if (ghostRootPrefab == null)
            {
                Debug.LogError("REPLAY: ghostRootPrefab is not set.");
                return;
            }

            SpawnAndPlayGhost(lastClip);
        }
    }

    private void SpawnAndPlayGhost(ReplayClip clip)
    {
        Vector3 pos = clip.startPosition;
        if (ghostSpawnPoint != null) pos = ghostSpawnPoint.position;

        var ghost = Instantiate(ghostRootPrefab, pos, Quaternion.identity);
        var applier = ghost.GetComponent<ProfileApplier>();
        if (applier != null) applier.DisableAutoApplyOnStart();

        // layer Ghost recursively 
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        if (ghostLayer >= 0) SetLayerRecursively(ghost, ghostLayer);

        // 1) find CloneSwitcher 
        var ghostSwitcher = ghost.GetComponent<CloneSwitcher>();
        if (ghostSwitcher == null)
        {
            Debug.LogError("REPLAY: Ghost has no CloneSwitcher component!");
            Destroy(ghost);
            return;
        }

        // 2) Find the profile index
        int index = -1;

        if (clip.profile != null)
            index = ghostSwitcher.profiles.FindIndex(p => p == clip.profile);

        if (index < 0 && !string.IsNullOrEmpty(clip.profileId))
            index = ghostSwitcher.profiles.FindIndex(p => p != null && p.id == clip.profileId);

        if (index < 0)
        {
            Debug.LogError($"REPLAY: Cannot find profile in ghost profiles list. clip.profileId={clip.profileId}");
            Destroy(ghost);
            return;
        }

        // 3) Switch the ghost to the desired profile 
        ghostSwitcher.SwitchTo(index);

        if (applier != null && clip.profile != null)
        {
            applier.ApplyProfile(clip.profile);
        }

        var ghostPlayer = ghost.GetComponent<Player>();
        var ghostPhysics = ghost.GetComponent<PhysicsControl>();

        var mj = ghost.GetComponent<MultipleJumpAbility>();
        if (mj != null)
        {
            mj.ResetJumpState();
            Debug.Log($"[GHOST AFTER PROFILE] max={mj.DebugMaxJumps()} num={mj.DebugNumJumps()}");
        }

        ghost.transform.position = clip.startPosition;

        if (ghostPhysics != null && ghostPhysics.rb != null)
        {
            ghostPhysics.rb.linearVelocity = clip.startVelocity;
        }

        if (ghostPlayer != null)
        {
            ghostPlayer.facingRight = clip.startFacingRight;
            if (ghostPlayer.visual != null)
            {
                var s = ghostPlayer.visual.localScale;
                s.x = clip.startFacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                ghostPlayer.visual.localScale = s;
            }

            ghostPlayer.stateMachine.ForceChange(PlayerStates.State.Idle);
        }

        // 4) Start playback
        var playback = ghost.GetComponent<ReplayPlayback>();
        if (playback == null) playback = ghost.AddComponent<ReplayPlayback>();

        playback.StartPlayback(clip);

        Debug.Log("REPLAY: Ghost spawned & playback started.");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}