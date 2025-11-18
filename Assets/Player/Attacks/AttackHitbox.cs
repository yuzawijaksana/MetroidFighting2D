using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public GameObject originatingPlayer; // Reference to the player who initiated the attack
    public float damage = 5f; // Light attack damage
    public float baseKnockback = 0.5f; // Base knockback
    public float knockbackGrowth = 0.1f; // Knockback growth per damage percent

    [Tooltip("Hitstop duration in frames (e.g., 7 = 7 frames)")]
    [Range(1, 30)]
    public int hitstopFrames = 7;

    [System.Serializable]
    public struct KnockbackVector
    {
        public float x; // Use -1, 0, or 1 for X, will be multiplied by facingSign
        public float y; // Use -1, 0, or 1 for Y
    }

    public KnockbackVector knockback = new KnockbackVector { x = 1, y = 0 }; // Set in inspector
    private PlayerController playerController; // Reference to PlayerController

    // Per-target: last hit time
    private Dictionary<Damageable, float> hitCooldown = new Dictionary<Damageable, float>();

    [Header("Hitbox Settings")]
    public bool singleHitPerActivation = false; // If true, disables after first hit
    public float hitboxActiveTime = 0f; // If > 0, disables after this time

    [Header("Knockback Tuning")]
    [Tooltip("Multiplier for knockback force. Adjust for game feel. 0.05 is a good start for gravity scale 2.5.")]
    public float knockbackMultiplier = 0.5f;
    
    public enum FeedbackType
    {
        None,           // No feedback
        Backwards,      // Push player backwards
        Jump,           // Make player jump (pogo effect)
        BackwardsJump   // Push backwards and make jump
    }
    
    [Tooltip("Type of feedback when attack connects")]
    public FeedbackType feedbackType = FeedbackType.Backwards;
    
    [Tooltip("Horizontal knockback force applied to the attacking player when hit connects")]
    public float attackerHorizontalForce = 5f;
    
    [Tooltip("Vertical force applied to the attacking player (for jump/pogo effects)")]
    public float attackerVerticalForce = 8f;
    
    [Tooltip("Force multiplier for moving attacks (higher values to overcome movement momentum)")]
    public float movingAttackForceMultiplier = 2f;
    
    [Tooltip("Stop attack animation immediately when hit connects")]
    public bool stopAnimationOnHit = true;
    [Tooltip("Disable hitbox immediately when hit connects")]
    public bool disableHitboxOnHit = true;
    [Tooltip("Stop any ongoing movement/attack coroutines")]
    public bool stopMovementOnHit = true;

    private bool hitboxActive = true;
    private HashSet<Damageable> hitThisActivation = new HashSet<Damageable>();
    private Collider2D hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        if (hitboxCollider == null)
            Debug.LogError("AttackHitbox requires a Collider2D.");
    }

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found. Ensure the AttackHitbox is a child of a GameObject with a PlayerController.");
        }
    }

    private void Update()
    {
        // Clean up expired targets (optional, for memory)
        var expired = new List<Damageable>();
        foreach (var kvp in hitCooldown)
        {
            if (kvp.Key == null)
                expired.Add(kvp.Key);
        }
        foreach (var key in expired)
        {
            hitCooldown.Remove(key);
        }
    }

    private void OnEnable()
    {
        hitboxActive = true;
        hitThisActivation.Clear();
        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
    }

    private void OnDisable()
    {
        hitThisActivation.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hitboxActive) return;

        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target == null) return;
        if (collision == null ||
            collision.gameObject == playerController?.gameObject ||
            collision.gameObject == originatingPlayer ||
            target.gameObject == originatingPlayer)
        {
            return;
        }

        // Prevent multiple hits per activation (even if OnTriggerEnter2D is called multiple times)
        if (hitThisActivation.Contains(target))
            return;

        ApplyKnockback(target);
        
        // Apply feedback knockback to attacking player
        ApplyAttackerFeedback();
        
        hitThisActivation.Add(target);

        // Trigger hitstop using GlobalSettings
        if (GlobalSettings.Instance != null && hitstopFrames > 0)
        {
            // Convert frames to seconds, assuming 60 FPS as a common baseline for "frames"
            // Alternatively, use a fixed small value or Time.fixedDeltaTime * hitstopFrames
            float hitstopDurationSeconds = hitstopFrames / 60.0f; 
            // GlobalSettings.Instance.TriggerHitPause(hitstopDurationSeconds);
        }

        // Disable hitbox after first hit to prevent further hits until next activation
        if (disableHitboxOnHit && hitboxCollider != null)
            hitboxCollider.enabled = false;
        hitboxActive = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target != null)
        {
            hitThisActivation.Remove(target);
        }
    }

    private void ApplyKnockback(Damageable target)
    {
        if (target == null) return;

        // Prevent multiple knockbacks per activation (per hit)
        if (hitThisActivation.Contains(target))
            return;

        Vector2 knockbackDir;

        // Check if knockback is set to (0,0) - use relative positioning
        if (knockback.x == 0 && knockback.y == 0)
        {
            // Calculate relative direction from hitbox center to target
            Vector2 hitboxCenter = transform.position;
            Vector2 targetPosition = target.transform.position;
            knockbackDir = (targetPosition - hitboxCenter).normalized;
            
            Debug.Log($"Relative knockback direction for {target.name}: {knockbackDir}");
        }
        else
        {
            // Use predefined knockback direction
            int facingSign = (playerController != null && playerController.isFacingRight) ? 1 : -1;
            knockbackDir = new Vector2(knockback.x * facingSign, knockback.y).normalized;
        }

        float percent = (target.currentHealth <= 0f) ? 1f : target.currentHealth;
        float knockbackForce = (baseKnockback + (percent * knockbackGrowth)) * knockbackMultiplier;

        Vector2 knockbackVec = knockbackDir * knockbackForce;

        target.TakeDamage(damage, knockbackVec, originatingPlayer);

        // Mark as hit for this activation to prevent repeated knockback
        hitThisActivation.Add(target);
        
        Debug.Log($"Knockback applied to {target.name}: Direction={knockbackDir}, Force={knockbackForce}");
    }

    private void ApplyAttackerFeedback()
    {
        if (playerController == null || feedbackType == FeedbackType.None) return;

        // Get player's Rigidbody2D
        Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            Debug.LogWarning("Player Rigidbody2D not found for attacker feedback");
            return;
        }

        // Stop ongoing movement/attack coroutines if enabled
        if (stopMovementOnHit)
        {
            // Stop all coroutines on the player to interrupt moving attacks
            playerController.StopAllCoroutines();
            
            // Try to get BirdController and stop its coroutines too
            BirdController birdController = playerController.GetComponent<BirdController>();
            if (birdController != null)
            {
                birdController.StopAllCoroutines();
                Debug.Log("Stopped BirdController coroutines for moving attack feedback");
            }

            // Reset gravity if it was modified by flying attacks
            if (playerRb.gravityScale != 2.5f) // Assuming 2.5f is normal gravity
            {
                playerRb.gravityScale = 2.5f;
                Debug.Log("Reset gravity scale for feedback");
            }

            // Reset rotation if it was modified by diagonal attacks
            if (playerController.transform.rotation != Quaternion.identity)
            {
                playerController.transform.rotation = Quaternion.identity;
                Debug.Log("Reset rotation for feedback");
            }
        }

        // Calculate force multiplier - higher for moving attacks to overcome momentum
        float forceMultiplier = IsMovingAttack() ? movingAttackForceMultiplier : 1f;
        
        Vector2 feedbackForce = Vector2.zero;
        
        // First, reset current velocity to ensure feedback works
        Vector2 currentVelocity = playerRb.linearVelocity;
        
        switch (feedbackType)
        {
            case FeedbackType.Backwards:
                // Push player backwards (opposite to facing direction)
                int backwardSign = playerController.isFacingRight ? -1 : 1;
                // Reset horizontal velocity first, then apply stronger feedback force
                playerRb.linearVelocity = new Vector2(0f, currentVelocity.y);
                feedbackForce = new Vector2(backwardSign * attackerHorizontalForce * forceMultiplier, 0f);
                playerRb.AddForce(feedbackForce, ForceMode2D.Impulse);
                Debug.Log($"Backwards feedback applied: Force={feedbackForce}, Multiplier={forceMultiplier}");
                break;
                
            case FeedbackType.Jump:
                // Make player jump (pogo effect) - reset Y velocity first for consistent jump
                playerRb.linearVelocity = new Vector2(currentVelocity.x, 0f);
                feedbackForce = new Vector2(0f, attackerVerticalForce * forceMultiplier);
                playerRb.AddForce(feedbackForce, ForceMode2D.Impulse);
                Debug.Log($"Jump feedback applied: Force={feedbackForce}, Multiplier={forceMultiplier}");
                break;
                
            case FeedbackType.BackwardsJump:
                // Combine backwards push with jump
                int backJumpSign = playerController.isFacingRight ? -1 : 1;
                playerRb.linearVelocity = Vector2.zero; // Reset all velocity for maximum effect
                feedbackForce = new Vector2(backJumpSign * attackerHorizontalForce * forceMultiplier, 
                                          attackerVerticalForce * forceMultiplier);
                playerRb.AddForce(feedbackForce, ForceMode2D.Impulse);
                Debug.Log($"Backwards+Jump feedback applied: Force={feedbackForce}, Multiplier={forceMultiplier}");
                break;
        }

        // Stop attack animation immediately
        if (stopAnimationOnHit)
        {
            Animator playerAnimator = playerController.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                // Force transition to idle state
                playerAnimator.SetBool("Idle", true);
                playerAnimator.SetTrigger("Idle");
                Debug.Log("Attack animation stopped due to hit connect");
            }
        }
    }

    // Helper method to detect if this is a moving attack
    private bool IsMovingAttack()
    {
        // Check if the player has significant velocity (indicating a moving attack)
        Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float speed = playerRb.linearVelocity.magnitude;
            return speed > 5f; // Threshold for "moving attack"
        }
        return false;
    }

    public void Initialize(GameObject player)
    {
        originatingPlayer = player;
    }
}
