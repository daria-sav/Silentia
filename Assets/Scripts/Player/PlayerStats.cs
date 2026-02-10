using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private Player player;
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

    void Start()
    {
        currentHealth = maxHealth;
        EnsureRenderer();
    }

    private bool EnsureRenderer()
    {
        if (player == null)
            player = GetComponentInParent<Player>();

        if (player == null)
            return false;

        // If SpriteRenderer was destroyed because body was switched, re-find it
        if (spriter == null)
        {
            spriter = player.GetComponentInChildren<SpriteRenderer>(true);
            if (spriter == null)
                return false;

            originalMaterial = spriter.material;
        }

        return true;
    }

    public void DamagePlayer(float damage)
    {
        if (canTakeDamage == false)
            return;

        currentHealth -= damage;
        EnsureRenderer();

        StartCoroutine(Flash());

        if (currentHealth <= 0)
        {
            if (player == null) EnsureRenderer();

            if (player.stateMachine.currentState != PlayerStates.State.KnockBack)
                player.stateMachine.ChangeState(PlayerStates.State.Death);
        }
    }

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

    public float GetCurrentHealth()
    {
        return currentHealth;
    } 

    public bool GetCanTakeDamage()
    {
        return canTakeDamage;
    }
}
