using UnityEngine;

/// <summary>
/// Adds a subtle floating and pulsing animation to a tutorial hint.
/// </summary>
public class TutorialHintFloat : MonoBehaviour
{
    [SerializeField] private float floatAmplitude = 0.12f;
    [SerializeField] private float floatSpeed = 2f;

    [SerializeField] private float pulseAmplitude = 0.08f;
    [SerializeField] private float pulseSpeed = 2f;

    private Vector3 startLocalPos;
    private Vector3 startScale;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        startLocalPos = transform.localPosition;
        startScale = transform.localScale;
    }

    private void Update()
    {
        float t = Time.time;

        float yOffset = Mathf.Sin(t * floatSpeed) * floatAmplitude;
        transform.localPosition = startLocalPos + new Vector3(0f, yOffset, 0f);

        float scaleOffset = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmplitude;
        transform.localScale = startScale * scaleOffset;
    }
    #endregion
}