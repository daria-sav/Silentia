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

    public int SelectedSlot { get; private set; } = 0;

    public event Action OnSlotsChanged;

    private string terminalSpawnKey;
    private bool terminalFacingRight = true;

    private bool restartInProgress;

    private ReplayRecorder boundRecorder;

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
        }
    }

    public void SelectSlot(int slot)
    {
        var hero = FindFirstObjectByType<Player>();
        var recorder = hero != null ? hero.GetComponent<ReplayRecorder>() : null;
        if (recorder != null && recorder.IsRecording) return;

        slot = Mathf.Clamp(slot, 0, SlotCount - 1);
        SelectedSlot = slot;
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

    public void RequestRestartAndStartRecording(int profileIndex)
    {
        if (restartInProgress) return;
        restartInProgress = true;

        pendingAction = PendingAction.StartRecording;
        pendingProfileIndex = profileIndex;

        SetState(TerminalState.EnteringRecord);

        SaveSpawnForNextLoad();

        Time.timeScale = 1f;

        if (LevelManager.instance != null)
        {
            LevelManager.instance.RestartLevel();
        }
            
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveClipToSelectedSlotAndRestart(ReplayClip clip)
    {
        if (clip == null) return;
        if (restartInProgress) return;
        restartInProgress = true;

        Clips[SelectedSlot] = clip;
        ClipProfileIds[SelectedSlot] = clip.profileId;
        OnSlotsChanged?.Invoke();

        pendingAction = PendingAction.EnterTerminal;
        SetState(TerminalState.EnteringTerminal);

        Time.timeScale = 1f;

        SaveSpawnForNextLoad();

        if (LevelManager.instance != null) 
            LevelManager.instance.RestartLevel();
        else 
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        restartInProgress = false;

        var hero = FindFirstObjectByType<Player>();
        if (hero == null)
        {
            Debug.LogError("TerminalSession: Player not found after scene reload.");
            restartInProgress = false;
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
            Debug.Log("TerminalSession: Entered TerminalPaused after reload.");
            return;
        }

        if (pendingAction == PendingAction.StartRecording)
        {
            pendingAction = PendingAction.None;

            // гарантируем что стартовый профиль не применится автоматически
            var applier = hero.GetComponent<ProfileApplier>();
            if (applier != null) applier.DisableAutoApplyOnStart();

            // переключаем тело на выбранный профиль
            if (pendingProfileIndex >= 0)
                switcher.SwitchTo(pendingProfileIndex);

            pendingProfileIndex = -1;

            // мир живой
            Time.timeScale = 1f;

            // включаем управление
            if (hero.gatherInput != null)
                hero.gatherInput.EnablePlayerMap();

            recorder.StartRecording();
            SetState(TerminalState.Recording);

            Debug.Log("TerminalSession: Started recording after reload.");
            return;
        }

        // ===== NO PENDING ACTION: NORMAL LOAD =====
        Time.timeScale = 1f;
        SetState(TerminalState.Normal);

        // всегда GG (0)
        switcher.SwitchTo(0);

        // управление включено
        if (hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
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

    public void RequestRestartAndEnterTerminal()
    {
        if (restartInProgress) return;
        restartInProgress = true;

        pendingAction = PendingAction.EnterTerminal;
        SetState(TerminalState.EnteringTerminal);

        SaveSpawnForNextLoad();

        Time.timeScale = 1f;

        if (LevelManager.instance != null)
        {
            LevelManager.instance.RestartLevel();
        }
            
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitTerminalPaused()
    {
        Time.timeScale = 1f;
        SetState(TerminalState.Normal);

        var hero = FindFirstObjectByType<Player>();
        if (hero != null && hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
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
        Debug.Log("TerminalSession: EnterTerminalPaused()");
    }

    public void UnfreezeAfterTerminalPlay()
    {
        SetState(TerminalState.Playback);
        Time.timeScale = 1f;

        var hero = FindFirstObjectByType<Player>();
        if (hero != null && hero.gatherInput != null)
            hero.gatherInput.EnablePlayerMap();
    }

    private void SetState(TerminalState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }
}