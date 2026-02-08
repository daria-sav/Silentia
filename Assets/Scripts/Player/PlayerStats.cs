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
        spriter = player.GetComponentInChildren<SpriteRenderer>(true);
        if (spriter == null)
        {
            Debug.LogError("PlayerStats: SpriteRenderer");
            enabled = false;
            return;
        }

        originalMaterial = spriter.material;
    }

    public void DamagePlayer(float damage)
    {
        if (canTakeDamage == false)
            return;
        currentHealth -= damage;
        StartCoroutine(Flash());

        if (currentHealth <= 0)
        {
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
