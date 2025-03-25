using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public PlayerAttack.AttackType attackType; // Type of attack
    public float damage; // Damage value
    public float knockbackForce; // Knockback force

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collided object has a Damageable component
        Damageable target = collision.GetComponent<Damageable>();
        if (target != null)
        {
            // Apply damage and knockback to the target
            Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
            target.TakeDamage(damage, knockbackDirection * knockbackForce);
        }
    }
}
