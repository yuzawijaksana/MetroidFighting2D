using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic; // Add this namespace for Dictionary

public class Damageable : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer; // Reference to parent SpriteRenderer

    [Header("Health Settings")]
    [SerializeField] public float currentHealth;
    [SerializeField] public int maxHearts = 3; // Add this for max health/lives

    [Header("Stun Settings")]
    private float stunDuration = 0.75f;
    private bool isStunned = false;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private PlayerController playerController;

    public CharacterIngameCellUI cellUI; // Assign after spawn

    private Material flashMaterial;
    private Material originalMaterial;
    private SpriteRenderer sr;

    // Initializes references and sets up ground check
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

    // Applies damage, knockback, and stun to the object
    private float lastHitTime = -1f;
    private float hitCooldown = 0.05f; // Minimum time between knockbacks (seconds)

    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        // Prevent double application in the same frame or from spamming
        if (Time.time - lastHitTime < hitCooldown)
            return;
        lastHitTime = Time.time;

        Debug.Log($"Applying hit effect: Damage={damage}, Knockback={knockback}, Attacker={attacker?.name}");

        if (attacker == transform.parent?.gameObject) return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            // Reset momentum before applying new knockback
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }

        if (damage > 0)
        {
            currentHealth += damage;
            Debug.Log($"Health updated to {currentHealth} for {gameObject.name}");
            
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
        }
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
