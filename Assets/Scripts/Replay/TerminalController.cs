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

    [Header("Slot UI")]
    [SerializeField] private GameObject slotUiPanel;

    private TerminalSession session;
    private bool subscribed;

    private GatherInput gatherInput;
    private bool playerInZone;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (terminalInput == null) terminalInput = GetComponent<TerminalInput>();
    }

    private void OnEnable()
    {
        TryBindSession();
        UpdateSlotUiVisibility();
    }

    private void OnDisable()
    {
        UnbindSession();
    }

    private void Update()
    {
        TryBindSession();

        if (session == null)
            return;

        bool paused = session.State == TerminalSession.TerminalState.TerminalPaused;

        if (terminalInput != null)
            terminalInput.SetTerminalPaused(paused);

        if (paused)
        {
            HandlePausedInput();
            return;
        }

        HandleNormalInput();
    }

    private void HandlePausedInput()
    {
        if (terminalInput == null || session == null)
            return;

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
        }
    }

    private void HandleNormalInput()
    {
        if (!playerInZone || session == null || gatherInput == null)
            return;

        if (gatherInput.ConsumeInteractDown())
        {
#if UNITY_EDITOR
            Debug.Log("[TerminalController] Interact -> entering terminal");
#endif
            if (slotUiPanel != null)
                slotUiPanel.SetActive(false);

            session.RequestRestartAndEnterTerminal();
        }
    }

    private void TryBindSession()
    {
        if (session != null)
            return;

        session = TerminalSession.Instance;
        if (session == null)
            return;

        if (!subscribed)
        {
            session.OnStateChanged += HandleTerminalStateChanged;
            UpdateSlotUiVisibility();
            subscribed = true;
        }
    }

    private void UnbindSession()
    {
        if (session != null && subscribed)
        {
            session.OnStateChanged -= HandleTerminalStateChanged;
            subscribed = false;
        }

        session = null;
    }

    private void HandleTerminalStateChanged(TerminalSession.TerminalState _)
    {
        UpdateSlotUiVisibility();
    }

    private void UpdateSlotUiVisibility()
    {
        if (slotUiPanel == null)
            return;

        if (session == null)
        {
            slotUiPanel.SetActive(false);
            return;
        }

        slotUiPanel.SetActive(session.State == TerminalSession.TerminalState.TerminalPaused);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        playerInZone = true;

        gatherInput = player.gatherInput != null ? player.gatherInput : player.GetComponent<GatherInput>();
        gatherInput?.ClearInteractBuffered();

#if UNITY_EDITOR
        Debug.Log("[TerminalController] ENTER zone");
#endif
        UpdateSlotUiVisibility();

        TerminalSession.Instance?.SetTerminalSpawn(terminalSpawnKey, facingRightAtTerminal);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

#if UNITY_EDITOR
        Debug.Log("[TerminalController] EXIT zone");
#endif

        playerInZone = false;
        gatherInput = null;
        UpdateSlotUiVisibility();
    }

    private void TryPlayAllClips()
    {
        if (session == null)
            return;

        if (ghostRootPrefab == null)
        {
            Debug.LogError("TerminalController: ghostRootPrefab is not assigned.");
            return;
        }

        var hero = FindFirstObjectByType<Player>();
        var recorder = hero != null ? hero.GetComponent<ReplayRecorder>() : null;

        if (recorder != null && recorder.IsRecording)
        {
#if UNITY_EDITOR
            Debug.Log("TerminalController: Cannot play while recording.");
#endif
            return;
        }

        bool playedAny = false;

        for (int i = 0; i < TerminalSession.SlotCount; i++)
        {
            var clip = session.Clips[i];
            if (clip == null) continue;

            string profileId = session.ClipProfileIds[i];
            SpawnAndPlayGhostForClip(clip, profileId);
            playedAny = true;
        }

#if UNITY_EDITOR
        if (!playedAny) Debug.Log("TerminalController: No clips to play.");
#endif
    }

    private void SpawnAndPlayGhostForClip(ReplayClip clip, string profileId)
    {
        Vector3 spawnPos = ghostSpawnPoint != null ? ghostSpawnPoint.position : transform.position;

        var ghost = Instantiate(ghostRootPrefab, spawnPos, Quaternion.identity);

        var applier = ghost.GetComponent<ProfileApplier>();
        if (applier != null) applier.DisableAutoApplyOnStart();

        var switcher = ghost.GetComponent<CloneSwitcher>() ?? ghost.GetComponentInChildren<CloneSwitcher>();
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

        var playback = ghost.GetComponent<ReplayPlayback>() ?? ghost.AddComponent<ReplayPlayback>();
        playback.StartPlayback(clip);
    }
}