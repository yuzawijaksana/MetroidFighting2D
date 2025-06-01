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

    public enum HitstopDuration
    {
        OneTwentieth, // 1/20 second
        OneTenth,     // 1/10 second
        OneFifth,     // 1/5 second
        Quarter       // 1/4 second
    }

    public PlayerAttack.AttackType attackType;
    public float damage = 5f; // Light attack damage
    public KnockbackDirection knockbackDirection;
    public GameObject originatingPlayer; // Reference to the player who initiated the attack
    public float baseKnockback = 5f; // Base knockback force
    public float knockbackGrowth = 0.5f; // Reduced from 0.5f for slower scaling
    public float damageScaling = 0.005f; // Reduced from 0.005f for less knockback contribution from damage
    public HitstopDuration hitstopDuration = HitstopDuration.OneTenth; // Default hitstop duration

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
        if (collision == null || hitObjects.Contains(collision.gameObject)) return;

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

        // Ensure the target is not the originating player
        if (target.gameObject == originatingPlayer)
        {
            Debug.LogWarning("Collision with originating player detected. Skipping.");
            return;
        }

        // Adjust knockback formula for stronger scaling
        float knockbackForce = baseKnockback + (damage * knockbackGrowth * damageScaling) + (Mathf.Pow(target.currentHealth / 150f, 1.5f) * knockbackGrowth * 10f);

        // Apply additional knockback force if the target is airborne
        if (!target.IsGrounded())
        {
            knockbackForce *= 1.5f; // Increase knockback by 50% for airborne targets
        }

        Vector2 knockbackDirectionVector = GetKnockbackDirection(target.transform, knockbackDirection);
        Vector2 knockback = knockbackDirectionVector * knockbackForce;

        // Apply hitstop effect directly
        StartCoroutine(ApplyHitstop(GetHitstopDurationInSeconds(hitstopDuration)));

        // Apply knockback directly to the Rigidbody2D of the target
        Rigidbody2D targetRb = target.GetComponentInParent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector2.zero; // Reset velocity before applying knockback
            targetRb.AddForce(knockback, ForceMode2D.Impulse);
        }

        // Apply damage to the target
        target.TakeDamage(damage, knockback, originatingPlayer);

        hitObjects.Add(collision.gameObject);
    }

    private IEnumerator ApplyHitstop(float duration)
    {
        float originalTimeScale = Time.timeScale; // Store the original time scale
        Time.timeScale = 0f; // Pause the game
        yield return new WaitForSecondsRealtime(duration); // Wait for the hitstop duration
        Time.timeScale = originalTimeScale; // Restore the original time scale
    }

    private float GetHitstopDurationInSeconds(HitstopDuration duration)
    {
        return duration switch
        {
            HitstopDuration.OneTwentieth => 0.05f, // 1/20 second
            HitstopDuration.OneTenth => 0.1f,     // 1/10 second
            HitstopDuration.OneFifth => 0.2f,     // 1/5 second
            HitstopDuration.Quarter => 0.25f,     // 1/4 second
            _ => 0.1f // Default to 1/10 second
        };
    }

    private void ApplyKnockback(Damageable target, KnockbackDirection directionType, float force)
    {
        if (target == null || target.gameObject == null)
        {
            return;
        }

        // Calculate knockback direction directly away from the attacker
        Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;

        // Apply knockback as an impulse
        Rigidbody2D targetRigidbody = target.GetComponentInParent<Rigidbody2D>();
        if (targetRigidbody != null)
        {
            // Reset velocity before applying knockback to avoid conflicts
            targetRigidbody.linearVelocity = Vector2.zero;

            // Apply knockback force
            targetRigidbody.AddForce(knockbackDirection * force, ForceMode2D.Impulse);
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

    private Vector2 GetKnockbackDirection(Transform targetTransform, KnockbackDirection directionType)
    {
        // Calculate relative position between attacker and target
        Vector2 relativePosition = (targetTransform.position - transform.position).normalized;

        switch (directionType)
        {
            case KnockbackDirection.Up:
                return Vector2.up;
            case KnockbackDirection.Down:
                return Vector2.down;
            case KnockbackDirection.Side:
                return new Vector2(relativePosition.x, 0).normalized; // Horizontal knockback
            case KnockbackDirection.SideUp:
                return new Vector2(relativePosition.x, 1).normalized; // Diagonal upward knockback
            case KnockbackDirection.SideDown:
                return new Vector2(relativePosition.x, -1).normalized; // Diagonal downward knockback
            default:
                return relativePosition; // Default to relative position for dynamic knockback
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
