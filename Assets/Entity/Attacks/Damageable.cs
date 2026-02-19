using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Damageable : MonoBehaviour
{
    // ==================== SHARED COMPONENTS ====================
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    public CharacterIngameCellUI cellUI;

    // ==================== GAME MODE ====================
    [Header("═══════════════ GAME MODE ═══════════════")]
    [Space(10)]
    [Tooltip("TRUE = Story Mode (dies at max health) | FALSE = PvP Mode (infinite damage)")]
    [SerializeField] private bool isStoryMode = true;
    [Space(20)]
    
    // ==================== SHARED SETTINGS ====================
    [Header("═══════════════ SHARED SETTINGS ═══════════════")]
    [Space(10)]
    [SerializeField] public float currentHealth;
    [SerializeField] public int maxHearts = 3;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Stun Settings")]
    private float stunDuration = 0.75f;
    private bool isStunned = false;
    
    private float lastHitTime = -1f;
    private float hitCooldown = 0.05f;
    [Space(20)]

    [Header("Contact Damage Settings")]
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private Vector2 contactKnockback = new Vector2(5f, 3f);
    [SerializeField] private float damageInterval = 0.5f;
    private Dictionary<Damageable, float> damageCooldown = new Dictionary<Damageable, float>();
    private HashSet<Damageable> currentlyCollidingWith = new HashSet<Damageable>(); // Track current collisions

    [Header("I-Frame Settings")]
    [SerializeField] private float iFrameDuration = 2f; // 2 second invulnerability
    private float iFrameTimer = 0f;
    [Space(20)]
    
    // ==================== PVP MODE SETTINGS ====================
    [Header("█████████ PVP MODE █████████")]
    [Space(5)]
    [TextArea(2, 3)]
    [SerializeField] private string pvpInfo = "PvP Mode:\n• Damage accumulates infinitely\n• No death - eliminated by ring-outs";
    [Space(20)]
    
    // ==================== STORY MODE SETTINGS ====================
    [Header("█████████ STORY MODE █████████")]
    [Space(5)]
    [Tooltip("Player dies when health reaches this value")]
    [SerializeField] private float maxHealthThreshold = 300f;
    [TextArea(2, 3)]
    [SerializeField] private string storyInfo = "Story Mode:\n• Dies at 300% damage\n• Respawn/Game Over on death";
    private bool isDead = false;
    
    // ==================== INITIALIZATION ====================
    private void Start()
    {
        currentHealth = 0f;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
        playerController = GetComponentInParent<PlayerController>();

        if (playerController != null)
        {
            groundCheckPoint = playerController.groundCheckPoint;
            whatIsGround = playerController.whatIsGround;
        }
        else if (groundCheckPoint == null)
        {
            Debug.LogError($"GroundCheckPoint is not assigned for {gameObject.name}. Ensure it is set in the inspector or via PlayerController.");
        }

        // Update mask color and initialize hearts on start
        if (cellUI != null)
        {
            cellUI.UpdateMaskColor(this);
            cellUI.InitHearts(maxHearts);
            cellUI.SetHearts(maxHearts);
            Debug.Log($"Damageable UI initialized for {gameObject.name}");
        }
        else
        {
            Debug.Log($"cellUI is null for {gameObject.name} - will be linked later by GameStarter");
        }
    }

    private void Update()
    {
        // Update i-frame timer
        if (iFrameTimer > 0)
        {
            iFrameTimer -= Time.deltaTime;
        }
        // When i-frames just ended, reset collision tracking so knockback can reapply
        else if (iFrameTimer <= 0 && currentlyCollidingWith.Count > 0)
        {
            Debug.Log($"[Damageable] I-frames ended, resetting collision tracking for {currentlyCollidingWith.Count} targets");
            currentlyCollidingWith.Clear();
        }

        // Backup detection method using physics overlap if triggers don't work
        DetectNearbyDamageable();
    }

    // Fallback collision detection using Physics2D
    private void DetectNearbyDamageable()
    {
        // Get capsule collider
        CapsuleCollider2D capsuleCol = GetComponent<CapsuleCollider2D>();
        if (capsuleCol == null) return;

        // Use OverlapBox to detect nearby Damageable objects
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, capsuleCol.size * 1.2f, 0f);
        HashSet<Damageable> stillColliding = new HashSet<Damageable>();

        foreach (Collider2D hit in hits)
        {
            if (hit == capsuleCol) continue; // Skip self

            Damageable otherDamageable = hit.GetComponent<Damageable>();
            if (otherDamageable == null)
            {
                otherDamageable = hit.GetComponentInChildren<Damageable>();
            }
            if (otherDamageable == null)
            {
                otherDamageable = hit.GetComponentInParent<Damageable>();
            }

            if (otherDamageable != null && otherDamageable != this)
            {
                // Only apply contact damage if the OTHER character is the player, not if it's an enemy
                EnemyAIController enemyController = otherDamageable.GetComponentInParent<EnemyAIController>();
                if (enemyController != null)
                {
                    continue; // Skip contact damage for enemies
                }

                stillColliding.Add(otherDamageable);

                // Only apply knockback on FIRST contact (not already in collision list)
                if (!currentlyCollidingWith.Contains(otherDamageable))
                {
                    // Knockback away from the enemy (relative to collision point)
                    Vector2 knockbackDirection = (hit.transform.position - transform.position).normalized;
                    Vector2 knockbackDir = new Vector2(knockbackDirection.x * contactKnockback.x, contactKnockback.y);
                    
                    Debug.Log($"[Damageable] CONTACT: Dealing {contactDamage} damage to {otherDamageable.gameObject.name}. Knockback dir: {knockbackDir}");
                    
                    otherDamageable.TakeDamage(contactDamage, knockbackDir, gameObject);
                    currentlyCollidingWith.Add(otherDamageable);
                }
            }
        }

        // Clear objects we're no longer touching
        var noLongerColliding = new List<Damageable>(currentlyCollidingWith);
        foreach (var obj in noLongerColliding)
        {
            if (!stillColliding.Contains(obj))
            {
                currentlyCollidingWith.Remove(obj);
                Debug.Log($"[Damageable] No longer in contact with {obj.gameObject.name}");
            }
        }
    }

    // ==================== CORE DAMAGE SYSTEM ====================
    // Applies damage, knockback, and stun to the object
    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        if (isDead) return; // Story mode only
        
        // Check if in i-frames (invulnerable)
        if (iFrameTimer > 0)
        {
            Debug.Log($"[Damageable] {gameObject.name} is invulnerable for {iFrameTimer:F1}s more");
            return;
        }
        
        // Check if this character is attacking - if so, block contact knockback
        PlayerController playerCtrl = GetComponentInParent<PlayerController>();
        EnemyAIController enemyCtrl = GetComponentInParent<EnemyAIController>();
        if ((playerCtrl != null && playerCtrl.IsAttackLocked) || (enemyCtrl != null && enemyCtrl.IsAttackLocked))
        {
            Debug.Log($"[Damageable] {gameObject.name} is attacking - blocking contact knockback");
            knockback = Vector2.zero; // Remove knockback but damage still applies
        }
        
        // Set i-frame timer and start blinking
        iFrameTimer = iFrameDuration;
        StopCoroutine(nameof(BlinkBlackCoroutine)); // Stop any existing blink
        StartCoroutine(BlinkBlackCoroutine(iFrameDuration));
        
        // Clear collision tracking so knockback can reapply after i-frames end
        currentlyCollidingWith.Clear();
        
        // Prevent double application in the same frame
        if (Time.time - lastHitTime < hitCooldown)
            return;
        lastHitTime = Time.time;

        Debug.Log($"Applying hit effect: Damage={damage}, Knockback={knockback}, Attacker={attacker?.name}");

        if (attacker == transform.parent?.gameObject) return;

        if (damage > 0)
        {
            currentHealth += damage;
            Debug.Log($"Health updated to {currentHealth} for {gameObject.name}");
        }

        // Apply knockback based on game mode
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            // Reset momentum before applying new knockback
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            Vector2 finalKnockback;
            
            if (isStoryMode)
            {
                // ==================== STORY MODE: CONSISTENT KNOCKBACK ====================
                // No scaling - use base knockback as-is
                finalKnockback = knockback;
                Debug.Log($"[STORY MODE] Applying consistent knockback: {finalKnockback}");
            }
            else
            {
                // ==================== PVP MODE: SCALED KNOCKBACK ====================
                // Scale knockback with damage percentage (Smash Bros style)
                float damageMultiplier = 1f + (currentHealth / 100f); // 1x at 0%, 2x at 100%, 3x at 200%, etc.
                finalKnockback = knockback * damageMultiplier;
                Debug.Log($"[PVP MODE] Knockback scaled by {damageMultiplier}x (health: {currentHealth}%) = {finalKnockback}");
            }
            
            rb.AddForce(finalKnockback, ForceMode2D.Impulse);
        }

        if (damage > 0)
        {
            // Start hit flash effect
            StartCoroutine(HitFlashEffect());
            
            // Update UI
            if (cellUI != null)
            {
                cellUI.UpdateMaskColor(this); // This will update both color and damage text
                Debug.Log($"UI updated for {gameObject.name}");
            }
            else
            {
                Debug.LogError($"cellUI is null for {gameObject.name}. UI will not update.");
            }
            
            // ==================== STORY MODE: DEATH CHECK ====================
            if (isStoryMode && currentHealth >= maxHealthThreshold)
            {
                Debug.Log($"[DEATH CHECK] Health {currentHealth} >= Threshold {maxHealthThreshold} - Calling Die()");
                Die();
            }
            else if (isStoryMode)
            {
                Debug.Log($"[STORY MODE] Health {currentHealth} < Threshold {maxHealthThreshold} - Still alive");
            }
            // ==================== PVP MODE: NO DEATH ====================
            // In PvP, damage keeps accumulating for knockback scaling
            // Players are eliminated by ring-outs, not health
        }
    }
    
    // ==================== STORY MODE METHODS ====================
    // Called when player dies (story mode only)
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"[STORY MODE] {gameObject.name} has died! Health: {currentHealth}/{maxHealthThreshold}");
        
        // Disable player control
        if (playerController != null)
        {
            playerController.isControllable = false;
        }
        
        // Trigger death animation
        if (anim != null)
        {
            anim.SetTrigger("Death");
        }
        
        // Disable the player GameObject or destroy it
        StartCoroutine(DeathSequence());
    }
    
    private IEnumerator DeathSequence()
    {
        // Wait for death animation or effects
        yield return new WaitForSeconds(1f);
        
        // Despawn/destroy the player
        Debug.Log($"{gameObject.name} despawning...");
        
        // Option 1: Disable the entire player
        transform.parent.gameObject.SetActive(false);
        
        // Option 2: Destroy the player (uncomment if you want to destroy instead)
        // Destroy(transform.parent.gameObject);
        
        // Option 3: Respawn (you can implement respawn logic here)
    }
    
    // ==================== SHARED UTILITY METHODS ====================
    // Public method to set game mode
    public void SetStoryMode(bool storyMode)
    {
        isStoryMode = storyMode;
        isDead = false; // Reset death state when switching modes
        Debug.Log($"Game mode set to: {(isStoryMode ? "Story" : "PvP")} for {gameObject.name}");
    }

    // Checks if the object is grounded
    public bool IsGrounded()
    {
        if (groundCheckPoint == null)
        {
            Debug.LogError($"GroundCheckPoint is not assigned for {gameObject.name}. Ensure it is set in the inspector or via PlayerController.");
            return false;
        }

        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, whatIsGround);
    }

    // Returns whether the object is currently stunned
    public bool IsStunned()
    {
        return isStunned;
    }

    // Resets the object's health
    public void ResetHealth()
    {
        currentHealth = 0;
        if (maxHearts > 0)
        {
            maxHearts--; // Lose one heart on reset/deadzone
            Debug.Log($"Health reset to {currentHealth} for {gameObject.name}. Hearts left: {maxHearts}");
        }

        // Update the individual cell UI for damage percentage and color
        if (cellUI != null)
        {
            cellUI.UpdateMaskColor(this); // This will update both color and damage text to show 0%
            Debug.Log($"Updated UI for {gameObject.name} - Health: {currentHealth}%, Hearts: {maxHearts}");
        }
        else
        {
            Debug.LogWarning($"cellUI is null for {gameObject.name}. Cannot update damage UI.");
        }

        // Update UI via grid (for hearts)
        if (GameStarter.Instance != null && GameStarter.Instance.ingameGridUI != null)
        {
            GameStarter.Instance.ingameGridUI.UpdateAllHearts(new Dictionary<int, int>
            {
                { playerController != null && playerController.controlScheme == ControlScheme.Keyboard1 ? 0 : 1, maxHearts }
            });
        }
        else
        {
            Debug.LogError($"IngameGridUI or GameStarter instance is not assigned for {gameObject.name}.");
        }
    }

    // Flash color on hit based on health (smooth gradient: 0=bright white, 150=yellow, 300=red)
    private IEnumerator HitFlashEffect()
    {
        if (spriteRenderer != null)
        {
            // Flash color is based on health: bright white at 0, yellow at 150, red at 300
            float t = Mathf.Clamp01(currentHealth / 150f);
            Color brightWhite = new Color(1.5f, 1.5f, 1.5f); // brighter than normal white
            Color yellow = Color.yellow;
            Color red = Color.red;

            Color flashColor;
            if (t < 0.5f)
            {
                // 0 to 150: bright white to yellow
                flashColor = Color.Lerp(brightWhite, yellow, t * 2f);
            }
            else
            {
                // 150 to 300: yellow to red
                flashColor = Color.Lerp(yellow, red, (t - 0.5f) * 2f);
            }

            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;
        }
    }

    public Color GetHealthColor()
    {
        // Clamp health between 0 and 150 for color interpolation
        float t = Mathf.Clamp01(currentHealth / 150f);

        if (t < 0.5f)
        {
            // 0 to 75: white to yellow
            return Color.Lerp(Color.white, Color.yellow, t * 2f);
        }
        else
        {
            // 75 to 150: yellow to red
            return Color.Lerp(Color.yellow, Color.red, (t - 0.5f) * 2f);
        }
    }

    // ==================== CONTACT DAMAGE SYSTEM ====================
    // Handle collision with other Damageable objects
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[Damageable] OnTriggerENTER detected: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log($"[Damageable] OnTriggerSTAY detected: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        
        // Search for Damageable in multiple ways: on self, children, or parents
        Damageable otherDamageable = collision.GetComponent<Damageable>();
        Debug.Log($"[Damageable] GetComponent result: {(otherDamageable != null ? otherDamageable.gameObject.name : "NULL")}");
        
        if (otherDamageable == null)
        {
            otherDamageable = collision.GetComponentInChildren<Damageable>();
            Debug.Log($"[Damageable] GetComponentInChildren result: {(otherDamageable != null ? otherDamageable.gameObject.name : "NULL")}");
        }
        
        if (otherDamageable == null)
        {
            otherDamageable = collision.GetComponentInParent<Damageable>();
            Debug.Log($"[Damageable] GetComponentInParent result: {(otherDamageable != null ? otherDamageable.gameObject.name : "NULL")}");
        }

        if (otherDamageable == null)
        {
            Debug.Log($"[Damageable] No Damageable found anywhere on {collision.gameObject.name}");
            return;
        }

        if (otherDamageable == this)
        {
            Debug.Log($"[Damageable] Self-collision ignored");
            return;
        }

        Debug.Log($"[Damageable] Found other Damageable: {otherDamageable.gameObject.name}");

        // Check cooldown for this specific target
        if (!damageCooldown.ContainsKey(otherDamageable) || Time.time >= damageCooldown[otherDamageable])
        {
            // Calculate knockback direction away from the other character
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            Vector2 finalKnockback = new Vector2(knockbackDirection.x * contactKnockback.x, contactKnockback.y);
            
            Debug.Log($"[Damageable] APPLYING DAMAGE - Calling TakeDamage on {otherDamageable.gameObject.name}");
            Debug.Log($"[Damageable] Damage: {contactDamage}, Knockback: {finalKnockback}");
            
            // Apply damage to the other character
            otherDamageable.TakeDamage(contactDamage, finalKnockback, gameObject);
            
            // Set cooldown
            damageCooldown[otherDamageable] = Time.time + damageInterval;
        }
        else
        {
            float timeUntilNextHit = damageCooldown[otherDamageable] - Time.time;
            Debug.Log($"[Damageable] Still in cooldown with {otherDamageable.gameObject.name} - {timeUntilNextHit:F2}s remaining");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log($"[Damageable] OnTriggerEXIT detected: {collision.gameObject.name}");
    }

    // Blink the player black during i-frames
    private IEnumerator BlinkBlackCoroutine(float duration)
    {
        SpriteRenderer parentRenderer = spriteRenderer;
        if (parentRenderer == null)
        {
            parentRenderer = GetComponentInParent<SpriteRenderer>();
        }

        if (parentRenderer == null) yield break;

        // Reset to pure white first
        parentRenderer.color = Color.white;
        
        Color blackColor = Color.black;
        float blinkSpeed = 0.1f; // Speed of blink
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            // Alternate between white and black
            float blink = Mathf.Sin(elapsedTime * (Mathf.PI / blinkSpeed)) * 0.5f + 0.5f;
            parentRenderer.color = Color.Lerp(Color.white, blackColor, blink);
            yield return null;
        }

        // Restore to pure white
        parentRenderer.color = Color.white;
    }
}
