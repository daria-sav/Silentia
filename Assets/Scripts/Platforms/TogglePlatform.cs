using UnityEngine;

/// <summary>
/// Toggles platform visibility and collision state in response to a pressed signal.
///
/// This component is used as a button target and can be configured to become
/// visible when pressed or hidden when pressed.
/// </summary>
public class TogglePlatform : MonoBehaviour, IButtonTarget
{
    [Header("What to toggle")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D platformCollider;

    [Header("Behaviour")]
    [SerializeField] private bool visibleWhenPressed = true; 

    private void Awake()
    {
        if (spriteRenderer == null) 
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (platformCollider == null) 
            platformCollider = GetComponent<Collider2D>();
    }

    public void SetPressed(bool pressed)
    {
        bool enabledState = visibleWhenPressed ? pressed : !pressed;

        if (spriteRenderer != null) 
            spriteRenderer.enabled = enabledState;

        if (platformCollider != null) 
            platformCollider.enabled = enabledState;
    }
}