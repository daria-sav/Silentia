using UnityEngine;

/// <summary>
/// Hazard trigger that applies spike damage and starts knockback on contact.
///
/// This trap uses <see cref="PlayerStats"/> to apply damage and
/// <see cref="KnockBackAbility"/> to launch the player away from the spike source.
/// Death pacing settings are forwarded to the knockback system so spike hits
/// can be tuned from the inspector.
/// </summary>
public class Spikes : MonoBehaviour
{
    [SerializeField] private float spikeDamage;
    [SerializeField] private float knockBackDuration;
    [SerializeField] private Vector2 knockBackForce;

    [Header("Death pacing")]
    [SerializeField] private float deathDelaySeconds = 0.25f;
    [SerializeField] private bool waitForGroundBeforeDeath = false;
    [SerializeField] private float maxWaitForGround = 0.6f;

    // ─────────────── TRIGGER LOGIC ───────────────

    #region Trigger Handling
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        PlayerStats playerStats = collision.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        KnockBackAbility knockBackAbility = collision.GetComponentInParent<KnockBackAbility>();
        if (knockBackAbility == null) 
            return;

        if (playerStats.GetCurrentHealth() <= 0)
            playerStats.SetDeferDeath(true);

        if (knockBackAbility != null)
            knockBackAbility.StartKnockBack(knockBackDuration, knockBackForce, transform, deathDelaySeconds, waitForGroundBeforeDeath, maxWaitForGround);

        playerStats.DamagePlayer(spikeDamage);
    }
    #endregion
}
