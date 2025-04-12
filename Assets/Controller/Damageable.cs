using UnityEngine;

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

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
        currentHealth = maxHealth;

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

    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        // Ignore damage from the same player
        if (attacker == transform.parent?.gameObject)
        {
            Debug.LogWarning("Damage from the same player ignored.");
            return;
        }

        // Increment damage instead of reducing health
        currentHealth += damage;

        // Scale knockback based on current health
        float knockbackMultiplier = 1 + (currentHealth / maxHealth);
        Vector2 scaledKnockback = knockback * knockbackMultiplier;

        ApplyKnockback(scaledKnockback);

        // Apply stun
        StartCoroutine(ApplyStun());

        Debug.Log($"Player knocked back with force {scaledKnockback} at health {currentHealth}.");
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        Rigidbody2D parentRb = transform.parent != null ? transform.parent.GetComponent<Rigidbody2D>() : null;

        if (parentRb != null)
        {
            parentRb.linearVelocity = Vector2.zero; // Reset velocity before applying knockback
            parentRb.AddForce(knockback, ForceMode2D.Impulse);
        }
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

    private System.Collections.IEnumerator ApplyStun()
    {
        isStunned = true;

        // Play stun animation if available
        if (anim != null)
        {
            anim.SetBool("Stunned", true);
        }

        yield return new WaitForSeconds(stunDuration);

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
}
