using UnityEngine;
using System.Collections.Generic;
using static PlayerAttack;
using System.Collections;
using System.Threading.Tasks;

public class BirdController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private PlayerAttack playerAttack;
    private Animator anim;
    private PlayerController playerController;

    [Header("Bird Settings")]
    [SerializeField] private float birdGravityReset = 2.5f; // Gravity scale for the bird

    [Header("Bird Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1.0f;
    [SerializeField] private float wallSlideShrinkY = 1.0f;
    [SerializeField] private float jumpShrinkX = 1.0f;
    [SerializeField] private float jumpShrinkY = 1.0f;

    [Header("Push Forces")]
    [SerializeField] private float sideLightPushForce = 8f;
    [SerializeField] private float sideAirPushForce = 7.5f;
    [SerializeField] private float downAirPushForce = 7.5f;
    [SerializeField] private float recoveryPushForce = 10f;
    [SerializeField] private float groundPoundPushForce = 10f;

    [Header("Hurtbox Settings")]
    [SerializeField] private GameObject hurtbox; // Reference to the player's hurtbox
    [SerializeField] private BoxCollider2D parentCollider; // Reference to the player's parent collider

    private Dictionary<AttackType, System.Action> attackBehaviors;
    private bool isAttackActive = false;
    private const float attackCooldown = 0.25f;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        originalColliderSize = boxCollider.size;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        playerController = GetComponent<PlayerController>();
        hurtbox = playerController.hurtbox;
        parentCollider = playerController.parentCollider.GetComponent<BoxCollider2D>();

        if (playerAttack == null)
        {
            Debug.LogError("PlayerAttack component not found on the same GameObject.");
        }

        InitializeAttackBehaviors();
    }

    private void OnEnable()
    {
        PlayerAttack.OnAttackPerformed += HandleAttack;
    }

    private void OnDisable()
    {
        PlayerAttack.OnAttackPerformed -= HandleAttack;
    }

    private void InitializeAttackBehaviors()
    {
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

    private async void HandleAttack(AttackHitbox hitbox)
    {
        if (hitbox.originatingPlayer != gameObject || isAttackActive) return;

        if (attackBehaviors.TryGetValue(hitbox.attackType, out var behavior))
        {
            behavior.Invoke();
        }

        isAttackActive = true;
        float attackDuration = playerAttack.GetCurrentAttackDuration();
        await ResetAttackActiveAfterDuration(attackDuration + 0.1f); // Add a slight delay to extend the hitbox duration
    }

    private async Task ResetAttackActiveAfterDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        isAttackActive = false;
    }

    private void HandleNeutralLightAttack()
    {
        // Leave blank
    }

    private void HandleSideLightAttack()
    {
        float originalGravityScale = rb.gravityScale;
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Walking", false);
        anim.SetBool("SideAir", true);
        rb.gravityScale = 0;

        rb.linearVelocity = new Vector2(pushDirection.x * sideLightPushForce, 0);

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(new Vector2(pushDirection.x * sideLightPushForce, 0), attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetBoolAfterDelay("SideAir", attackDuration));
    }

    private void HandleDownLightAttack()
    {
        // Leave blank
    }

    private void HandleNeutralAirAttack()
    {
        // Leave blank
    }

    private void HandleSideAirAttack()
    {
        float originalGravityScale = rb.gravityScale;
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Falling", false);
        anim.SetBool("SideAir", true);
        rb.gravityScale = 0;

        rb.linearVelocity = new Vector2(pushDirection.x * sideAirPushForce, 0);

        // Rotate the hurtbox and flip the collider by swapping its size
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(parentCollider.size.y, parentCollider.size.x); // Flip the collider size
        }

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(new Vector2(pushDirection.x * sideAirPushForce, 0), attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetHurtboxAndColliderAfterDelay(attackDuration)); // Reset hurtbox and collider
        StartCoroutine(ResetBoolAfterDelay("SideAir", attackDuration));
    }

    private void HandleDownAirAttack()
    {
        float originalGravityScale = rb.gravityScale;
        Vector2 pushDirection = new Vector2(0, -1); // Fully downward
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Falling", false);
        anim.SetBool("DownAir", true);
        rb.gravityScale = 0; // Temporarily disable gravity

        // Apply immediate downward force
        rb.linearVelocity = pushDirection * downAirPushForce;

        // Rotate the hurtbox and flip the collider by swapping its size
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(parentCollider.size.y, parentCollider.size.x); // Flip the collider size
        }

        // Rotate the sprite diagonally
        float rotationAngle = transform.localScale.x > 0 ? -45f : 45f; // Rotate based on facing direction
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(pushDirection * downAirPushForce, attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetSpriteRotationAfterDelay(attackDuration)); // Reset sprite rotation
        StartCoroutine(ResetHurtboxAndColliderAfterDelay(attackDuration)); // Reset hurtbox and collider
        StartCoroutine(ResetBoolAfterDelay("DownAir", attackDuration)); // Reset DownAir after the duration
    }

    private void HandleNeutralHeavyAttack()
    {
        // Leave blank
    }

    private void HandleSideHeavyAttack()
    {
        // Leave blank
    }

    private void HandleDownHeavyAttack()
    {
        // Leave blank
    }

    private void HandleRecoveryAttack()
    {
        float originalGravityScale = rb.gravityScale;
        Vector2 pushDirection = new Vector2(transform.localScale.x > 0 ? 1 : -1, 1).normalized; // Diagonal up
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Falling", false);
        anim.SetBool("Recovery", true); // Use Recovery animation boolean
        rb.gravityScale = 0;

        rb.linearVelocity = pushDirection * recoveryPushForce;

        // Rotate the hurtbox and flip the collider by swapping its size
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(parentCollider.size.y, parentCollider.size.x); // Flip the collider size
        }

        // Rotate the sprite diagonally
        float rotationAngle = transform.localScale.x > 0 ? 45f : -45f; // Rotate based on facing direction
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(pushDirection * recoveryPushForce, attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetSpriteRotationAfterDelay(attackDuration)); // Reset sprite rotation
        StartCoroutine(ResetHurtboxAndColliderAfterDelay(attackDuration)); // Reset hurtbox and collider
        StartCoroutine(ResetBoolAfterDelay("Recovery", attackDuration)); // Reset Recovery after the duration
    }

    private void HandleGroundPoundAttack()
    {
        float originalGravityScale = rb.gravityScale;
        Vector2 pushDirection = new Vector2(0, -1); // Fully downward
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Falling", false);
        anim.SetBool("GroundPound", true); // Use GroundPound animation boolean
        rb.gravityScale = 0; // Temporarily disable gravity

        // Apply immediate downward force
        rb.linearVelocity = new Vector2(0, -groundPoundPushForce);

        // Rotate the hurtbox and flip the collider by swapping its size
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(parentCollider.size.y, parentCollider.size.x); // Flip the collider size
        }

        // Rotate the sprite downward
        float rotationAngle = transform.localScale.x > 0 ? -90f : 90f; // Rotate based on facing direction
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        playerController.LockAttack(attackDuration);
        StartCoroutine(ResetAfterGroundPound(attackDuration));
    }

    private IEnumerator ResetAfterGroundPound(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Reset gravity scale
        rb.gravityScale = birdGravityReset;

        // Reset hurtbox and collider
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.identity;
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y);
        }

        // Reset sprite rotation
        transform.rotation = Quaternion.identity;

        // Reset animation
        anim.SetBool("GroundPound", false);
    }

    private IEnumerator MaintainAttackVelocity(Vector2 velocity, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            rb.linearVelocity = new Vector2(velocity.x, velocity.y);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ResetBoolAfterDelay(string parameterName, float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool(parameterName, false);
    }

    private IEnumerator ResetSpriteRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.rotation = Quaternion.identity; // Reset rotation to default
    }

    private IEnumerator ResetHurtboxAndColliderAfterDelay(float delay)
    {
        // Wait for the animation to finish before resetting
        yield return new WaitForSeconds(delay + 1f); // Add a slight delay to ensure animation completion

        // Reset hurtbox and parentCollider together
        if (hurtbox != null)
        {
            hurtbox.transform.rotation = Quaternion.identity; // Reset hurtbox rotation to default
        }
        if (parentCollider != null)
        {
            parentCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y); // Reset collider size
        }
    }

    private void RevertGravityScale()
    {
        rb.gravityScale = birdGravityReset;
    }

    public void ShrinkCollider(float xFactor, float yFactor)
    {
        boxCollider.size = new Vector2(originalColliderSize.x / xFactor, originalColliderSize.y / yFactor);
    }

    public void ShrinkColliderForWallSlide()
    {
        ShrinkCollider(wallSlideShrinkX, wallSlideShrinkY);
    }

    public void ShrinkColliderForJump()
    {
        ShrinkCollider(jumpShrinkX, jumpShrinkY);
    }

    public void RestoreCollider()
    {
        boxCollider.size = originalColliderSize;
    }
}
