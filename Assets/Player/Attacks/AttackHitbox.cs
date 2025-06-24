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
        hitThisActivation.Add(target);

        // Trigger hitstop using GlobalSettings
        if (GlobalSettings.Instance != null && hitstopFrames > 0)
        {
            // Convert frames to seconds, assuming 60 FPS as a common baseline for "frames"
            // Alternatively, use a fixed small value or Time.fixedDeltaTime * hitstopFrames
            float hitstopDurationSeconds = hitstopFrames / 60.0f; 
            GlobalSettings.Instance.TriggerHitPause(hitstopDurationSeconds);
        }

        // Disable hitbox after first hit to prevent further hits until next activation
        if (hitboxCollider != null)
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

    public void Initialize(GameObject player)
    {
        originatingPlayer = player;
    }
}
