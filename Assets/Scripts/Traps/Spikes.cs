using UnityEngine;

public class Spikes : MonoBehaviour
{
    [SerializeField] private float spikeDamage;
    [SerializeField] private float knockBackDuration;
    [SerializeField] private Vector2 knockBackForce;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        KnockBackAbility knockBackAbility = collision.GetComponentInParent<KnockBackAbility>();
        knockBackAbility.StartKnockBack(knockBackDuration, knockBackForce, transform);
        // StartCoroutine(knockBackAbility.KnockBack(knockBackDuration, knockBackForce, transform));

        PlayerStats playerStats = collision.GetComponent<PlayerStats>();
        playerStats.DamagePlayer(spikeDamage);
    }
}
