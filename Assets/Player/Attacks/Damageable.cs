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
    private float stunDuration = 0.75f;
    private bool isStunned = false;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private PlayerController playerController;

    // Initializes references and sets up ground check
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = 0f;

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
        ApplyStun();

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
        Debug.Log($"Health reset to {currentHealth} for {gameObject.name}.");
    }

    // Sets the object's health to a specific value
    public void ResetHealthTo(float value)
    {
        currentHealth = value;
        Debug.Log($"Health reset to {currentHealth} for {gameObject.name}.");
    }
}
