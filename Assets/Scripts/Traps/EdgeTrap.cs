using UnityEngine;

/// <summary>
/// Simple trigger hazard that applies damage when the player enters its area.
///
/// Unlike more advanced traps, this class only forwards damage to
/// <see cref="PlayerStats"/> and does not handle knockback or death pacing.
/// </summary>
public class EdgeTrap : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        PlayerStats playerStats = collision.GetComponent<PlayerStats>();

        if (playerStats == null)
            return;

        playerStats.DamagePlayer(damage);
    }
}