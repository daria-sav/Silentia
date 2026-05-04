using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for the replay system. Handles the full lifecycle:
/// entering the terminal, selecting slots, starting/stopping recordings,
/// saving clips, and spawning ghosts for playback.
///
/// Singleton with DontDestroyOnLoad — survives scene reloads so that
/// recorded clips persist between level restarts.
/// </summary>
public class TerminalSession : MonoBehaviour
{
    public static TerminalSession Instance { get; private set; }

    public const int SlotCount = 3;

    [Header("Replay slots (runtime)")]
    public ReplayClip[] Clips { get; private set; } = new ReplayClip[SlotCount];
    public string[] ClipProfileIds { get; private set; } = new string[SlotCount];
    public int SelectedSlot { get; private set; }

    public event Action OnSlotsChanged;

    [Header("Ghost playback")]
    [SerializeField] private GameObject ghostRootPrefab;
    [SerializeField] private string ghostSpawnKey = "Terminal";
    private Transform ghostSpawnPointRuntime;

    public enum TerminalState
    {
        Normal,
        EnteringTerminal,
        TerminalPaused,
        EnteringRecord,
        Recording,
        Playback
    }

    public TerminalState State { get; private set; } = TerminalState.Normal;
    public event Action<TerminalState> OnStateChanged;

    private enum PendingAction
    {
        None,
        EnterTerminal,
        StartRecording
    }

    private PendingAction pendingAction = PendingAction.None;
    private int pendingProfileIndex = -1;

    // spawn persist
    private string terminalSpawnKey;
    private bool terminalFacingRight = true;

    // internals 
    private bool restartInProgress;
    private ReplayRecorder boundRecorder;
    private string lastSceneName;
    private Player cachedHero;

    // ───────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        lastSceneName = SceneManager.GetActiveScene().name;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BindRecorder(null);
            Instance = null;
        }
    }
    #endregion

    // ────────────── SLOT API ─────────────────

    #region Slot API
    public void SelectSlot(int slot)
    {
        if (boundRecorder != null && boundRecorder.IsRecording) return;

        SelectedSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
        OnSlotsChanged?.Invoke();
    }

    public bool HasClip(int slot)
    {
        return slot >= 0 && slot < SlotCount && Clips[slot] != null;
    }

    public Sprite GetSlotIcon(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
            return null;

        if (Clips[slot] == null)
            return null;

        string profileId = ClipProfileIds[slot];
        if (string.IsNullOrEmpty(profileId))
            return null;

        return GetProfileIconById(profileId);
    }

    private Sprite GetProfileIconById(string profileId)
    {
        if (string.IsNullOrEmpty(profileId) || cachedHero == null)
            return null;

        var switcher = cachedHero.GetComponent<CloneSwitcher>();
        if (switcher == null || switcher.profiles == null)
            return null;

        foreach (var profile in switcher.profiles)
        {
            if (profile == null)
                continue;

            if (profile.id == profileId)
                return profile.slotIcon;
        }

        return null;
    }

    public void ClearSelectedSlot()
    {
        int selected = Mathf.Clamp(SelectedSlot, 0, SlotCount - 1);

        Clips[selected] = null;
        ClipProfileIds[selected] = null;

        OnSlotsChanged?.Invoke();
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Clips[i] = null;
            ClipProfileIds[i] = null;
        }
        SelectedSlot = 0;
        OnSlotsChanged?.Invoke();
    }

    public List<CharacterProfile> GetProfiles()
    {
        if (cachedHero == null) return null;
        var switcher = cachedHero.GetComponent<CloneSwitcher>();
        return switcher?.profiles;
    }
    #endregion

    // ──────────── TERMINAL FLOW ──────────────

    #region Terminal Flow
    public void SetTerminalSpawn(string spawnKey, bool facingRight)
    {
        terminalSpawnKey = spawnKey;
        terminalFacingRight = facingRight;
    }

    public void RequestRestartAndEnterTerminal()
    {
        if (!BeginRestart(PendingAction.EnterTerminal))
            return;

        SetState(TerminalState.EnteringTerminal);
        SaveSpawnForNextLoad();
        RestartLevel();
    }

    public void RequestRestartAndStartRecording(int profileIndex)
    {
        if (!BeginRestart(PendingAction.StartRecording))
            return;

        pendingProfileIndex = profileIndex;

        SetState(TerminalState.EnteringRecord);
        SaveSpawnForNextLoad();
        RestartLevel();
    }

    public void SaveClipToSelectedSlotAndRestart(ReplayClip clip)
    {
        if (clip == null)
            return;

        if (!BeginRestart(PendingAction.EnterTerminal))
            return;

        Clips[SelectedSlot] = clip;
        ClipProfileIds[SelectedSlot] = clip.profileId;
        OnSlotsChanged?.Invoke();

        SetState(TerminalState.EnteringTerminal);
        SaveSpawnForNextLoad();
        RestartLevel();
    }

    public void ExitTerminalPaused()
    {
        ResumeWorld(TerminalState.Normal);
    }

    public void UnfreezeAfterTerminalPlay()
    {
        ResumeWorld(TerminalState.Playback);
    }
    #endregion

    // ──────────── PROFILE VALIDATION ─────────

    #region Profile Validation
    public bool TryGetProfileId(int profileIndex, out string profileId)
    {
        profileId = null;

        var switcher = cachedHero != null ? cachedHero.GetComponent<CloneSwitcher>() : null;
        if (switcher == null || switcher.profiles == null) return false;

        if (profileIndex < 0 || profileIndex >= switcher.profiles.Count) return false;

        var p = switcher.profiles[profileIndex];
        if (p == null) return false;

        profileId = p.id;
        return !string.IsNullOrEmpty(profileId);
    }

    public bool IsProfileAlreadyUsed(string profileId, int ignoreSlot, out int usedSlot)
    {
        usedSlot = -1;
        if (string.IsNullOrEmpty(profileId)) return false;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i == ignoreSlot) continue;
            if (!string.IsNullOrEmpty(ClipProfileIds[i]) && ClipProfileIds[i] == profileId)
            {
                usedSlot = i;
                return true;
            }
        }
        return false;
    }

    public bool CanStartRecordingWithProfile(int profileIndex, out string message)
    {
        message = null;

        if (State != TerminalState.TerminalPaused)
        {
            message = "You can only start recording from the terminal menu.";
            return false;
        }

        if (!TryGetProfileId(profileIndex, out var profileId))
        {
            message = "Profile not found.";
            return false;
        }

        if (IsProfileAlreadyUsed(profileId, ignoreSlot: SelectedSlot, out int usedSlot))
        {
            message = $"This clone is already used in Slot {usedSlot + 1}.";
            return false;
        }

        return true;
    }
    #endregion

    // ──────────── GHOST SPAWNING ─────────────

    #region Ghost Spawning
    // spawns ghosts for ALL filled slots
    public void PlayAllClips()
    {
        PlayAllExistingClipsExcept(-1);
    }

    private void PlayAllExistingClipsExcept(int ignoreSlot)
    {
        if (ghostRootPrefab == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i == ignoreSlot) continue;

            if (Clips[i] == null) continue;

            SpawnAndPlayGhostForClip(Clips[i], ClipProfileIds[i]);
        }
    }

    public void SpawnAndPlayGhostForClip(ReplayClip clip, string profileId)
    {
        Vector3 spawnPos = ghostSpawnPointRuntime != null
            ? ghostSpawnPointRuntime.position
            : (cachedHero != null ? cachedHero.transform.position : Vector3.zero);

        var ghost = Instantiate(ghostRootPrefab, spawnPos, Quaternion.identity);

        var bodySetup = ghost.GetComponent<BodySetup>();
        if (bodySetup != null) bodySetup.DisableAutoApplyOnStart();

        var switcher = ghost.GetComponent<CloneSwitcher>() ?? ghost.GetComponentInChildren<CloneSwitcher>();
        if (switcher == null)
        {
            Destroy(ghost);
            return;
        }

        int idx = switcher.profiles.FindIndex(p => p != null && p.id == profileId);
        if (idx < 0)
        {
            Destroy(ghost);
            return;
        }

        switcher.SwitchTo(idx);

        var visualStyle = ghost.GetComponent<GhostVisualStyle>();
        if (visualStyle != null)
        {
            visualStyle.ApplyStyle();
        }

        var playback = ghost.GetComponent<ReplayPlayback>() ?? ghost.AddComponent<ReplayPlayback>();
        playback.StartPlayback(clip);
    }
    #endregion

    // ──────────── SCENE LOADING ──────────────

    #region Scene Loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(lastSceneName) && lastSceneName != scene.name)
        {
            ClearAllSlots();
        }

        lastSceneName = scene.name;
        restartInProgress = false;

        ResolveGhostSpawnPoint();

        cachedHero = FindFirstObjectByType<Player>();

        if (cachedHero == null)
        {
            Debug.LogError("TerminalSession: Player not found after scene reload.");
            return;
        }

        var switcher = cachedHero.GetComponent<CloneSwitcher>();
        var recorder = cachedHero.GetComponent<ReplayRecorder>();

        if (switcher == null || recorder == null)
        {
            Debug.LogError("TerminalSession: Missing CloneSwitcher or ReplayRecorder on Player.");
            return;
        }

        BindRecorder(recorder);

        // execute pending action from before the restart
        if (pendingAction == PendingAction.EnterTerminal)
        {
            pendingAction = PendingAction.None;
            pendingProfileIndex = -1;

            EnterTerminalPaused(cachedHero, switcher);
            SetState(TerminalState.TerminalPaused);
            return;
        }

        if (pendingAction == PendingAction.StartRecording)
        {
            pendingAction = PendingAction.None;

            PrepareHeroForRecording(cachedHero, switcher, pendingProfileIndex);

            pendingProfileIndex = -1;

            Time.timeScale = 1f;

            cachedHero.gatherInput?.EnablePlayerMap();

            StartCoroutine(BeginRecordingNextFrame(recorder));

            return;
        }

        // no pending action -> normal load
        Time.timeScale = 1f;
        SetState(TerminalState.Normal);

        switcher.SwitchTo(0);

        cachedHero.gatherInput?.EnablePlayerMap();
    }
    #endregion

    // ──────────── INTERNAL HELPERS ────────────

    #region Internal Helpers
    private void ResumeWorld(TerminalState newState)
    {
        Time.timeScale = 1f;
        SetState(newState);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (cachedHero != null && cachedHero.gatherInput != null)
            cachedHero.gatherInput.EnablePlayerMap();
    }

    private void PrepareHeroForRecording(Player hero, CloneSwitcher switcher, int profileIndex)
    {
        var bodySetup = hero.GetComponent<BodySetup>();
        if (bodySetup != null)
            bodySetup.DisableAutoApplyOnStart();

        if (profileIndex >= 0)
            switcher.SwitchTo(profileIndex);
    }

    private void EnterTerminalPaused(Player hero, CloneSwitcher switcher)
    {
        CameraManager.instance?.SnapToTarget();

        // freeze world
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        hero.gatherInput?.DisablePlayerMap();

        if (hero.motor != null)
            hero.motor.RB.linearVelocity = Vector2.zero;

        hero.stateMachine.ForceChange(PlayerStates.State.Idle);

        switcher.SwitchTo(0);
    }

    private void BindRecorder(ReplayRecorder recorder)
    {
        if (boundRecorder == recorder) return;

        if (boundRecorder != null)
            boundRecorder.OnRecordingStopped -= HandleRecordingStopped;

        boundRecorder = recorder;

        if (boundRecorder != null)
            boundRecorder.OnRecordingStopped += HandleRecordingStopped;
    }

    private void HandleRecordingStopped(ReplayClip clip)
    {
        // clip is already complete here
        SaveClipToSelectedSlotAndRestart(clip);
    }

    private bool BeginRestart(PendingAction action)
    {
        if (restartInProgress)
            return false;

        restartInProgress = true;
        pendingAction = action;
        return true;
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
            LevelManager.Instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SaveSpawnForNextLoad()
    {
        if (string.IsNullOrEmpty(terminalSpawnKey)) 
            return;

        if (SaveLoadManager.Instance == null) 
            return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        SaveLoadManager.Instance.SaveSpawnData(
            currentSceneName,
            terminalSpawnKey,
            terminalFacingRight);
    }

    private void SetState(TerminalState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }

    private void ResolveGhostSpawnPoint()
    {
        ghostSpawnPointRuntime = null;

        var points = FindObjectsByType<SpawnIdentifier>(FindObjectsSortMode.None);

        foreach (var p in points)
        {
            if (p != null && p.spawnKey == ghostSpawnKey)
            {
                ghostSpawnPointRuntime = p.transform;
                return;
            }
        }
    }

    private System.Collections.IEnumerator BeginRecordingNextFrame(ReplayRecorder recorder)
    {
        yield return null; 

        recorder.StartRecording();
        SetState(TerminalState.Recording);
        PlayAllExistingClipsExcept(SelectedSlot);
    }
    #endregion
}