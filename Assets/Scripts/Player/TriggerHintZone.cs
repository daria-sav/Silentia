using System.Collections;
using UnityEngine;

public class TriggerHintZone : MonoBehaviour
{
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup _group;
    private Coroutine _fade;

    private void Awake()
    {
        _group = GetOrAddGroup(hintRoot);
        SetInstant(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SetVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_group == null || hintRoot == null) return;

        if (visible) hintRoot.SetActive(true);

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(visible ? 1f : 0f, () =>
        {
            if (!visible) hintRoot.SetActive(false);
        }));
    }

    private void SetInstant(bool visible)
    {
        if (_group != null) _group.alpha = visible ? 1f : 0f;
        if (hintRoot != null) hintRoot.SetActive(visible);
    }

    private IEnumerator FadeTo(float target, System.Action onComplete)
    {
        float start = _group.alpha, elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        _group.alpha = target;
        onComplete?.Invoke();
    }

    private static CanvasGroup GetOrAddGroup(GameObject go)
    {
        if (go == null) return null;
        return go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
    }
}