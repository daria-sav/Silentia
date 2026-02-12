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

    // pending command (applied after scene reload)
    private bool pendingStartRecording;
    private int pendingProfileIndex = -1;

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

        pendingProfileIndex = profileIndex;
        pendingStartRecording = true;

        SaveSpawnForNextLoad();

        if (LevelManager.instance != null)
            LevelManager.instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveClipToSelectedSlotAndRestart(ReplayClip clip)
    {
        if (clip == null) return;
        restartInProgress = true;

        if (clip != null)
        {
            Clips[SelectedSlot] = clip;
            ClipProfileIds[SelectedSlot] = clip.profileId;
            OnSlotsChanged?.Invoke();
        }

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

        if (!pendingStartRecording)
        {
            switcher.SwitchTo(0);
            return;
        }

        var applier = hero.GetComponent<ProfileApplier>();
        if (applier != null) applier.DisableAutoApplyOnStart();

        if (pendingProfileIndex >= 0)
            switcher.SwitchTo(pendingProfileIndex);

        recorder.StartRecording();

        pendingStartRecording = false;
        pendingProfileIndex = -1;

        Debug.Log("TerminalSession: Applied pending switch + started recording after reload.");
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
}