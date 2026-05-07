using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene transitions with a fullscreen fade overlay.
///
/// This manager fades the screen in when a scene starts and fades it out
/// before loading or restarting a scene. It uses unscaled time so transitions
/// still work while gameplay is paused, for example during terminal flow.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration;

    private CanvasGroup canvasGroup;
    private Coroutine transitionRoutine;
    private bool isTransitioning;
    public bool IsTransitioning => isTransitioning;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (canvasGroup == null)
            return;

        StartTransition(FadeIn());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    /// <summary>
    /// Restarts the currently active scene using a fade-out transition
    /// </summary>
    public void RestartLevel()
    {
        if (!CanStartTransition())
            return;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        StartTransition(FadeOutAndLoad(buildIndex));
    }

    /// <summary>
    /// Loads a scene by name using a fade-out transition
    /// </summary>
    /// <param name="sceneName"> scene name to load </param>
    public void LoadLevel(string sceneName)
    {
        if (!CanStartTransition())
            return;

        StartTransition(FadeOutAndLoad(sceneName));
    }
    #endregion

    // ─────────────── COROUTINES ──────────────

    #region Fade Coroutines
    /// <summary>
    /// Fades the screen from the current alpha to fully transparent
    /// </summary>
    private IEnumerator FadeIn()
    {
        isTransitioning = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        yield return null;
        CameraManager.instance?.SnapToTarget();

        yield return FadeCanvasTo(0f);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        isTransitioning = false;
        transitionRoutine = null;
    }

    /// <summary>
    /// Fades the screen to black and then loads a scene by name
    /// </summary>
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        isTransitioning = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        yield return FadeCanvasTo(1f);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Fades the screen to black and then loads a scene by build index.
    /// </summary>
    private IEnumerator FadeOutAndLoad(int buildIndex)
    {
        isTransitioning = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        yield return FadeCanvasTo(1f);
        SceneManager.LoadScene(buildIndex);
    }

    /// <summary>
    /// Smoothly changes the overlay alpha using unscaled time so pause state
    /// does not affect scene transitions
    /// </summary>
    /// <param name="targetAlpha"> final alpha value </param>
    private IEnumerator FadeCanvasTo(float targetAlpha)
    {
        if (canvasGroup == null)
            yield break;

        float time = 0f;
        float startAlpha = canvasGroup.alpha;

        if (Mathf.Approximately(fadeDuration, 0f))
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    /// <summary>
    /// Returns true when a new scene transition can be started
    /// </summary>
    private bool CanStartTransition()
    {
        return !isTransitioning && canvasGroup != null;
    }

    /// <summary>
    /// Stops the previous transition coroutine before starting a new one
    /// </summary>
    /// <param name="routine"> transition routine to run</param>
    private void StartTransition(IEnumerator routine)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(routine);
    }
    #endregion
}
