using UnityEngine;

public class Spikes : MonoBehaviour
{
    [SerializeField] private float spikeDamage;
    [SerializeField] private float knockBackDuration;
    [SerializeField] private Vector2 knockBackForce;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerStats playerStats = collision.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        KnockBackAbility knockBackAbility = collision.GetComponentInParent<KnockBackAbility>();
        if (knockBackAbility != null)
            knockBackAbility.StartKnockBack(knockBackDuration, knockBackForce, transform);

        playerStats.DamagePlayer(spikeDamage);
    }
}
