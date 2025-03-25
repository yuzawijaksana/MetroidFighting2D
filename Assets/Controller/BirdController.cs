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

    [Header("Bird Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1.0f;
    [SerializeField] private float wallSlideShrinkY = 1.0f;
    [SerializeField] private float jumpShrinkX = 1.0f;
    [SerializeField] private float jumpShrinkY = 1.0f;

    private Dictionary<AttackType, System.Action> attackBehaviors;
    private AttackType lastAttackType;

    [Header("Attack Cooldown Settings")]
    [SerializeField] private float lastAttackTime = 0f;
    [SerializeField] private float sameAttackCD = 1.0f;
    [SerializeField] private float diffAttackCD = 0.5f;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        originalColliderSize = boxCollider.size;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        playerAttack = GetComponent<PlayerAttack>();
        playerController = GetComponent<PlayerController>(); // Reference PlayerController

        if (playerAttack == null)
        {
            Debug.LogError("PlayerAttack component not found on the same GameObject.");
        }

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
    }

    private void OnDisable()
    {
        PlayerAttack.OnAttackPerformed -= HandleAttack;
    }

    private void HandleAttack(AttackType attackType, float duration)
    {
        // Ensure this BirdController only responds to attacks from its linked PlayerController
        if (playerController == null || !playerController.isControllable) return;

        float currentTime = Time.time;
        float cooldown = attackType == lastAttackType ? sameAttackCD : diffAttackCD;

        if (currentTime < lastAttackTime + cooldown) return;

        if (attackBehaviors.TryGetValue(attackType, out var behavior))
        {
            GameObject hitbox = GetHitboxForAttackType(attackType);
            if (hitbox != null)
            {
                hitbox.SetActive(true);
                StartCoroutine(DeactivateHitboxAfterDuration(hitbox, duration));
            }

            behavior.Invoke();

            lastAttackTime = currentTime;
            lastAttackType = attackType;

            StartCoroutine(StartCooldownAfterAttack(cooldown));
        }
    }

    private IEnumerator StartCooldownAfterAttack(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
    }

    private IEnumerator DeactivateHitboxAfterDuration(GameObject hitbox, float duration)
    {
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false);
    }

    private GameObject GetHitboxForAttackType(AttackType attackType)
    {
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
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("NeutralLight", true);
        ResetAnimatorBool("NeutralLight", attackDuration);
    }

    private void HandleSideLightAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("SideLight", true);
        ResetAnimatorBool("SideLight", attackDuration);
    }

    private void HandleDownLightAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("DownLight", true);
        ResetAnimatorBool("DownLight", attackDuration);
    }

    private void HandleNeutralAirAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("NeutralAir", true);
        ResetAnimatorBool("NeutralAir", attackDuration);

        // No locking for NeutralAir
    }

    private void HandleSideAirAttack()
    {
        float originalGravityScale = rb.gravityScale;
        float pushForce = 7.5f;
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float attackDuration = playerAttack != null ? playerAttack.GetCurrentAttackDuration() : 0.5f;

        anim.SetBool("SideAir", true);
        rb.gravityScale = 0;
        rb.linearVelocity = pushDirection * pushForce;
        playerController.LockAttack(attackDuration);
        StartCoroutine(MaintainAttackVelocity(pushDirection * pushForce, attackDuration));

        Invoke(nameof(RevertGravityScale), attackDuration);
        ResetAnimatorBool("SideAir", attackDuration);
    }

    private IEnumerator MaintainAttackVelocity(Vector2 velocity, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            rb.linearVelocity = velocity; // Maintain the velocity
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void HandleDownAirAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("DownAir", true);
        ResetAnimatorBool("DownAir", attackDuration);
    }

    private void HandleNeutralHeavyAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("NeutralHeavy", true);
        ResetAnimatorBool("NeutralHeavy", attackDuration);
    }

    private void HandleSideHeavyAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("SideHeavy", true);
        ResetAnimatorBool("SideHeavy", attackDuration);
    }

    private void HandleDownHeavyAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("DownHeavy", true);
        ResetAnimatorBool("DownHeavy", attackDuration);
    }

    private void HandleRecoveryAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("Recovery", true);
        ResetAnimatorBool("Recovery", attackDuration);
    }

    private void HandleGroundPoundAttack()
    {
        float attackDuration = playerAttack.GetCurrentAttackDuration();

        anim.SetBool("GroundPound", true);
        ResetAnimatorBool("GroundPound", attackDuration);
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

    private void Update()
    {
        if (playerController.IsAttackLocked)
        {
            // Prevent BirdController from performing actions while attack is locked
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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
