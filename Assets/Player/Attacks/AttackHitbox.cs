using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public enum HitstopDuration
    {
        OneTwentieth, // 1/20 second
        OneTenth,     // 1/10 second
        OneFifth,     // 1/5 second
        Quarter       // 1/4 second
    }

    public float damage = 5f; // Light attack damage
    public GameObject originatingPlayer; // Reference to the player who initiated the attack
    public float baseKnockback = 5f; // Base knockback force
    public float knockbackGrowth = 0.5f; // Reduced from 0.5f for slower scaling
    public HitstopDuration hitstopDuration = HitstopDuration.OneTenth; // Default hitstop duration

    [System.Serializable]
    public struct KnockbackVector
    {
        public float x; // Use -1, 0, or 1 for X, will be multiplied by facingSign
        public float y; // Use -1, 0, or 1 for Y
    }

    public KnockbackVector knockback = new KnockbackVector { x = 1, y = 0 }; // Set in inspector

    private HashSet<GameObject> hitObjects = new HashSet<GameObject>(); // Track objects already hit
    private PlayerController playerController; // Reference to PlayerController
    private bool isHitstopActive = false; // Add this field

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
        // Search for the Damageable component in the collision object or its children
        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target == null) return;

        if (
            collision == null ||
            hitObjects.Contains(collision.gameObject) ||
            collision.gameObject == playerController?.gameObject ||
            collision.gameObject == originatingPlayer ||
            target.gameObject == originatingPlayer
        ) return;

        // Add the object to the hitObjects set to prevent further hits
        hitObjects.Add(collision.gameObject);

        // Apply damage to the target
        target.TakeDamage(damage, Vector2.zero, originatingPlayer);

        // Apply knockback immediately
        ApplyKnockback(target);

        // Apply hitstop after hit (handled locally)
        // StartCoroutine(ApplyHitstop(GetHitstopDurationInSeconds(hitstopDuration)));
    }

    private void ApplyKnockback(Damageable target)
    {
        if (target == null) return;

        // Always use the knockback values, with facingsign applied to X
        int facingSign = (playerController != null && playerController.isFacingRight) ? 1 : -1;
        Vector2 calculatedKnockbackDirection = new Vector2(knockback.x * facingSign, knockback.y).normalized;

        // Only use knockbackGrowth for scaling (no damageScaling)
        const float maxDamage = 300f;
        float damageRatio = Mathf.Clamp01(target.currentHealth / maxDamage);
        float knockbackForce = baseKnockback * (1f + damageRatio * knockbackGrowth);

        Debug.Log($"[Knockback] {target.name} currentHealth={target.currentHealth}, ratio={damageRatio}, force={knockbackForce}");

        Vector2 knockbackVec = calculatedKnockbackDirection * knockbackForce;

        Rigidbody2D targetRb = target.GetComponentInParent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetRb.AddForce(knockbackVec, ForceMode2D.Impulse);
            Debug.Log($"Knockback applied to {target.name}: Direction={calculatedKnockbackDirection}, Force={knockbackForce}");
        }
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

    private IEnumerator ApplyHitstop(float duration)
    {
        if (isHitstopActive) yield break; // Prevent overlapping hitstop
        isHitstopActive = true;
        Debug.Log($"Hitstop applied for {duration} seconds.");
        float originalTimeScale = Time.timeScale; // Store the original time scale
        Time.timeScale = 0f; // Pause the game
        yield return new WaitForSecondsRealtime(duration); // Wait for the hitstop duration
        Time.timeScale = originalTimeScale; // Restore the original time scale
        isHitstopActive = false;
    }
}
