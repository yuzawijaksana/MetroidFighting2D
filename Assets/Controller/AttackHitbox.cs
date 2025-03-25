using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public enum KnockbackDirection
    {
        Up,
        Down,
        Side,
        SideUp,
        SideDown
    }

    public PlayerAttack.AttackType attackType;
    public float damage;
    public KnockbackDirection knockbackDirection;
    public KnockbackDirection airKnockbackDirection; // Knockback direction for airborne targets
    public float groundedKnockbackForce = 5f; // Knockback force for grounded targets
    public float airKnockbackForce = 7.5f; // Knockback force for airborne targets
    public float airKnockbackDelay = 0.2f; // Delay before applying air knockback

    private HashSet<GameObject> hitObjects = new HashSet<GameObject>(); // Track objects already hit
    private PlayerController playerController; // Reference to PlayerController

    private void Start()
    {
        // Get the PlayerController from the parent or attached GameObject
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found. Ensure the AttackHitbox is a child of a GameObject with a PlayerController.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore collisions with the player's own GameObject
        if (collision.gameObject == playerController.gameObject)
        {
            Debug.Log($"Ignored collision with own player: {collision.gameObject.name}");
            return;
        }

        if (hitObjects.Contains(collision.gameObject))
        {
            Debug.Log($"Already hit {collision.gameObject.name}, skipping.");
            return; // Skip if the object has already been hit
        }

        Debug.Log($"AttackHitbox collided with {collision.gameObject.name} on layer {collision.gameObject.layer}");

        Damageable target = collision.GetComponent<Damageable>();
        if (target != null)
        {
            Debug.Log($"Damageable component found on {collision.gameObject.name}. Processing knockback and damage.");

            // Determine if the target is grounded or airborne
            bool isTargetGrounded = target.IsGrounded();

            // Apply grounded knockback immediately
            if (isTargetGrounded)
            {
                ApplyKnockback(target, knockbackDirection, groundedKnockbackForce);
                StartCoroutine(ApplyAirKnockbackAfterDelay(target));
            }
            else
            {
                // Apply air knockback directly if already airborne
                ApplyKnockback(target, airKnockbackDirection, airKnockbackForce);
            }

            target.TakeDamage(damage, Vector2.zero); // Pass knockback direction if needed

            // Flip the enemy to face the attack direction
            FlipTargetToFaceAttack(target.transform);

            // Add the object to the hitObjects set to prevent duplicate hits
            hitObjects.Add(collision.gameObject);

            // Reset the hitbox after processing the attack
            ResetHitbox();
        }
        else
        {
            Debug.LogWarning($"No Damageable component found on {collision.gameObject.name}. Ensure the Hurtbox has the Damageable component.");
        }
    }

    private void ApplyKnockback(Damageable target, KnockbackDirection directionType, float force)
    {
        Vector2 direction = GetKnockbackDirection(target.transform.position, directionType) * force;

        // Retrieve Rigidbody2D from the parent of the Damageable component
        Rigidbody2D targetRigidbody = target.GetComponentInParent<Rigidbody2D>();
        if (targetRigidbody != null)
        {
            // Reset velocity before applying knockback to avoid conflicts
            targetRigidbody.linearVelocity = Vector2.zero;

            // Apply knockback as an impulse
            targetRigidbody.AddForce(direction, ForceMode2D.Impulse);

            Debug.Log($"Applied knockback to {target.gameObject.name} with force {direction}");
        }
        else
        {
            Debug.LogWarning($"No Rigidbody2D found on {target.gameObject.name} or its parent. Knockback cannot be applied.");
        }
    }

    private IEnumerator ApplyAirKnockbackAfterDelay(Damageable target)
    {
        yield return new WaitForSeconds(airKnockbackDelay);

        // Check if the target is still airborne before applying air knockback
        if (!target.IsGrounded())
        {
            ApplyKnockback(target, airKnockbackDirection, airKnockbackForce);
        }
    }

    private void ResetHitbox()
    {
        // Reset the hitbox state to ensure it can detect collisions again
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false; // Temporarily disable the collider
            collider.enabled = true;  // Re-enable the collider to reset its state
            Debug.Log("Hitbox has been reset.");
        }

        // Clear the hitObjects set to allow new hits in the next attack
        hitObjects.Clear();
    }

    private Vector2 GetKnockbackDirection(Vector3 targetPosition, KnockbackDirection directionType)
    {
        // Use PlayerController's facing direction
        float facingDirection = playerController != null && playerController.isFacingRight ? 1 : -1;

        switch (directionType)
        {
            case KnockbackDirection.Up:
                return Vector2.up;
            case KnockbackDirection.Down:
                return Vector2.down;
            case KnockbackDirection.Side:
                return new Vector2(facingDirection, 0).normalized;
            case KnockbackDirection.SideUp:
                return new Vector2(facingDirection, 0.5f).normalized;
            case KnockbackDirection.SideDown:
                return new Vector2(facingDirection, -0.5f).normalized;
            default:
                return Vector2.zero;
        }
    }

    private void FlipTargetToFaceAttack(Transform targetTransform)
    {
        // Flip the parent of the target to face the direction of the attack
        Transform parentTransform = targetTransform.parent;
        if (parentTransform != null)
        {
            float attackDirection = playerController != null && playerController.isFacingRight ? -1 : 1;
            parentTransform.localScale = new Vector3(Mathf.Abs(parentTransform.localScale.x) * attackDirection, parentTransform.localScale.y, parentTransform.localScale.z);
            Debug.Log($"Flipped {parentTransform.name} to face attack direction.");
        }
        else
        {
            Debug.LogWarning($"Target {targetTransform.name} has no parent to flip.");
        }
    }
}
