using UnityEngine;
using System.Collections.Generic;
using static PlayerAttack; // Add this to reference AttackType
using System.Collections;

public class BirdController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private PlayerAttack playerAttack; // Reference to PlayerAttack
    private Animator anim;

    [Header("Bird Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1.0f; // Default to no shrink
    [SerializeField] private float wallSlideShrinkY = 1.0f; // Default to no shrink
    [SerializeField] private float jumpShrinkX = 1.0f; // Default to no shrink
    [SerializeField] private float jumpShrinkY = 1.0f; // Default to no shrink

    private Dictionary<AttackType, System.Action> attackBehaviors;
    private AttackType lastAttackType; // Track the last attack type

    

    [Header("Attack Cooldown Settings")]
    [SerializeField] private float lastAttackTime = 0f; // Track the last attack time
    [SerializeField] private float sameAttackCD = 1.0f; // Cooldown for the same attack
    [SerializeField] private float diffAttackCD = 0.5f; // Cooldown for different attacks;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        originalColliderSize = boxCollider.size;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // Initialize Animator

        // Initialize PlayerAttack reference
        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null)
        {
            Debug.LogError("PlayerAttack component not found on the same GameObject.");
        }

        // Initialize attack behaviors
        attackBehaviors = new Dictionary<AttackType, System.Action>
        {
            { AttackType.NeutralLight, HandleNeutralLightAttack },
            { AttackType.SideLight, HandleSideLightAttack },
            { AttackType.DownLight, HandleDownLightAttack },
            { AttackType.NeutralAir, HandleNeutralAirAttack },
            { AttackType.SideAir, HandleSideAirAttack },
            { AttackType.DownAir, HandleDownAirAttack },
            { AttackType.NeutralHeavy, HandleNeutralHeavyAttack },
            { AttackType.SideHeavy, HandleSideHeavyAttack },
            { AttackType.DownHeavy, HandleDownHeavyAttack },
            { AttackType.Recovery, HandleRecoveryAttack },
            { AttackType.GroundPound, HandleGroundPoundAttack }
        };
    }

    private void OnEnable()
    {
        PlayerAttack.OnAttackPerformed += HandleAttack;
        Debug.Log("BirdController subscribed to OnAttackPerformed.");
    }

    private void OnDisable()
    {
        PlayerAttack.OnAttackPerformed -= HandleAttack;
        Debug.Log("BirdController unsubscribed from OnAttackPerformed.");
    }

    private void HandleAttack(AttackType attackType, float duration)
    {
        float currentTime = Time.time;
        float cooldown = attackType == lastAttackType ? sameAttackCD : diffAttackCD;

        // Check if the attack is on cooldown
        if (currentTime < lastAttackTime + cooldown)
        {
            float remainingCooldown = (lastAttackTime + cooldown) - currentTime;
            Debug.Log($"Attack {attackType} is on cooldown for {remainingCooldown:F2} seconds ({(attackType == lastAttackType ? "same" : "different")} attack cooldown).");
            return;
        }

        if (attackBehaviors.TryGetValue(attackType, out var behavior))
        {
            // Activate the corresponding hitbox
            GameObject hitbox = GetHitboxForAttackType(attackType);
            if (hitbox != null)
            {
                hitbox.SetActive(true);
                StartCoroutine(DeactivateHitboxAfterDuration(hitbox, duration));
            }

            // Invoke the attack behavior
            behavior.Invoke();

            // Update last attack time and type
            lastAttackTime = currentTime;
            lastAttackType = attackType;

            // Start cooldown after the attack duration ends
            StartCoroutine(StartCooldownAfterAttack(cooldown));
        }
        else
        {
            Debug.LogWarning($"No behavior found for attack type: {attackType}");
        }
    }

    private IEnumerator StartCooldownAfterAttack(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
    }

    private IEnumerator DeactivateHitboxAfterDuration(GameObject hitbox, float duration)
    {
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false); // Deactivate the hitbox
    }

    private GameObject GetHitboxForAttackType(AttackType attackType)
    {
        // Map attack types to their corresponding hitboxes
        return attackType switch
        {
            AttackType.NeutralLight => playerAttack.neutralLight,
            AttackType.SideLight => playerAttack.sideLight,
            AttackType.DownLight => playerAttack.downLight,
            AttackType.NeutralAir => playerAttack.neutralAir,
            AttackType.SideAir => playerAttack.sideAir,
            AttackType.DownAir => playerAttack.downAir,
            AttackType.NeutralHeavy => playerAttack.neutralHeavy,
            AttackType.SideHeavy => playerAttack.sideHeavy,
            AttackType.DownHeavy => playerAttack.downHeavy,
            AttackType.Recovery => playerAttack.recovery,
            AttackType.GroundPound => playerAttack.groundPound,
            _ => null
        };
    }

    private void HandleNeutralLightAttack()
    {
        anim.SetBool("NeutralLight", true);
        ResetAnimatorBool("NeutralLight", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleSideLightAttack()
    {
        anim.SetBool("SideLight", true);
        ResetAnimatorBool("SideLight", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleDownLightAttack()
    {
        anim.SetBool("DownLight", true);
        ResetAnimatorBool("DownLight", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleNeutralAirAttack()
    {
        anim.SetBool("NeutralAir", true);
        ResetAnimatorBool("NeutralAir", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleSideAirAttack()
    {
        anim.SetBool("SideAir", true);

        // Temporarily set gravity scale to 0
        float originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0;

        // Push the bird forward
        float pushForce = 10f; // Adjust this value as needed
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        rb.linearVelocity = pushDirection * pushForce;

        // Use the attack duration to revert gravity scale and reset the animation bool
        float attackDuration = playerAttack != null ? playerAttack.GetCurrentAttackDuration() : 0.5f; // Default to 0.5f if duration is unavailable
        Invoke(nameof(RevertGravityScale), attackDuration);
        ResetAnimatorBool("SideAir", attackDuration);
    }

    private void HandleDownAirAttack()
    {
        anim.SetBool("DownAir", true);
        ResetAnimatorBool("DownAir", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleNeutralHeavyAttack()
    {
        anim.SetBool("NeutralHeavy", true);
        ResetAnimatorBool("NeutralHeavy", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleSideHeavyAttack()
    {
        anim.SetBool("SideHeavy", true);
        ResetAnimatorBool("SideHeavy", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleDownHeavyAttack()
    {
        anim.SetBool("DownHeavy", true);
        ResetAnimatorBool("DownHeavy", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleRecoveryAttack()
    {
        anim.SetBool("Recovery", true);
        ResetAnimatorBool("Recovery", playerAttack.GetCurrentAttackDuration());
    }

    private void HandleGroundPoundAttack()
    {
        anim.SetBool("GroundPound", true);
        ResetAnimatorBool("GroundPound", playerAttack.GetCurrentAttackDuration());
    }

    private void ResetAnimatorBool(string parameterName, float delay)
    {
        StartCoroutine(ResetBoolAfterDelay(parameterName, delay));
    }

    private IEnumerator ResetBoolAfterDelay(string parameterName, float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool(parameterName, false);
    }

    private void RevertGravityScale()
    {
        rb.gravityScale = 1;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the bird is hit by an attack hitbox
        AttackHitbox hitbox = collision.GetComponent<AttackHitbox>();
        if (hitbox != null)
        {
            HandleHit(hitbox.attackType, hitbox.damage, hitbox.knockbackForce, collision.transform.position);
        }
    }

    private void HandleHit(PlayerAttack.AttackType attackType, float damage, float knockbackForce, Vector3 attackerPosition)
    {
        Vector2 knockbackDirection = transform.position.x > attackerPosition.x ? Vector2.right : Vector2.left;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    }

    // Collider Shrinking
    public void ShrinkCollider(float xFactor, float yFactor)
    {
        boxCollider.size = new Vector2(originalColliderSize.x / xFactor, originalColliderSize.y / yFactor);
    }

    public void ShrinkColliderForWallSlide()
    {
        boxCollider.size = new Vector2(originalColliderSize.x / wallSlideShrinkX, originalColliderSize.y / wallSlideShrinkY);
    }

    public void ShrinkColliderForJump()
    {
        boxCollider.size = new Vector2(originalColliderSize.x / jumpShrinkX, originalColliderSize.y / jumpShrinkY);
    }

    public void RestoreCollider()
    {
        boxCollider.size = originalColliderSize;
    }
}
