using UnityEngine;
using System.Collections.Generic;
using static PlayerAttack;
using System.Collections;

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
            { AttackType.NeutralLight, () => HandleAttackAnimation("NeutralLight") },
            { AttackType.SideLight, () => HandleAttackAnimation("SideLight") },
            { AttackType.DownLight, () => HandleAttackAnimation("DownLight") },
            { AttackType.NeutralAir, () => HandleAttackAnimation("NeutralAir") },
            { AttackType.SideAir, HandleSideAirAttack },
            { AttackType.DownAir, () => HandleAttackAnimation("DownAir") },
            { AttackType.NeutralHeavy, () => HandleAttackAnimation("NeutralHeavy") },
            { AttackType.SideHeavy, () => HandleAttackAnimation("SideHeavy") },
            { AttackType.DownHeavy, () => HandleAttackAnimation("DownHeavy") },
            { AttackType.Recovery, () => HandleAttackAnimation("Recovery") },
            { AttackType.GroundPound, () => HandleAttackAnimation("GroundPound") }
        };
    }

    private void HandleAttack(AttackHitbox hitbox)
    {
        if (hitbox.originatingPlayer != gameObject || isAttackActive) return;

        if (attackBehaviors.TryGetValue(hitbox.attackType, out var behavior))
        {
            behavior.Invoke();
        }

        isAttackActive = true;
        float attackDuration = playerAttack.GetCurrentAttackDuration();
        StartCoroutine(ResetAttackActiveAfterDuration(attackDuration + attackCooldown));
    }

    private IEnumerator ResetAttackActiveAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttackActive = false;
    }

    private void HandleAttackAnimation(string animationName)
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();
        anim.SetBool(animationName, true);
        StartCoroutine(ResetBoolAfterDelay(animationName, attackDuration));
    }

    private void HandleSideAirAttack()
    {
        float originalGravityScale = rb.gravityScale;
        float pushForce = 7.5f;
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("SideAir", true);
        rb.gravityScale = 0;

        // Preserve vertical velocity while applying horizontal push force
        rb.linearVelocity = new Vector2(pushDirection.x * pushForce, rb.linearVelocity.y);

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(new Vector2(pushDirection.x * pushForce, rb.linearVelocity.y), attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetBoolAfterDelay("SideAir", attackDuration));
    }

    private IEnumerator MaintainAttackVelocity(Vector2 velocity, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            rb.linearVelocity = velocity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ResetBoolAfterDelay(string parameterName, float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool(parameterName, false);
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
