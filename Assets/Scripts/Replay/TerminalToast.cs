using System.Collections;
using TMPro;
using UnityEngine;

public class TerminalToast : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float showSeconds = 1.8f;

    private Coroutine routine;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        gameObject.SetActive(false);
    }

    public void Show(string msg)
    {
        if (text == null) return;

        text.text = msg;
        gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(HideLater());
    }

    private IEnumerator HideLater()
    {
        yield return new WaitForSecondsRealtime(showSeconds);
        gameObject.SetActive(false);
        routine = null;
    }
}