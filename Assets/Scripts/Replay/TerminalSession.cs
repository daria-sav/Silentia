using UnityEngine;
using UnityEngine.SceneManagement;

public class TerminalSession : MonoBehaviour
{
    public static TerminalSession Instance { get; private set; }

    [Header("Runtime stored data")]
    public ReplayClip LastClip { get; private set; }

    // pending command (applied after scene reload)
    private bool pendingStartRecording;
    private int pendingProfileIndex = -1;
    public int LastRecordedProfileIndex { get; private set; } = -1;

    private ReplayRecorder boundRecorder;

    private string terminalSpawnKey;
    private bool terminalFacingRight = true;
    private bool restartInProgress;

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

    public void RequestRestartAndStartRecording(int profileIndex)
    {
        if (restartInProgress) return;
        restartInProgress = true;

        LastRecordedProfileIndex = profileIndex;
        pendingProfileIndex = profileIndex;
        pendingStartRecording = true;

        SaveSpawnForNextLoad();

        if (LevelManager.instance != null)
            LevelManager.instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveClipAndRestart(ReplayClip clip)
    {
        if (restartInProgress) return;
        restartInProgress = true;

        LastClip = clip;

        SaveSpawnForNextLoad();

        if (LevelManager.instance != null)
            LevelManager.instance.RestartLevel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        restartInProgress = false;
        // Find the hero in the freshly loaded scene
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

        // Always bind recorder after any scene load so stop events are handled.
        BindRecorder(recorder);

        // We do not want global hotkeys anywhere (terminal will handle input)
        switcher.SetHotkeysEnabled(false);

        // Apply pending: restart -> switch profile -> start recording
        if (!pendingStartRecording) return;

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
        SaveClipAndRestart(clip);
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
}