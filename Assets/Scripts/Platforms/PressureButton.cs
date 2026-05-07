using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pressure-triggered button that sends a pressed / released signal to all assigned targets.
///
/// The button tracks valid pressers by layer, changes its visual state, and notifies
/// all components implementing <see cref="IButtonTarget"/> when its pressed state changes.
/// Multiple valid objects can stand on the button at the same time.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressureButton : MonoBehaviour
{
    [Header("Who can press")]
    [SerializeField] private LayerMask pressLayers = ~0;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Tutorial Hint")]
    [SerializeField] private GameObject tutorialHint;
    [SerializeField] private bool hideHintAfterFirstPress = true;

    private bool tutorialHintHidden;

    [Header("Targets")]
    [Tooltip("Anything with a component that implements IButtonTarget")]
    [SerializeField] private List<MonoBehaviour> targetBehaviours = new();

    private readonly List<IButtonTarget> targets = new();
    private readonly HashSet<Transform> pressers = new();

    private Collider2D myCollider;

    // ─────────────── LIFECYCLE ───────────────

    #region Unity Lifecycle
    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();

        if (myCollider != null && !myCollider.isTrigger)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[PressureButton] Collider is not Trigger. Turn on 'Is Trigger' on this object's Collider2D.", this);
#endif
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        targets.Clear();

        foreach (MonoBehaviour mb in targetBehaviours)
        {
            if (mb is IButtonTarget target)
                targets.Add(target);
        }

        SetVisual(false);
    }
    #endregion

    // ─────────────── TRIGGER LOGIC ───────────────

    #region Trigger Handling
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInPressLayers(other.gameObject))
            return;

        Transform root = other.transform.root;

        bool wasPressed = pressers.Count > 0;
        pressers.Add(root);
        bool isPressed = pressers.Count > 0;

        if (wasPressed != isPressed)
            SetPressed(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInPressLayers(other.gameObject))
            return;

        Transform root = other.transform.root;

        bool wasPressed = pressers.Count > 0;
        pressers.Remove(root);
        bool isPressed = pressers.Count > 0;

        if (wasPressed != isPressed)
            SetPressed(false);
    }
    #endregion

    // ─────────────── HELPERS ────────────────

    #region Internal Helpers
    private bool IsInPressLayers(GameObject obj)
    {
        int bit = 1 << obj.layer;
        return (pressLayers.value & bit) != 0;
    }

    private void SetPressed(bool pressed)
    {
        SetVisual(pressed);

        if (pressed)
            HideTutorialHint();

        for (int i = 0; i < targets.Count; i++)
            targets[i].SetPressed(pressed);
    }

    private void SetVisual(bool pressed)
    {
        if (spriteRenderer == null) 
            return;

        if (pressed && downSprite != null) 
            spriteRenderer.sprite = downSprite;

        else if (!pressed && upSprite != null) 
            spriteRenderer.sprite = upSprite;
    }

    private void HideTutorialHint()
    {
        if (!hideHintAfterFirstPress)
            return;

        if (tutorialHintHidden)
            return;

        tutorialHintHidden = true;

        if (tutorialHint != null)
            tutorialHint.SetActive(false);
    }
    #endregion
}