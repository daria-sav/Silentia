using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays character introduction dialogs during the tutorial.
/// </summary>
public class CharacterIntroUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Image heroIcon;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private TMP_Text continueHint;
    [SerializeField] private CharacterListUI characterListUI;

    [Header("Hero")]
    [SerializeField] private Sprite heroSprite;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float textTypeSpeed = 0.03f;

    [Header("Input")]
    [SerializeField] private TerminalInput terminalInput;

    private CharacterIntroSequence sequence;
    private int currentEntry;
    private bool waitingForInput;
    private bool typingDone;
    private Coroutine typeRoutine;

    public bool IsPlaying { get; private set; }

    // ───────────── PUBLIC API ─────────────

    #region Public API
    public void Play(CharacterIntroSequence sequence, System.Action onComplete)
    {
        if (sequence == null || sequence.entries == null || sequence.entries.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        this.sequence = sequence;
        currentEntry = 0;
        _onComplete = onComplete;
        IsPlaying = true;

        if (heroIcon != null)
        {
            heroIcon.sprite = heroSprite;
            heroIcon.enabled = heroSprite != null;
        }

        gameObject.SetActive(true);
        StartCoroutine(FadePanel(0f, 1f, () => ShowEntry(currentEntry)));
    }
    #endregion

    // ───────────── LIFECYCLE ─────────────

    #region Lifecycle
    private void Update()
    {
        if (!waitingForInput) return;
        if (terminalInput == null || !terminalInput.ConfirmDown()) return;

        if (!typingDone)
        {
            SkipTyping();
            return;
        }

        currentEntry++;
        if (currentEntry < sequence.entries.Length)
        {
            ShowEntry(currentEntry);
        }
        else
        {
            StartCoroutine(FadePanel(1f, 0f, () =>
            {
                gameObject.SetActive(false);
                IsPlaying = false;
                characterListUI?.ClearHighlight();
                _onComplete?.Invoke();
            }));
        }
    }

    #endregion

    // ───────────── DIALOG FLOW ─────────────

    #region Dialog Flow

    private System.Action _onComplete;

    private void ShowEntry(int index)
    {
        var entry = sequence.entries[index];

        characterListUI?.HighlightProfile(entry.profile);

        // continueHint
        if (continueHint != null)
            continueHint.text = "[ Space / Enter / Click ]";

        if (typeRoutine != null) StopCoroutine(typeRoutine);

        typingDone = false;
        waitingForInput = true;

        typeRoutine = StartCoroutine(TypeText(entry.introText));
    }

    private IEnumerator TypeText(string text)
    {
        if (dialogText == null) yield break;
        dialogText.text = "";

        if (textTypeSpeed <= 0f)
        {
            dialogText.text = text;
            typingDone = true;
            waitingForInput = true;
            yield break;
        }

        foreach (char c in text)
        {
            dialogText.text += c;
            yield return new WaitForSecondsRealtime(textTypeSpeed);
        }

        typingDone = true;
        waitingForInput = true;
    }

    private void SkipTyping()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        var entry = sequence.entries[currentEntry];
        if (dialogText != null) dialogText.text = entry.introText;
        typingDone = true;
    }
    #endregion

    // ───────────── FADE ─────────────

    #region Fade
    private IEnumerator FadePanel(float from, float to, System.Action onDone)
    {
        waitingForInput = false;
        if (panelGroup == null) { onDone?.Invoke(); yield break; }

        float elapsed = 0f;
        panelGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        panelGroup.alpha = to;
        onDone?.Invoke();
    }
    #endregion
}