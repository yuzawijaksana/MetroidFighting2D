using UnityEngine;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        // Reduce health
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");

        // Apply knockback
        if (rb != null)
        {
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }

        // Play hit animation
        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }

        // Check if the character is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        // Destroy the GameObject
        Destroy(gameObject);
    }
}
