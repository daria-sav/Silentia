using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Controls the introductory story sequence before gameplay starts.
///
/// The sequence displays text lines one by one with soft fade transitions,
/// allows the player to advance each line through input, and then fades into
/// the next scene.
/// </summary>
public class IntroSequence : MonoBehaviour
{
    [Header("Text")]
    [TextArea(2, 5)]
    [SerializeField] private string[] lines;
    [SerializeField] private TMP_Text label;

    [Header("Next Scene")]
    [SerializeField] private string nextScene;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1.2f;
    [SerializeField] private float holdDuration = 2.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float pauseBetween = 0.3f;

    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup screenFade;

    [Header("Input")]
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference clickAction;

    private bool _advance;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (submitAction != null)
        {
            submitAction.action.Enable();
            submitAction.action.performed += OnAdvance;
        }

        if (clickAction != null)
        {
            clickAction.action.Enable();
            clickAction.action.performed += OnAdvance;
        }
    }

    private void OnDisable()
    {
        if (submitAction != null)
            submitAction.action.performed -= OnAdvance;

        if (clickAction != null)
            clickAction.action.performed -= OnAdvance;
    }

    // ─────────────── SEQUENCE ────────────────

    private IEnumerator Start()
    {
        label.alpha = 0f;

        if (screenFade != null)
            yield return Fade(screenFade, 1f, 0f, 1.0f);

        foreach (var line in lines)
        {
            _advance = false;
            label.text = line;

            yield return FadeLabel(0f, 1f, fadeInDuration);

            float t = 0f;

            // keeps the current line visible until the timer ends or the player advances
            while (t < holdDuration && !_advance)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            _advance = false;

            yield return FadeLabel(1f, 0f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(pauseBetween);
        }

        label.alpha = 0f;

        if (screenFade != null)
            yield return Fade(screenFade, 0f, 1f, 1.2f);

        SceneManager.LoadScene(nextScene);
    }
    #endregion

    // ─────────────── INPUT ───────────────

    #region Input
    private void OnAdvance(InputAction.CallbackContext ctx)
    {
        _advance = true;
    }
    #endregion

    // ─────────────── HELPERS ───────────────

    #region Helpers
    private IEnumerator FadeLabel(float from, float to, float duration)
    {
        float t = 0f;
        label.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            label.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        label.alpha = to;
    }

    private IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
    #endregion
}