using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player health, damage, flash feedback,
/// and death state transition.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Health")]
    [SerializeField] private float maxHealth;
    private float currentHealth;

    [Header("Flash")]
    [SerializeField] private float flashDuration;
    [SerializeField, Range(0, 1)] private float flashStrength;
    [SerializeField] private Color flashColor;
    [SerializeField] private Material flashMaterial;

    private Material originalMaterial;
    private SpriteRenderer spriter;
    private bool canTakeDamage = true;

    public bool DeferDeath { get; private set; }

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    void Start()
    {
        currentHealth = maxHealth;
        EnsureRenderer();
    }
    #endregion

    // ────────────────── API ──────────────────

    #region Public API
    public void SetDeferDeath(bool value) => DeferDeath = value;

    public void DamagePlayer(float damage)
    {
        if (!canTakeDamage)
            return;

        currentHealth -= damage;
        EnsureRenderer();
        
        StartCoroutine(Flash());

        if (currentHealth <= 0)
        {
            if (player == null) 
                EnsureRenderer();

            if (DeferDeath)
                return;

            if (player.stateMachine.currentState != PlayerStates.State.KnockBack)
                player.stateMachine.ChangeState(PlayerStates.State.Death);
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool GetCanTakeDamage()
    {
        return canTakeDamage;
    }
    #endregion

    // ──────────────── HELPERS ────────────────

    #region Helpers
    private bool EnsureRenderer()
    {
        if (player == null)
            player = GetComponentInParent<Player>();

        if (player == null)
            return false;

        // if the old body was replaced, find the new renderer
        if (spriter == null)
        {
            spriter = player.GetComponentInChildren<SpriteRenderer>(true);
            if (spriter == null)
                return false;

            originalMaterial = spriter.material;
        }

        return true;
    }
    #endregion

    // ───────────── FLASH EFFECT ──────────────

    #region Flash Effect
    private IEnumerator Flash()
    {
        canTakeDamage = false;
        spriter.material = flashMaterial;
        flashMaterial.SetColor("_FlashColor", flashColor);
        flashMaterial.SetFloat("_FlashAmount", flashStrength);

        yield return new WaitForSeconds(flashDuration);

        spriter.material = originalMaterial;

        if (currentHealth > 0)
            canTakeDamage = true;
    }
    #endregion
}
