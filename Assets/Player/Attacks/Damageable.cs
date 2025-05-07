using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Health Settings")]
    [SerializeField] public float currentHealth;

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 0.5f; // Duration of the stun
    private bool isStunned = false;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private PlayerController playerController; // Reference to PlayerController

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = 0f; // Initialize currentHealth to 0

        // Try to get the PlayerController component
        playerController = GetComponentInParent<PlayerController>();

        // If PlayerController exists, use its groundCheckPoint
        if (playerController != null)
        {
            groundCheckPoint = playerController.groundCheckPoint;
            whatIsGround = playerController.whatIsGround;
        }
        else if (groundCheckPoint == null)
        {
            Debug.LogError($"GroundCheckPoint is not assigned for {gameObject.name}. Ensure it is set in the inspector or via PlayerController.");
        }
    }

    private void Update()
    {
        // Ensure the Inspector reflects the current health during runtime
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        // Ignore damage from the same player
        if (attacker == transform.parent?.gameObject)
        {
            Debug.LogWarning("Damage from the same player ignored.");
            return;
        }

        // Log debug information about the damage received
        Debug.Log($"Received {damage} damage from {attacker.name} at position {attacker.transform.position}.");

        // Increment currentHealth
        currentHealth += damage;

        // Scale knockback based on health increments of 10
        float knockbackMultiplier = 1 + (currentHealth / 100f); // Adjust multiplier logic for every 10 health
        Vector2 scaledKnockback = knockback * knockbackMultiplier;

        // Apply the scaled knockback
        ApplyKnockback(scaledKnockback);

        // Apply stun
        ApplyStun();

        Debug.Log($"Player knocked back with force {scaledKnockback} at health {currentHealth}.");
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        Rigidbody2D parentRb = transform.parent != null ? transform.parent.GetComponent<Rigidbody2D>() : null;

        if (parentRb != null)
        {
            // Reset velocity before applying knockback
            parentRb.linearVelocity = Vector2.zero;

            // Temporarily set gravity scale to 0
            float originalGravityScale = parentRb.gravityScale;
            parentRb.gravityScale = 0;

            // Apply knockback force
            parentRb.AddForce(knockback, ForceMode2D.Impulse);

            // Restore gravity scale after a short delay
            StartCoroutine(RestoreGravityAfterDelay(parentRb, originalGravityScale, 0.1f));
        }
    }

    private IEnumerator RestoreGravityAfterDelay(Rigidbody2D rb, float originalGravityScale, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
        }
    }

    private async void StopMovementAfterKnockback(PlayerController playerController, float delay)
    {
        await Task.Delay((int)(delay * 1000)); // Convert seconds to milliseconds
        playerController.ApplyHitRecovery();
    }

    public bool IsGrounded()
    {
        if (groundCheckPoint == null)
        {
            Debug.LogError($"GroundCheckPoint is not assigned for {gameObject.name}. Ensure it is set in the inspector or via PlayerController.");
            return false;
        }

        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, whatIsGround);
    }

    private async void ApplyStun()
    {
        isStunned = true;

        // Play stun animation if available
        if (anim != null)
        {
            anim.SetBool("Stunned", true);
        }

        await Task.Delay((int)(stunDuration * 1000)); // Convert seconds to milliseconds

        isStunned = false;

        // Reset stun animation
        if (anim != null)
        {
            anim.SetBool("Stunned", false);
        }
    }

    private void ResetHurtbox()
    {
        // Reset the hurtbox state to ensure it can detect collisions again
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false; // Temporarily disable the collider
            collider.enabled = true;  // Re-enable the collider to reset its state
        }
    }

    private void Die()
    {
        // Reset health before destroying the object
        ResetHealth();

        // Destroy the sprite renderer to remove the visual representation
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Destroy(spriteRenderer);
        }

        // Destroy the parent GameObject
        GameObject parentObject = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(parentObject, 0.1f); // Optional delay for cleanup
    }

    public void ResetHealth()
    {
        currentHealth = 0;
        Debug.Log($"Health reset to {currentHealth} for {gameObject.name}.");
    }

    public void ResetHealthTo(float value)
    {
        currentHealth = value;
        Debug.Log($"Health reset to {currentHealth} for {gameObject.name}.");
    }
}
