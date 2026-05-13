using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a short terminal message for a limited amount of real time.
///
/// This is used for lightweight feedback in the terminal UI, such as
/// validation errors or state messages. The toast uses real-time waiting
/// so it still hides correctly while the game is paused with
/// <see cref="Time.timeScale"/> set to zero.
/// </summary>
public class TerminalToast : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float showSeconds = 1.8f;

    private Coroutine hideRoutine;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        if (messageText == null) 
            messageText = GetComponent<TMP_Text>();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        hideRoutine = null;
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void Show(string message)
    {
        if (messageText == null) 
            return;

        messageText.text = message;
        gameObject.SetActive(true);

        if (hideRoutine != null) 
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideLater());
    }
    #endregion

    // ─────────────── COROUTINES ──────────────

    #region Coroutines
    private IEnumerator HideLater()
    {
        yield return new WaitForSecondsRealtime(showSeconds);

        gameObject.SetActive(false);
        hideRoutine = null;
    }
    #endregion
}