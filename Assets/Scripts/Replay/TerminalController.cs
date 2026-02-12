using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class TerminalController : MonoBehaviour
{
    [Header("Ghost playback")]
    [SerializeField] private GameObject ghostRootPrefab;
    [SerializeField] private Transform ghostSpawnPoint;


    [Header("Terminal spawn control")]
    [SerializeField] private string terminalSpawnKey = "Terminal";
    [SerializeField] private bool facingRightAtTerminal = true;
    //private bool playbackSpawned;

    private bool playerInZone;

    [Header("Slot UI")]
    [SerializeField] private GameObject slotUiPanel;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        UpdateSlotUiVisibility();

        if (!playerInZone) return;

        int selected = GetDigit1to6Down();
        if (selected != -1)
        {
            int profileIndex = selected; 
            if (TerminalSession.Instance == null)
            {
                Debug.LogError("TerminalController: TerminalSession not found in scene.");
                return;
            }

            if (slotUiPanel != null) slotUiPanel.SetActive(false);

            TerminalSession.Instance.RequestRestartAndStartRecording(profileIndex);
            return;
        }

        // C - playback
        if (IsPlayDown())
        {
            TryPlayAllClips();
        }
    }

    private void TryPlayAllClips()
    {
        var session = TerminalSession.Instance;

        if (session == null)
        {
            Debug.LogError("TerminalController: TerminalSession not found.");
            return;
        }

        var hero = FindFirstObjectByType<Player>();
        var recorder = hero != null ? hero.GetComponent<ReplayRecorder>() : null;

        if (recorder != null && recorder.IsRecording)
        {
            Debug.Log("TerminalController: Cannot play while recording.");
            return;
        }

        if (ghostRootPrefab == null)
        {
            Debug.LogError("TerminalController: ghostRootPrefab is not assigned.");
            return;
        }

        bool playedAny = false;

        for (int i = 0; i < TerminalSession.SlotCount; i++)
        {
            var clip = session.Clips[i];
            if (clip == null) continue;

            string profileId = session.ClipProfileIds[i];
            SpawnAndPlayGhostForClip(clip, profileId, i);
            playedAny = true;
        }

        if (!playedAny)
            Debug.Log("TerminalController: No clips to play.");
    }

    private void SpawnAndPlayGhostForClip(ReplayClip clip, string profileId, int slotIndex)
    {
        Vector3 spawnPos = (ghostSpawnPoint != null) ? ghostSpawnPoint.position : transform.position;

        //spawnPos.x += slotIndex * 0.6f;

        GameObject ghost = Instantiate(ghostRootPrefab, spawnPos, Quaternion.identity);

        var applier = ghost.GetComponent<ProfileApplier>();
        if (applier != null) applier.DisableAutoApplyOnStart();

        var switcher = ghost.GetComponent<CloneSwitcher>();
        if (switcher == null) switcher = ghost.GetComponentInChildren<CloneSwitcher>();

        if (switcher == null)
        {
            Debug.LogError("Ghost has no CloneSwitcher");
            Destroy(ghost);
            return;
        }

        int idx = switcher.profiles.FindIndex(p => p != null && p.id == profileId);
        if (idx < 0)
        {
            Debug.LogError($"Ghost profile not found for id={profileId}");
            Destroy(ghost);
            return;
        }

        switcher.SwitchTo(idx);

        var playback = ghost.GetComponent<ReplayPlayback>();
        if (playback == null) playback = ghost.AddComponent<ReplayPlayback>();

        playback.StartPlayback(clip);
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

    private void UpdateSlotUiVisibility()
    {
        if (slotUiPanel == null) return;

        bool isRecording = false;
        var hero = FindFirstObjectByType<Player>();
        var recorder = hero != null ? hero.GetComponent<ReplayRecorder>() : null;
        if (recorder != null) isRecording = recorder.IsRecording;

        slotUiPanel.SetActive(playerInZone && !isRecording);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;

        UpdateSlotUiVisibility();

        if (TerminalSession.Instance != null)
            TerminalSession.Instance.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            UpdateSlotUiVisibility();
        }
    }
}