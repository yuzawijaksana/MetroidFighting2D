using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

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

        // Update mask color on start
        if (cellUI != null)
            cellUI.UpdateMaskColor(this);
    }

    private void Update()
    {
        
    }

    // Applies damage, knockback, and stun to the object
    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        if (attacker == transform.parent?.gameObject)
        {
            Debug.LogWarning("Damage from the same player ignored.");
            return;
        }

        Debug.Log($"Received {damage} damage from {attacker.name} at position {attacker.transform.position}.");

        currentHealth += damage;

        float knockbackMultiplier = 1 + (currentHealth / 100f);
        Vector2 scaledKnockback = knockback * knockbackMultiplier;

        ApplyKnockback(scaledKnockback);
        StartCoroutine(HitFlashEffect());
        StartCoroutine(ScreenShakeEffect(0.15f, 0.25f)); // Add screen shake
        ApplyStun();

        // Update mask color on the correct UI cell
        if (cellUI != null)
            cellUI.UpdateMaskColor(this);

        Debug.Log($"Player knocked back with force {scaledKnockback} at health {currentHealth}.");
    }

    // Applies knockback to the object
    public void ApplyKnockback(Vector2 knockback)
    {
        Rigidbody2D parentRb = transform.parent != null ? transform.parent.GetComponent<Rigidbody2D>() : null;
        parentRb.AddForce(knockback, ForceMode2D.Impulse);
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

    // Applies a stun effect to the object
    private async void ApplyStun()
    {
        isStunned = true;

        if (anim != null) anim.SetBool("Stunned", true);
        if (playerController != null) playerController.SetControllable(false); // Disable input

        // Calculate stun duration based on current health, capped at 1000 milliseconds
        int calculatedStunDuration = Mathf.Min((int)(currentHealth / (currentHealth * stunDuration)), 1000);
        await Task.Delay(calculatedStunDuration);

        isStunned = false;
        if (anim != null) anim.SetBool("Stunned", false);
        if (playerController != null) playerController.SetControllable(true); // Re-enable input
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
            maxHearts--; // Lose one heart on reset/deadzone
        Debug.Log($"Health reset to {currentHealth} for {gameObject.name}. Hearts left: {maxHearts}");
        if (cellUI != null)
        {
            cellUI.UpdateMaskColor(this);
            cellUI.SetHearts(maxHearts); // Update heart UI
        }
        if (maxHearts <= 0)
        {
            // Freeze the game
            Time.timeScale = 0f;
            // Remove the dead character's parent GameObject
            if (transform.parent != null)
                Destroy(transform.parent.gameObject);
        }
    }

    // Flash color on hit based on health (smooth gradient: 0=bright white, 150=yellow, 300=red)
    private IEnumerator HitFlashEffect()
    {
        if (spriteRenderer != null)
        {
            // Flash color is based on health: bright white at 0, yellow at 150, red at 300
            float t = Mathf.Clamp01(currentHealth / 300f);
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
        // Clamp health between 0 and 300 for color interpolation
        float t = Mathf.Clamp01(currentHealth / 300f);

        if (t < 0.5f)
        {
            // 0 to 150: green to yellow
            return Color.Lerp(Color.white, Color.yellow, t * 2f);
        }
        else
        {
            // 150 to 300: yellow to red
            return Color.Lerp(Color.yellow, Color.red, (t - 0.5f) * 2f);
        }
    }

    // Screen shake effect (requires Cinemachine camera)
    private IEnumerator ScreenShakeEffect(float duration, float intensity)
    {
        // Use new API: CinemachineCamera and FindFirstObjectByType
        var vcam = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            var noise = vcam.GetComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
            if (noise != null)
            {
                float originalAmplitude = noise.AmplitudeGain;
                noise.AmplitudeGain = intensity;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                noise.AmplitudeGain = originalAmplitude;
            }
        }
    }

    // Returns the color based on current health (0=green, 150=yellow, 300=red)
    
}
