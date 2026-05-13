using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows pillar-related speech and interaction hints near the player.
/// </summary>
public class PillarTutorialHints : MonoBehaviour
{
    [Header("Speech Bubble")]
    [SerializeField] private RectTransform speechBubbleRoot;
    [SerializeField] private TMP_Text speechText;
    [SerializeField] private string speechMessage = "Seems like I can't handle this alone...";
    [SerializeField] private Vector2 speechOffset = new Vector2(0f, 120f);

    [Header("Interact Hint")]
    [SerializeField] private RectTransform interactHintRoot;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private string hintMessage = "[E]  Interact";

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup _speechGroup;
    private CanvasGroup _hintGroup;
    private Canvas _canvas;

    private bool _playerInZone;
    private bool _bubbleDone;

    private Coroutine _speechFade;
    private Coroutine _hintFade;

    // ───────────── LIFECYCLE ─────────────

    #region Lifecycle
    private void Awake()
    {
        _speechGroup = GetOrAddGroup(speechBubbleRoot);
        _hintGroup = GetOrAddGroup(interactHintRoot);

        _canvas = speechBubbleRoot != null
            ? speechBubbleRoot.GetComponentInParent<Canvas>()
            : null;

        if (speechText != null) speechText.text = speechMessage;
        if (hintText != null) hintText.text = hintMessage;

        SetInstant(_speechGroup, speechBubbleRoot, false);
        SetInstant(_hintGroup, interactHintRoot, false);
    }

    private void Update()
    {
        bool terminalIdle = TerminalSession.Instance == null
            || TerminalSession.Instance.State == TerminalSession.TerminalState.Normal;

        bool showHint = _playerInZone && terminalIdle;
        SetVisible(_hintGroup, interactHintRoot, showHint, ref _hintFade);

        if (!_bubbleDone && !terminalIdle)
            _bubbleDone = true;

        bool showBubble = _playerInZone && terminalIdle && !_bubbleDone;
        SetVisible(_speechGroup, speechBubbleRoot, showBubble, ref _speechFade);
    }

    private void LateUpdate()
    {
        if (_bubbleDone || speechBubbleRoot == null || !speechBubbleRoot.gameObject.activeSelf)
            return;

        UpdateBubblePosition();
    }
    #endregion

    // ───────────── PUBLIC API ─────────────

    #region Public API
    public void OnPlayerEntered()
    {
        _playerInZone = true;
    }

    public void OnPlayerExited()
    {
        _playerInZone = false;
    }
    #endregion

    // ───────────── POSITION ─────────────

    #region Position
    private void UpdateBubblePosition()
    {
        var cam = Camera.main;
        if (cam == null || _canvas == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            screenPos,
            null,
            out Vector2 localPoint
        );

        speechBubbleRoot.anchoredPosition = localPoint + speechOffset;
    }
    #endregion

    // ───────────── VISIBILITY ─────────────

    #region Visibility

    private void SetVisible(CanvasGroup group, RectTransform root, bool visible, ref Coroutine handle)
    {
        if (group == null || root == null) return;

        float target = visible ? 1f : 0f;
        if (Mathf.Approximately(group.alpha, target) && root.gameObject.activeSelf == visible)
            return;

        if (visible) root.gameObject.SetActive(true);

        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(FadeTo(group, target, () =>
        {
            if (!visible) root.gameObject.SetActive(false);
        }));
    }

    private IEnumerator FadeTo(CanvasGroup group, float target, System.Action onComplete)
    {
        float start = group.alpha, elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = target;
        onComplete?.Invoke();
    }

    private static void SetInstant(CanvasGroup g, RectTransform root, bool visible)
    {
        if (g != null) g.alpha = visible ? 1f : 0f;
        if (root != null) root.gameObject.SetActive(visible);
    }
    #endregion

    // ───────────── HELPERS ─────────────

    #region Helpers
    private static CanvasGroup GetOrAddGroup(RectTransform rt)
    {
        if (rt == null) return null;
        return rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
    }
    #endregion
}