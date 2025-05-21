using UnityEngine;
using UnityEngine.InputSystem; // Add this import for the new Input System
using System.Collections.Generic;
using static PlayerAttack;
using System.Collections;
using System.Threading.Tasks;

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

    private async void HandleAttack(AttackHitbox hitbox)
    {
        if (hitbox.originatingPlayer != gameObject || isAttackActive) return;

        if (attackBehaviors.TryGetValue(hitbox.attackType, out var behavior))
        {
            behavior.Invoke();
        }

        isAttackActive = true;
        float attackDuration = playerAttack.GetCurrentAttackDuration();
        await ResetAttackActiveAfterDuration(attackDuration + attackCooldown);
    }

    private async Task ResetAttackActiveAfterDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        isAttackActive = false;
    }

    private void HandleNeutralLightAttack()
    {
        // Implement Neutral Light Attack behavior
    }

    // Shared spin logic for attacks
    private void StartSpin(float duration, float spinSpeed, float direction)
    {
        var t = transform;
        Vector3 axis = Vector3.forward; // Always use world Z axis for proper flip
        float elapsed = 0f;

        async void Spin()
        {
            while (elapsed < duration)
            {
                float delta = Mathf.Min(Time.deltaTime, duration - elapsed);
                t.Rotate(axis, spinSpeed * delta * direction, Space.Self);
                elapsed += delta;
                await Task.Yield();
            }
            t.rotation = Quaternion.identity;
        }
        Spin();
    }

    private void HandleSideLightAttack()
    {
        // Roll forward (spin and dash)
        float dashSpeed = 7.5f;
        float dashDuration = playerAttack.GetCurrentAttackDuration();
        float spinSpeed = 1080f; // degrees per second for roll
        float elapsed = 0f;
        float direction = (playerController != null && playerController.isFacingRight) ? 1f : -1f;
        anim.SetBool("NeutralAir", true);
        Vector2 originalVelocity = rb.linearVelocity;

        // Use the same direction for both dash and spin
        StartSpin(dashDuration, spinSpeed, direction);

        async void Dash()
        {
            while (elapsed < dashDuration)
            {
                rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
            rb.linearVelocity = originalVelocity;
            anim.SetBool("NeutralAir", false);
        }
        Dash();
    }

    private void HandleDownLightAttack()
    {
        // Implement Down Light Attack behavior
    }

    private void HandleNeutralAirAttack()
    {
        anim.SetBool("NeutralAir", true);
        float spinSpeed = 2190f; // degrees per second (faster spin)
        float duration = playerAttack.GetCurrentAttackDuration();
        float direction = (playerController != null && playerController.isFacingRight) ? 1f : -1f;

        StartSpin(duration, spinSpeed, direction);

        async void EndAnim()
        {
            await Task.Delay((int)(duration * 1000));
            anim.SetBool("NeutralAir", false);
        }
        EndAnim();
    }

    private void HandleSideAirAttack()
    {
        // Implement Side Air Attack behavior
    }

    private void HandleDownAirAttack()
    {
        // Implement Down Air Attack behavior
    }

    private void HandleNeutralHeavyAttack()
    {
        // Implement Neutral Heavy Attack behavior
    }

    private void HandleSideHeavyAttack()
    {
        // Implement Side Heavy Attack behavior
    }

    private void HandleDownHeavyAttack()
    {
        // Implement Down Heavy Attack behavior
    }

    private void HandleRecoveryAttack()
    {
        // Implement Recovery Attack behavior
    }

    private void HandleGroundPoundAttack()
    {
        // Implement Ground Pound Attack behavior
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
