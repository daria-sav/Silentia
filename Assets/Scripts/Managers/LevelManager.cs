using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene transitions through a fullscreen fade overlay.
///
/// The manager fades the screen in when a scene starts and fades it out
/// before loading or restarting a scene. It uses unscaled time so transitions
/// remain stable even when gameplay is paused, such as during terminal flow.
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

    // ─────────────── PUBLIC API ───────────────

    #region Public API

    // restarts the currently active scene using a fade-out transition
    public void RestartLevel()
    {
        if (!CanStartTransition())
            return;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        StartTransition(FadeOutAndLoad(buildIndex));
    }

    // loads the given scene using a fade-out transition
    public void LoadLevel(string sceneName)
    {
        if (!CanStartTransition())
            return;

        StartTransition(FadeOutAndLoad(sceneName));
    }
    #endregion

    // ─────────────── COROUTINES ──────────────

    #region Fade Coroutines

    // fades the screen from black to transparent after a scene starts
    private IEnumerator FadeIn()
    {
        isTransitioning = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        yield return null;

        // snaps the camera to its target before revealing the scene
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

    // fades the screen to black and then loads a scene by name
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

    // fades the screen to black and then loads a scene by build index
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

    // smoothly changes the fade overlay alpha using unscaled time
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

    // returns true when a new scene transition can be started
    private bool CanStartTransition()
    {
        return !isTransitioning && canvasGroup != null;
    }

    // stops the previous transition coroutine before starting a new one
    private void StartTransition(IEnumerator routine)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(routine);
    }
    #endregion
}
