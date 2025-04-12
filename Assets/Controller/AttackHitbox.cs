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
    public GameObject originatingPlayer; // Reference to the player who initiated the attack

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
        ProcessHit(collision);
    }

    private void ProcessHit(Collider2D collision)
    {
        if (collision == null)
        {
            Debug.LogWarning("Collision object is null. Skipping hit processing.");
            return;
        }

        // Ignore collisions with the player's own GameObject
        if (collision.gameObject == playerController?.gameObject)
        {
            Debug.LogWarning("Collision with own player detected. Skipping.");
            return;
        }

        // Ignore collisions with the originating player
        if (collision.gameObject == originatingPlayer)
        {
            Debug.LogWarning("Collision with originating player detected. Skipping.");
            return;
        }

        if (hitObjects.Contains(collision.gameObject))
        {
            Debug.LogWarning($"Collision with {collision.gameObject.name} already processed. Skipping.");
            return; // Skip if the object has already been hit
        }

        // Search for the Damageable component in the collision object or its children
        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target == null)
        {
            Debug.LogWarning($"No Damageable component found on {collision.gameObject.name} or its hierarchy. Skipping.");
            return;
        }

        // Determine if the target is grounded or airborne
        bool isTargetGrounded = target.IsGrounded();
        Vector2 knockback = isTargetGrounded
            ? GetKnockbackDirection(target.transform.position, knockbackDirection) * groundedKnockbackForce
            : GetKnockbackDirection(target.transform.position, airKnockbackDirection) * airKnockbackForce;

        // Apply damage and knockback
        target.TakeDamage(damage, knockback, originatingPlayer); // Pass originatingPlayer as the attacker

        // Log when a player hits another player
        Debug.Log($"Player hit {collision.gameObject.name} for {damage} damage.");

        hitObjects.Add(collision.gameObject);
    }

    private void ApplyKnockback(Damageable target, KnockbackDirection directionType, float force)
    {
        if (target == null || target.gameObject == null)
        {
            return;
        }

        Vector2 direction = GetKnockbackDirection(target.transform.position, directionType) * force;

        // Retrieve Rigidbody2D from the parent of the Damageable component
        Rigidbody2D targetRigidbody = target.GetComponentInParent<Rigidbody2D>();
        if (targetRigidbody != null)
        {
            // Reset velocity before applying knockback to avoid conflicts
            targetRigidbody.linearVelocity = Vector2.zero;

            // Apply knockback as an impulse
            targetRigidbody.AddForce(direction, ForceMode2D.Impulse);
        }
    }

    private IEnumerator ApplyAirKnockbackAfterDelay(Damageable target)
    {
        yield return new WaitForSeconds(airKnockbackDelay);

        // Check if the target is null or destroyed before accessing it
        if (target == null || target.gameObject == null)
        {
            yield break;
        }

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
        }

        // Clear the hitObjects set to allow new hits in the next attack
        hitObjects.Clear();
    }

    public void ResetHitObjects()
    {
        hitObjects.Clear();
        Debug.Log("Hit objects have been reset.");
    }

    private void OnEnable()
    {
        ResetHitObjects();
    }

    private void OnDisable()
    {
        // Reset hit objects when the hitbox is disabled
        ResetHitObjects();
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
        }
    }

    public void StartAttack(float duration)
    {
        // Start the attack and reset hit objects after the duration
        StartCoroutine(ResetHitObjectsAfterDuration(duration));
    }

    private IEnumerator ResetHitObjectsAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        ResetHitObjects();
    }

    public void Initialize(GameObject player)
    {
        originatingPlayer = player;
    }
}
