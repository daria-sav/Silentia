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
    [SerializeField] private TerminalInput terminalInput;

    private bool playerInZone;

    [Header("Slot UI")]
    [SerializeField] private GameObject slotUiPanel;

    private TerminalSession cachedSession;

    private void Awake()
    {
        if (terminalInput == null) terminalInput = GetComponent<TerminalInput>();
    }

    private void OnEnable()
    {
        cachedSession = TerminalSession.Instance;
        if (cachedSession != null)
            cachedSession.OnStateChanged += HandleTerminalStateChanged;
    }

    private void OnDisable()
    {
        if (cachedSession != null)
            cachedSession.OnStateChanged -= HandleTerminalStateChanged;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        var session = cachedSession;
        if (session == null || terminalInput == null) return;

        bool paused = session.State == TerminalSession.TerminalState.TerminalPaused;

        // Terminal map включается только когда мы реально в TerminalPaused
        terminalInput.SetTerminalPaused(paused);

        // В обычном режиме терминал "вооружается" только внутри зоны
        if (!paused && playerInZone)
            terminalInput.UpdateArming();

        // ===== TerminalPaused =====
        if (paused)
        {
            if (terminalInput.ExitDown())
            {
                session.ExitTerminalPaused();
                return;
            }

            int profileIndex = terminalInput.ProfileDown();
            if (profileIndex != -1)
            {
                session.RequestRestartAndStartRecording(profileIndex);
                return;
            }

            if (terminalInput.PlayDown())
            {
                TryPlayAllClips();
                session.UnfreezeAfterTerminalPlay();
                return;
            }

            return;
        }

        // ===== Normal =====
        if (!playerInZone) return;

        if (terminalInput.InteractDown())
        {
            if (slotUiPanel != null) slotUiPanel.SetActive(false);
            session.RequestRestartAndEnterTerminal();
        }
    }

    private void TryPlayAllClips()
    {
        var session = cachedSession;

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


    private void UpdateSlotUiVisibility()
    {
        if (slotUiPanel == null) return;

        var session = cachedSession;
        if (session == null)
        {
            slotUiPanel.SetActive(false);
            return;
        }

        slotUiPanel.SetActive(session.State == TerminalSession.TerminalState.TerminalPaused);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;
        terminalInput.NotifyEnteredZone();

        UpdateSlotUiVisibility();

        if (TerminalSession.Instance != null)
            TerminalSession.Instance.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = false;
        terminalInput.NotifyExitedZone();

        UpdateSlotUiVisibility();
    }

    private void HandleTerminalStateChanged(TerminalSession.TerminalState _)
    {
        UpdateSlotUiVisibility();
    }
}