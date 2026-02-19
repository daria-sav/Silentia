using UnityEngine;

public class TogglePlatform : MonoBehaviour, IButtonTarget
{
    [Header("What to toggle")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D col;

    [Header("Behaviour")]
    [SerializeField] private bool visibleWhenPressed = true; 

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    public void SetPressed(bool pressed)
    {
        bool enabledState = visibleWhenPressed ? pressed : !pressed;

        if (spriteRenderer != null) spriteRenderer.enabled = enabledState;
        if (col != null) col.enabled = enabledState;
    }
}