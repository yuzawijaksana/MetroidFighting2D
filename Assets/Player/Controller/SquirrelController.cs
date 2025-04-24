using UnityEngine;
using UnityEngine.InputSystem; // Add this import for the new Input System
using System.Collections.Generic;
using static PlayerAttack;
using System.Collections;

public class SquirrelController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private PlayerAttack playerAttack;
    private Animator anim;
    private PlayerController playerController;

    [Header("Squirrel Settings")]
    [SerializeField] private float squirrelGravityReset = 2.5f; // Gravity scale for the squirrel

    [Header("Squirrel Collider Settings")]
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

    private void HandleNeutralLightAttack()
    {
        Debug.Log("Neutral Light Attack - Not Implemented");
    }

    private void HandleSideLightAttack()
    {
        float originalGravityScale = rb.gravityScale;
        float pushForce = 8f;
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("SideLight", true);
        rb.gravityScale = 0;

        // Preserve vertical velocity while applying horizontal push force
        rb.linearVelocity = new Vector2(pushDirection.x * pushForce, rb.linearVelocity.y);

        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(new Vector2(pushDirection.x * pushForce, rb.linearVelocity.y), attackDuration));
        Invoke(nameof(RevertGravityScale), attackDuration);

        StartCoroutine(ResetBoolAfterDelay("SideLight", attackDuration));
    }

    private void HandleDownLightAttack()
    {
        Debug.Log("Down Light Attack - Not Implemented");
    }

    private void HandleNeutralAirAttack()
    {
        float spinSpeed = 2000f; // Degrees per second
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("NeutralAir", true);
        playerController.LockAttack(attackDuration);

        StartCoroutine(SpinDuringAttack(spinSpeed, attackDuration));
        StartCoroutine(ResetBoolAfterDelay("NeutralAir", attackDuration));
    }

    private IEnumerator SpinDuringAttack(float spinSpeed, float duration)
    {
        float elapsedTime = 0f;
        float adjustedSpinSpeed = transform.localScale.x > 0 ? -spinSpeed : spinSpeed;

        while (elapsedTime < duration)
        {
            transform.Rotate(0, 0, adjustedSpinSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Quaternion.identity;
    }

    private void HandleSideAirAttack()
    {
        Debug.Log("Side Air Attack - Not Implemented");
    }

    private void HandleDownAirAttack()
    {
        Debug.Log("Down Air Attack - Not Implemented");
    }

    private void HandleNeutralHeavyAttack()
    {
        Debug.Log("Neutral Heavy Attack - Not Implemented");
    }

    private void HandleSideHeavyAttack()
    {
        Debug.Log("Side Heavy Attack - Not Implemented");
    }

    private void HandleDownHeavyAttack()
    {
        Debug.Log("Down Heavy Attack - Not Implemented");
    }

    private void HandleRecoveryAttack()
    {
        Debug.Log("Recovery Attack - Not Implemented");
    }

    private void HandleGroundPoundAttack()
    {
        Debug.Log("Ground Pound Attack - Not Implemented");
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
        rb.gravityScale = squirrelGravityReset;
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
