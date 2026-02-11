using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class TerminalController : MonoBehaviour
{
    [Header("Ghost playback")]
    [SerializeField] private GameObject ghostRootPrefab;

    [Tooltip("Optional explicit spawn point. If null, terminal transform is used.")]
    [SerializeField] private Transform ghostSpawnPoint;

    private bool playerInZone;

    [Header("Terminal spawn control")]
    [SerializeField] private string terminalSpawnKey = "Terminal";
    [SerializeField] private bool facingRightAtTerminal = true;
    private bool playbackSpawned;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInZone) return;

        int selected = GetDigit1to6Down();
        if (selected != -1)
        {
            int profileIndex = selected - 1; 
            if (TerminalSession.Instance == null)
            {
                Debug.LogError("TerminalController: TerminalSession not found in scene.");
                return;
            }

            TerminalSession.Instance.RequestRestartAndStartRecording(profileIndex);
            return;
        }

        // C - playback
        if (IsPlayDown())
        {
            TryPlayLastClip();
        }
    }

    private void TryPlayLastClip()
    {
        if (playbackSpawned) return;

        var session = TerminalSession.Instance;
        if (session == null)
        {
            Debug.LogError("TerminalController: TerminalSession not found.");
            return;
        }

        var clip = session.LastClip;
        if (clip == null)
        {
            Debug.Log("TerminalController: No saved clip to play.");
            return;
        }

        if (ghostRootPrefab == null)
        {
            Debug.LogError("TerminalController: ghostRootPrefab is not assigned.");
            return;
        }

        var hero = FindFirstObjectByType<Player>();
        if (hero != null)
        {
            var recorder = hero.GetComponent<ReplayRecorder>();
            if (recorder != null && recorder.IsRecording)
            {
                Debug.Log("TerminalController: Cannot play while recording.");
                return;
            }
        }

        Vector3 spawnPos = (ghostSpawnPoint != null) ? ghostSpawnPoint.position : transform.position;

        GameObject ghostRoot = Instantiate(ghostRootPrefab, spawnPos, Quaternion.identity);

        // IMPORTANT: prevent ProfileApplier.Start() from overriding our chosen profile on ghost
        var ghostApplier = ghostRoot.GetComponent<ProfileApplier>();
        if (ghostApplier != null)
            ghostApplier.DisableAutoApplyOnStart();

        // Prefer CloneSwitcher on root, fallback to children
        var ghostSwitcher = ghostRoot.GetComponent<CloneSwitcher>();
        if (ghostSwitcher == null)
            ghostSwitcher = ghostRoot.GetComponentInChildren<CloneSwitcher>();

        if (ghostSwitcher == null)
        {
            Debug.LogError("TerminalController: Ghost has no CloneSwitcher!");
            Destroy(ghostRoot);
            return;
        }

        // Robust: find profile by clip.profileId (works even if profile list ordering differs)
        int idx = ghostSwitcher.profiles.FindIndex(p => p != null && p.id == clip.profileId);
        if (idx < 0)
        {
            Debug.LogError($"TerminalController: Ghost profile not found for id={clip.profileId}");
            Destroy(ghostRoot);
            return;
        }

        ghostSwitcher.SwitchTo(idx);

        // If ProfileApplier is not on root but on the same object as switcher, disable it too (safety)
        var applierOnSwitcher = ghostSwitcher.GetComponent<ProfileApplier>();
        if (applierOnSwitcher != null)
            applierOnSwitcher.DisableAutoApplyOnStart();

        // Playback: prefer on root, fallback to children, add if missing
        var playback = ghostRoot.GetComponent<ReplayPlayback>();
        if (playback == null)
            playback = ghostRoot.GetComponentInChildren<ReplayPlayback>();
        if (playback == null)
            playback = ghostRoot.AddComponent<ReplayPlayback>();

        playback.StartPlayback(clip);

        playbackSpawned = true;
    }

    private int GetDigit1to6Down()
    {
        var kb = Keyboard.current;
        if (kb == null) return -1;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 1;
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 2;
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 3;
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 4;
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 5;
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return 6;

        return -1;
    }

    private bool IsPlayDown()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        // C key
        return kb.cKey.wasPressedThisFrame;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;

        if (TerminalSession.Instance != null)
            TerminalSession.Instance.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            playbackSpawned = false;
        }
    }
}