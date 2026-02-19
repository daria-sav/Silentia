using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressureButton : MonoBehaviour
{
    [Header("Who can press")]
    [SerializeField] private LayerMask pressLayers = ~0;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Targets")]
    [Tooltip("Anything with a component that implements IButtonTarget")]
    [SerializeField] private List<MonoBehaviour> targetBehaviours = new();

    private readonly List<IButtonTarget> targets = new();
    private int pressCount;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        targets.Clear();
        foreach (var mb in targetBehaviours)
        {
            if (mb is IButtonTarget t)
                targets.Add(t);
        }

        SetVisual(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsInPressLayers(collision.collider.gameObject))
            return;

        if (!HasTopContact(collision))
            return;

        pressCount++;
        if (pressCount == 1)
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
        int bit = 1 << obj.layer;
        return (pressLayers.value & bit) != 0;
    }

    private bool HasTopContact(Collision2D collision)
    {
        foreach (var c in collision.contacts)
            if (c.normal.y < -0.5f) return true;
        return false;
    }

    private void SetPressed(bool pressed)
    {
        SetVisual(pressed);

        for (int i = 0; i < targets.Count; i++)
            targets[i].SetPressed(pressed);
    }

    private void SetVisual(bool pressed)
    {
        if (spriteRenderer == null) return;
        if (pressed && downSprite != null) spriteRenderer.sprite = downSprite;
        else if (!pressed && upSprite != null) spriteRenderer.sprite = upSprite;
    }
}