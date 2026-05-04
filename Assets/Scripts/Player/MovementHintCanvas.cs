using System.Collections;
using UnityEngine;

/// <summary>
/// Shows a movement hint above the player once per lifetime.
/// Hides permanently the first time the player moves horizontally.
/// Uses PlayerPrefs so it never shows again after the first session.
/// </summary>
public class MovementHintUI : MonoBehaviour
{
    private const string PrefKey = "MovementHintShown";

    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private GatherInput gatherInput; 

    private CanvasGroup group;
    private bool isDone;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (gatherInput == null)
            gatherInput = GetComponentInParent<GatherInput>(true);

        if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
        {
            isDone = true;
            group.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }

        group.alpha = 1f;
    }

    private void Update()
    {
        if (isDone) return;

        if (gatherInput == null) return;

        if (Mathf.Abs(gatherInput.horizontalInput) < 0.1f) return;

        isDone = true;
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float start = group.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, 0f, elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static void ResetHint()
    {
        PlayerPrefs.DeleteKey("MovementHintShown");
    }
}