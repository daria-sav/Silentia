using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ButtonSwitch : MonoBehaviour
{
    [Header("Who can press")]
    [SerializeField] private LayerMask pressLayers = ~0;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Target platform")]
    [SerializeField] private ControlledMovingPlatform targetPlatform;

    private int pressCount;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        SetVisual(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsInPressLayers(collision.collider.gameObject))
            return;

        if (!HasTopContact(collision))
            return;

        pressCount++;
        SetPressed(true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsInPressLayers(collision.collider.gameObject))
            return;

        pressCount = Mathf.Max(0, pressCount - 1);

        if (pressCount == 0)
            SetPressed(false);
    }

    private bool IsInPressLayers(GameObject obj)
    {
        int layerBit = 1 << obj.layer;
        return (pressLayers.value & layerBit) != 0;
    }

    private bool HasTopContact(Collision2D collision)
    {
        foreach (var c in collision.contacts)
        {
            if (c.normal.y < -0.5f)
                return true;
        }
        return false;
    }

    private void SetPressed(bool pressed)
    {
        SetVisual(pressed);

        if (targetPlatform != null)
            targetPlatform.SetMoving(pressed);
    }

    private void SetVisual(bool pressed)
    {
        if (spriteRenderer == null) return;

        if (pressed && downSprite != null) spriteRenderer.sprite = downSprite;
        else if (!pressed && upSprite != null) spriteRenderer.sprite = upSprite;
    }
}