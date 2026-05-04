using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Scrolls the credits container upward, then shows a "Thank You" screen.
/// Press Skip (any key / mouse click) to jump straight to Thank You.
/// After the thank-you delay the game returns to the Main Menu.
/// </summary>
public class CreditsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform creditsContainer;
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private CanvasGroup thankYouCanvasGroup;

    [Header("Scrolling")]
    [SerializeField] private float scrollSpeed = 60f;   // units per second
    [SerializeField] private float endPauseSeconds = 2f;    // pause after scroll ends

    [Header("Thank You screen")]
    [SerializeField] private float thankYouDuration = 5f;    // seconds before going to menu
    [SerializeField] private float fadeDuration = 1f;

    [Header("Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    // ── state ──────────────────────────────────────────────────────────────
    private enum Phase { Scrolling, EndPause, FadingToThankYou, ThankYou, FadingOut }
    private Phase phase = Phase.Scrolling;

    private float phaseTimer;
    private float scrollFinishY;   // Y position when container is fully scrolled
    private bool skipped;

    // ───────────────────────────── LIFECYCLE ───────────────────────────────

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (thankYouCanvasGroup != null)
        {
            thankYouCanvasGroup.alpha = 0f;
            thankYouCanvasGroup.interactable = false;
            thankYouCanvasGroup.blocksRaycasts = false;
        }

        if (creditsCanvasGroup != null)
            creditsCanvasGroup.alpha = 1f;

        // The container should scroll until its bottom edge clears the screen top.
        // scrollFinishY = container height  (anchored at top-center, pivot bottom)
        if (creditsContainer != null)
            scrollFinishY = 500f;
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.Scrolling: UpdateScrolling(); break;
            case Phase.EndPause: UpdateEndPause(); break;
            case Phase.FadingToThankYou: UpdateFadeToThankYou(); break;
            case Phase.ThankYou: UpdateThankYou(); break;
            case Phase.FadingOut: UpdateFadeOut(); break;
        }

        // Any key / click = skip straight to thank-you
        if (!skipped && phase == Phase.Scrolling)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                Skip();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                Skip();
        }
    }

    // ───────────────────────────── PHASES ──────────────────────────────────

    private void UpdateScrolling()
    {
        if (creditsContainer == null) return;

        creditsContainer.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (creditsContainer.anchoredPosition.y >= scrollFinishY)
        {
            creditsContainer.anchoredPosition =
                new Vector2(creditsContainer.anchoredPosition.x, scrollFinishY);

            EnterPhase(Phase.EndPause);
        }
    }

    private void UpdateEndPause()
    {
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= endPauseSeconds)
            EnterPhase(Phase.FadingToThankYou);
    }

    private void UpdateFadeToThankYou()
    {
        phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(phaseTimer / fadeDuration);

        if (creditsCanvasGroup != null) creditsCanvasGroup.alpha = 1f - t;
        if (thankYouCanvasGroup != null) thankYouCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            if (thankYouCanvasGroup != null)
            {
                thankYouCanvasGroup.interactable = true;
                thankYouCanvasGroup.blocksRaycasts = true;
            }
            EnterPhase(Phase.ThankYou);
        }
    }

    private void UpdateThankYou()
    {
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= thankYouDuration)
            EnterPhase(Phase.FadingOut);
    }

    private void UpdateFadeOut()
    {
        phaseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(phaseTimer / fadeDuration);

        if (thankYouCanvasGroup != null)
            thankYouCanvasGroup.alpha = 1f - t;

        if (t >= 1f)
            GoToMainMenu();
    }

    // ───────────────────────────── HELPERS ─────────────────────────────────

    private void EnterPhase(Phase next)
    {
        phase = next;
        phaseTimer = 0f;
    }

    private void Skip()
    {
        skipped = true;

        if (creditsContainer != null)
            creditsContainer.anchoredPosition =
                new Vector2(creditsContainer.anchoredPosition.x, scrollFinishY);

        EnterPhase(Phase.FadingToThankYou);
    }

    // ───────────────────────────── PUBLIC ──────────────────────────────────

    public void OnSkipButton()
    {
        if (phase == Phase.Scrolling || phase == Phase.EndPause)
            Skip();
    }

    private void GoToMainMenu()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(mainMenuScene);
        else
            SceneManager.LoadScene(mainMenuScene);
    }
}