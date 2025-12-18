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
        
    }

    // ==================== CORE DAMAGE SYSTEM ====================
    // Applies damage, knockback, and stun to the object
    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        if (isDead) return; // Story mode only
        
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
}
