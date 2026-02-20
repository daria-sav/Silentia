using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TerminalSession : MonoBehaviour
{
    public static TerminalSession Instance { get; private set; }

    public const int SlotCount = 3;

    [Header("Replay slots (runtime)")]
    public ReplayClip[] Clips { get; private set; } = new ReplayClip[SlotCount];
    public string[] ClipProfileIds { get; private set; } = new string[SlotCount];
    public int SelectedSlot { get; private set; }

    public event Action OnSlotsChanged;

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

    // ===== Spawn persist =====
    private string terminalSpawnKey;
    private bool terminalFacingRight = true;

    private bool restartInProgress;

    private ReplayRecorder boundRecorder;

    private void Awake()
    {
        // Singleton + survive scene reload
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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

    public void SelectSlot(int slot)
    {
        var hero = FindFirstObjectByType<Player>();
        var recorder = hero != null ? hero.GetComponent<ReplayRecorder>() : null;
        if (recorder != null && recorder.IsRecording) return;

        SelectedSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
        OnSlotsChanged?.Invoke();
    }

    public bool HasClip(int slot)
    {
        return slot >= 0 && slot < SlotCount && Clips[slot] != null;
    }

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
        Time.timeScale = 1f;
        SetState(TerminalState.Normal);

        var hero = FindFirstObjectByType<Player>();
        if (hero != null && hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
    }

    public void UnfreezeAfterTerminalPlay()
    {
        Time.timeScale = 1f;
        SetState(TerminalState.Playback);

        var hero = FindFirstObjectByType<Player>();
        if (hero != null && hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        restartInProgress = false;

        var hero = FindFirstObjectByType<Player>();
        if (hero == null)
        {
            Debug.LogError("TerminalSession: Player not found after scene reload.");
            return;
        }

        var switcher = hero.GetComponent<CloneSwitcher>();
        var recorder = hero.GetComponent<ReplayRecorder>();

        if (switcher == null || recorder == null)
        {
            Debug.LogError("TerminalSession: Missing CloneSwitcher or ReplayRecorder on Player.");
            return;
        }

        BindRecorder(recorder);

        switcher.SetHotkeysEnabled(false);

        if (pendingAction == PendingAction.EnterTerminal)
        {
            pendingAction = PendingAction.None;
            pendingProfileIndex = -1;

            EnterTerminalPaused(hero, switcher);
            SetState(TerminalState.TerminalPaused);
            return;
        }

        if (pendingAction == PendingAction.StartRecording)
        {
            pendingAction = PendingAction.None;

            PrepareHeroForRecording(hero, switcher, pendingProfileIndex);

            pendingProfileIndex = -1;

            Time.timeScale = 1f;

            if (hero.gatherInput != null)
                hero.gatherInput.EnablePlayerMap();

            recorder.StartRecording();
            SetState(TerminalState.Recording);

            return;
        }

        // No pending action -> normal load
        Time.timeScale = 1f;
        SetState(TerminalState.Normal);

        switcher.SwitchTo(0);

        if (hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
    }

    private void PrepareHeroForRecording(Player hero, CloneSwitcher switcher, int profileIndex)
    {
        var applier = hero.GetComponent<ProfileApplier>();
        if (applier != null)
            applier.DisableAutoApplyOnStart();

        if (profileIndex >= 0)
            switcher.SwitchTo(profileIndex);
    }

    private void EnterTerminalPaused(Player hero, CloneSwitcher switcher)
    {
        // freeze world
        Time.timeScale = 0f;

        // lock hero input + ensure idle and no movement
        if (hero.gatherInput != null)
            hero.gatherInput.DisablePlayerMap();

        if (hero.physicsControl != null && hero.physicsControl.rb != null)
            hero.physicsControl.rb.linearVelocity = Vector2.zero;

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

        if (LevelManager.instance != null)
            LevelManager.instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ClearSelectedSlot()
    {
        int s = Mathf.Clamp(SelectedSlot, 0, SlotCount - 1);

        Clips[s] = null;
        ClipProfileIds[s] = null;

        OnSlotsChanged?.Invoke();
    }

    private void SaveSpawnForNextLoad()
    {
        if (string.IsNullOrEmpty(terminalSpawnKey)) return;
        if (SaveLoadManager.instance == null) return;

        var data = new SpawnData
        {
            spawnPintKey = terminalSpawnKey,
            facingRight = terminalFacingRight
        };

        SaveLoadManager.instance.Save(data, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);
    }

    private void SetState(TerminalState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }

    public bool TryGetProfileId(int profileIndex, out string profileId)
    {
        profileId = null;

        var hero = FindFirstObjectByType<Player>();
        var switcher = hero != null ? hero.GetComponent<CloneSwitcher>() : null;
        if (switcher == null || switcher.profiles == null) return false;

        int idx = profileIndex; 

        if (idx < 0 || idx >= switcher.profiles.Count) return false;
        var p = switcher.profiles[idx];
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

        int usedSlot;
        if (IsProfileAlreadyUsed(profileId, ignoreSlot: SelectedSlot, out usedSlot))
        {
            message = $"This clone is already used in Slot {usedSlot + 1}.";
            return false;
        }

        return true;
    }
}