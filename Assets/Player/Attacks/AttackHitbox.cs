using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public GameObject originatingPlayer; // Reference to the player who initiated the attack
    public float damage = 5f; // Light attack damage
    public float baseKnockback = 0.5f; // Small knockback per frame
    public float knockbackGrowth = 0.1f; // Growth per hit/frame

    [Tooltip("Hitstop duration in frames (e.g., 7 = 7 frames)")]
    [Range(1, 30)]
    public int hitstopFrames = 7;

    [System.Serializable]
    public struct KnockbackVector{
        public float x; // Use -1, 0, or 1 for X, will be multiplied by facingSign
        public float y; // Use -1, 0, or 1 for Y
    }

    public KnockbackVector knockback = new KnockbackVector { x = 1, y = 0 }; // Set in inspector
    private PlayerController playerController; // Reference to PlayerController

    // Per-target: last knockback time, accumulated knockback
    private Dictionary<Damageable, float> hitCooldown = new Dictionary<Damageable, float>();
    private Dictionary<Damageable, float> knockbackAccum = new Dictionary<Damageable, float>();

    private bool hitStopActive = false;

    private void Start()
    {
        // Get the PlayerController from the parent or attached GameObject
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
            knockbackAccum.Remove(key);
        }
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        hitStopActive = true;
        Time.timeScale = 0.0f;
        Debug.Log("[AttackHitbox] Time.timeScale set to 0 (hit pause started)");

        float timer = 0f;
        while (timer < duration)
        {
            yield return null;
            timer += Time.unscaledDeltaTime;
        }

        Time.timeScale = 1.0f;
        hitStopActive = false;
        Debug.Log("[AttackHitbox] Time.timeScale set to 1 (hit pause ended)");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log($"[AttackHitbox] Time.timeScale={Time.timeScale}");

        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target == null)
        {
            Debug.Log("No Damageable found on collision: " + collision?.name);
            return;
        }
        if (collision == null ||
            collision.gameObject == playerController?.gameObject ||
            collision.gameObject == originatingPlayer ||
            target.gameObject == originatingPlayer)
        {
            Debug.Log("Collision ignored: " + collision?.name);
            return;
        }

        // Calculate hitstop duration in seconds (7 frames at 60fps)
        float hitstopDuration = hitstopFrames / 60f;

        // Only apply if not in hitstop for this target
        float now = Time.time;
        if (!hitCooldown.TryGetValue(target, out float nextValidTime) || now >= nextValidTime)
        {
            // Accumulate knockback for this target
            if (!knockbackAccum.ContainsKey(target))
            {
                knockbackAccum[target] = 0f;
                Debug.Log($"First knockback for {target.name}");
            }
            knockbackAccum[target] += knockbackGrowth;
            Debug.Log($"Accum knockback for {target.name}: {knockbackAccum[target]}");

            // Apply knockback
            Debug.Log($"Applying knockback to {target.name} at time {now}");
            ApplyKnockback(target, knockbackAccum[target]);

            // Only apply hitstop if Time.timeScale is 1 and NO OTHER hitstop is running
            if (!hitStopActive && Time.timeScale == 1f)
            {
                Debug.Log($"Applying hitstop to {target.name} for {hitstopDuration} seconds");
                StartCoroutine(HitStopCoroutine(hitstopDuration));
            }
            else
            {
                Debug.Log($"Skipped hitstop for {target.name} because a hitstop is already active or Time.timeScale != 1");
            }

            // Set next valid time for this target
            hitCooldown[target] = now + hitstopDuration;
        }
        else
        {
            Debug.Log($"Hit cooldown active for {target.name}, next valid: {nextValidTime}, now: {now}");
        }

        // Reset knockback accumulation if not in hitbox anymore (prevents infinite stacking)
        // This should be handled in OnTriggerExit2D
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target != null)
        {
            Debug.Log($"Resetting knockback accumulation for {target.name} (OnTriggerExit2D)");
            knockbackAccum.Remove(target);
            hitCooldown.Remove(target);
        }
    }

    private void ApplyKnockback(Damageable target, float extraKnockback)
    {
        if (target == null)
        {
            Debug.Log("ApplyKnockback: target is null");
            return;
        }

        int facingSign = (playerController != null && playerController.isFacingRight) ? 1 : -1;
        Vector2 calculatedKnockbackDirection = new Vector2(knockback.x * facingSign, knockback.y).normalized;

        float knockbackForce = baseKnockback + extraKnockback;
        knockbackForce = Mathf.Clamp(knockbackForce, 0, baseKnockback + knockbackGrowth * 10);

        Vector2 knockbackVec = calculatedKnockbackDirection * knockbackForce;

        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null)
        {
            targetRb = target.GetComponentInParent<Rigidbody2D>();
            if (targetRb == null)
            {
                Debug.LogWarning($"No Rigidbody2D found for {target.name}");
                return;
            }
        }

        Debug.Log($"AddForce to {target.name}: {knockbackVec} (Force: {knockbackForce}, Dir: {calculatedKnockbackDirection})");
        targetRb.AddForce(knockbackVec, ForceMode2D.Force);
    }

    public void Initialize(GameObject player)
    {
        originatingPlayer = player;
    }
}
