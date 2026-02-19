using System.Collections.Generic;
using UnityEngine;

public class EnemyContactKnockback : MonoBehaviour
{
    [Header("Contact Knockback Settings")]
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private Vector2 contactKnockback = new Vector2(5f, 3f);
    [SerializeField] private float damageInterval = 0.5f;

    private Dictionary<Damageable, float> damageCooldown = new Dictionary<Damageable, float>();
    private Transform enemyTransform;

    private void Start()
    {
        enemyTransform = transform.parent != null ? transform.parent : transform;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponentInChildren<Damageable>();
        if (damageable == null)
        {
            return;
        }
        
        // Check cooldown for this specific target
        if (!damageCooldown.ContainsKey(damageable) || Time.time >= damageCooldown[damageable])
        {
            // Calculate knockback direction
            Vector2 knockbackDirection = (collision.transform.position - enemyTransform.position).normalized;
            Vector2 finalKnockback = new Vector2(knockbackDirection.x * contactKnockback.x, contactKnockback.y);
            
            // Deal damage
            damageable.TakeDamage(contactDamage, finalKnockback, enemyTransform.gameObject);
            
            // Set cooldown
            damageCooldown[damageable] = Time.time + damageInterval;
        }
    }
}
