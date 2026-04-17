using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD countdown timer during recording.
/// Circular ring fills down as time runs out.
/// </summary>
public class RecordingTimerUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image ringFill;

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color warningColor = new Color(0.75f, 0.21f, 0.53f); // Hex: #4B1535
    [SerializeField] private float warningThreshold = 3f;

    private ReplayRecorder recorder;
    private bool isTracking;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();

        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnStateChanged += OnStateChanged;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnStateChanged -= OnStateChanged;
    }
    #endregion

    // ─────────────── UPDATE ──────────────────

    #region Update
    private void Update()
    {
        if (!isTracking || recorder == null || !recorder.IsRecording)
            return;

        float elapsed = recorder.CurrentClip != null
            ? recorder.CurrentClip.FrameCount * Time.fixedDeltaTime
            : 0f;

        float remaining = Mathf.Max(0f, recorder.maxSeconds - elapsed);
        float fraction = remaining / recorder.maxSeconds;

        // text
        label.text = Mathf.CeilToInt(remaining).ToString();

        // color
        Color c = remaining <= warningThreshold ? warningColor : normalColor;
        label.color = c;

        if (ringFill != null)
        {
            ringFill.fillAmount = fraction;
            ringFill.color = c;
        }
    }
    #endregion

    // ─────────────── HANDLERS ────────────────

    #region Handlers
    private void OnStateChanged(TerminalSession.TerminalState state)
    {
        bool recording = state == TerminalSession.TerminalState.Recording;

        if (recording)
        {
            recorder = FindFirstObjectByType<ReplayRecorder>();
            isTracking = recorder != null;
        }
        else
        {
            isTracking = false;
            recorder = null;
        }

        gameObject.SetActive(recording);
    }
    #endregion
}