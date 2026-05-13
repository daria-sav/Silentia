using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the stop-recording hint during terminal recording.
/// </summary>
public class RecordingHint : MonoBehaviour
{
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private string message = "[Q]  Stop Recording";
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup _group;
    private Coroutine _fade;
    private bool _visible;

    // ───────────── LIFECYCLE ─────────────

    #region Lifecycle
    private void Awake()
    {
        _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (hintText != null) hintText.text = message;
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }

    private void Update()
    {
        bool shouldShow = TerminalSession.Instance != null
            && TerminalSession.Instance.State == TerminalSession.TerminalState.Recording;

        if (shouldShow == _visible) return;
        _visible = shouldShow;

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(_visible ? 1f : 0f));
    }
    #endregion

    // ───────────── FADE ─────────────

    #region Fade
    private IEnumerator FadeTo(float target)
    {
        float start = _group.alpha, elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        _group.alpha = target;
        _group.blocksRaycasts = target > 0f;
    }
    #endregion
}