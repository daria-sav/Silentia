using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays short gate-related hint messages on the UI.
///
/// The hint appears for a limited time and then fades out using unscaled time,
/// so it can still work correctly even when the game is paused or slowed down.
/// </summary>
public class GateHintUI : MonoBehaviour
{
    public static GateHintUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Timing")]
    [SerializeField] private float visibleTime = 2f;
    [SerializeField] private float fadeTime = 0.25f;

    private Coroutine currentRoutine;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        HideImmediately();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    // ─────────────── PUBLIC API ───────────────

    #region Public API

    public void Show(string message)
    {
        if (canvasGroup == null || messageText == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message));
    }
    #endregion

    // ─────────────── UI ROUTINES ───────────────

    #region UI Routines
    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return new WaitForSecondsRealtime(visibleTime);

        float elapsed = 0f;

        // fades the hint out smoothly without depending on Time.timeScale
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        HideImmediately();
    }
    #endregion

    // ─────────────── HELPERS ───────────────

    #region Helpers

    private void HideImmediately()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    #endregion
}