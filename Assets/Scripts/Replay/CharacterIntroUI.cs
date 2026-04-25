using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a dialog sequence introducing characters.
/// Hero icon + speech text. The introduced character
/// is highlighted in CharacterListUI, not shown here.
/// Blocked until player presses Space / Enter / Click.
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

    // ── runtime ───────────────────────────────────────────────────
    private CharacterIntroSequence _sequence;
    private int _currentEntry;
    private bool _waitingForInput;
    private bool _typingDone;
    private Coroutine _typeRoutine;

    public bool IsPlaying { get; private set; }

    // ── PUBLIC API ────────────────────────────────────────────────

    public void Play(CharacterIntroSequence sequence, System.Action onComplete)
    {
        if (sequence == null || sequence.entries == null || sequence.entries.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _sequence = sequence;
        _currentEntry = 0;
        _onComplete = onComplete;
        IsPlaying = true;

        if (heroIcon != null)
        {
            heroIcon.sprite = heroSprite;
            heroIcon.enabled = heroSprite != null;
        }

        gameObject.SetActive(true);
        StartCoroutine(FadePanel(0f, 1f, () => ShowEntry(_currentEntry)));
    }

    // ── UPDATE ────────────────────────────────────────

    private void Update()
    {
        if (!_waitingForInput) return;
        if (terminalInput == null || !terminalInput.ConfirmDown()) return;

        if (!_typingDone)
        {
            SkipTyping();
            return;
        }

        _currentEntry++;
        if (_currentEntry < _sequence.entries.Length)
        {
            ShowEntry(_currentEntry);
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

    // ── HELPERS ───────────────────────────────────────────────────

    private System.Action _onComplete;

    private void ShowEntry(int index)
    {
        var entry = _sequence.entries[index];

        characterListUI?.HighlightProfile(entry.profile);

        // continueHint
        if (continueHint != null)
            continueHint.text = "[ Space / Enter / Click ]";

        if (_typeRoutine != null) StopCoroutine(_typeRoutine);

        _typingDone = false;
        _waitingForInput = false;

        _typeRoutine = StartCoroutine(TypeText(entry.introText));
    }

    private IEnumerator TypeText(string text)
    {
        if (dialogText == null) yield break;
        dialogText.text = "";

        if (textTypeSpeed <= 0f)
        {
            dialogText.text = text;
            _typingDone = true;
            _waitingForInput = true;
            yield break;
        }

        foreach (char c in text)
        {
            dialogText.text += c;
            yield return new WaitForSecondsRealtime(textTypeSpeed);
        }

        _typingDone = true;
        _waitingForInput = true;
    }

    private void SkipTyping()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        var entry = _sequence.entries[_currentEntry];
        if (dialogText != null) dialogText.text = entry.introText;
        _typingDone = true;
    }

    private IEnumerator FadePanel(float from, float to, System.Action onDone)
    {
        _waitingForInput = false;
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

        if (to > 0f) _waitingForInput = true;
    }
}