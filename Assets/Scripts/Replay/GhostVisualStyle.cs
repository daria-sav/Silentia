using UnityEngine;

/// <summary>
/// Applies ghost darkening and transparency while preserving
/// the original sprite colors and details.
/// </summary>
public class GhostVisualStyle : MonoBehaviour
{
    [Header("Ghost Style")]
    [SerializeField, Range(0.2f, 1f)] private float brightnessMultiplier = 0.65f;
    [SerializeField, Range(0.2f, 1f)] private float alphaMultiplier = 0.65f;

    [Header("Options")]
    [SerializeField] private bool includeInactiveChildren = true;

    private SpriteRenderer[] cachedRenderers;
    private Color[] originalColors;

    public void ApplyStyle()
    {
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        originalColors = new Color[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            originalColors[i] = cachedRenderers[i].color;
            ApplyToRenderer(cachedRenderers[i], originalColors[i]);
        }
    }

    public void RefreshStyle()
    {
        if (cachedRenderers == null || originalColors == null || cachedRenderers.Length != originalColors.Length)
        {
            ApplyStyle();
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            ApplyToRenderer(cachedRenderers[i], originalColors[i]);
        }
    }

    private void ApplyToRenderer(SpriteRenderer sr, Color baseColor)
    {
        sr.color = new Color(
            baseColor.r * brightnessMultiplier,
            baseColor.g * brightnessMultiplier,
            baseColor.b * brightnessMultiplier,
            baseColor.a * alphaMultiplier
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        RefreshStyle();
    }
#endif
}