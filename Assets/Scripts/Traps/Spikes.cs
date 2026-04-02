using UnityEngine;

public class Spikes : MonoBehaviour
{
    [SerializeField] private float spikeDamage;
    [SerializeField] private float knockBackDuration;
    [SerializeField] private Vector2 knockBackForce;

    [Header("Death pacing")]
    [SerializeField] private float deathDelaySeconds = 0.25f;
    [SerializeField] private bool waitForGroundBeforeDeath = false;
    [SerializeField] private float maxWaitForGround = 0.6f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerStats playerStats = collision.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        KnockBackAbility knockBackAbility = collision.GetComponentInParent<KnockBackAbility>();
        if (knockBackAbility == null) return;

        if (playerStats.GetCurrentHealth() <= 0)
            playerStats.SetDeferDeath(true);

        if (knockBackAbility != null)
            knockBackAbility.StartKnockBack(knockBackDuration, knockBackForce, transform, deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround);
        playerStats.DamagePlayer(spikeDamage);
    }
}
