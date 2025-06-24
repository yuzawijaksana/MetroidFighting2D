using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System;

public class BirdController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerAttack playerAttack;
    private PlayerController playerController;

    [Header("Bird Settings")]
    [SerializeField] private float birdGravityReset = 2.5f; // Gravity scale for the bird

    [Header("Bird Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1.0f;
    [SerializeField] private float wallSlideShrinkY = 1.0f;
    [SerializeField] private float jumpShrinkX = 1.0f;
    [SerializeField] private float jumpShrinkY = 1.0f;

    [Header("Side Light Attack Settings")]

    private Dictionary<PlayerAttack.AttackType, Action> attackBehaviors;

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

    private void InitializeAttackBehaviors()
    {
        attackBehaviors = new Dictionary<PlayerAttack.AttackType, Action>
        {
            { PlayerAttack.AttackType.NeutralLight, HandleNeutralLightAttack },
            { PlayerAttack.AttackType.SideLight, HandleSideLightAttack },
            { PlayerAttack.AttackType.DownLight, HandleDownLightAttack },
            { PlayerAttack.AttackType.NeutralAir, HandleNeutralAirAttack },
            { PlayerAttack.AttackType.SideAir, HandleSideAirAttack },
            { PlayerAttack.AttackType.DownAir, HandleDownAirAttack },
            { PlayerAttack.AttackType.NeutralHeavy, HandleNeutralHeavyAttack },
            { PlayerAttack.AttackType.SideHeavy, HandleSideHeavyAttack },
            { PlayerAttack.AttackType.DownHeavy, HandleDownHeavyAttack },
            { PlayerAttack.AttackType.Recovery, HandleRecoveryAttack },
            { PlayerAttack.AttackType.GroundPound, HandleGroundPoundAttack }
        };
    }

    public void PerformAttack(PlayerAttack.AttackType attackType)
    {
        if (attackBehaviors.TryGetValue(attackType, out var behavior))
        {
            behavior.Invoke();
        }
        else
        {
            Debug.LogWarning($"No behavior defined for attack type: {attackType}");
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

    private IEnumerator FlyingStraight(string attackAnim, Vector2 pushDirection)
    {
        anim.SetTrigger(attackAnim);

        // Wait until the animator transitions to the correct state
        yield return null;
        float maxWait = 10f; // Prevent infinite loop
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(attackAnim) && maxWait-- > 0)
            yield return null;

        // Lock gravity while flying straight
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        // Flip horizontally if needed (like PlayerController), including for diagonal attacks
        float sign = pushDirection.x != 0 ? Mathf.Sign(pushDirection.x) : Mathf.Sign(transform.localScale.x);
        float scaleX = Mathf.Abs(transform.localScale.x) * sign;
        transform.localScale = new Vector2(scaleX, transform.localScale.y);

        // Only rotate for diagonal (not pure left/right) attacks
        float angle = 0f;
        if (Mathf.Abs(pushDirection.x) > 0.01f && Mathf.Abs(pushDirection.y) > 0.01f)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            angle = Mathf.Atan2(pushDirection.y, Mathf.Abs(pushDirection.x)) * Mathf.Rad2Deg;
            if (facing < 0)
                angle = -angle;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        float animationDuration = anim.GetCurrentAnimatorStateInfo(0).length;
        float elapsedTime = 0f;
        Debug.Log($"Flying Attack animation duration: {animationDuration} seconds");

        while (elapsedTime < animationDuration)
        {
            rb.linearVelocity = pushDirection.normalized * 7.5f;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = birdGravityReset; // Restore gravity
        anim.SetBool("Idle", true); // Set Idle state after attack

        // Reset rotation after attack
        transform.rotation = Quaternion.identity;
    }

    private void HandleNeutralLightAttack()
    {
        anim.SetTrigger("NeutralLight");
        float animationDuration = anim.GetCurrentAnimatorStateInfo(0).length;
        float elapsedTime = 0f;
        Debug.Log($"Neutral Light Attack animation duration: {animationDuration} seconds");
        StartCoroutine(ResetToIdleAfterAnimCoroutine("NeutralLight"));
    }

    public void HandleSideLightAttack()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        StartCoroutine(FlyingStraight("SideLight", dir));
    }

    private void HandleDownLightAttack()
    {
        anim.SetTrigger("DownLight");
        float animationDuration = anim.GetCurrentAnimatorStateInfo(0).length;
        float elapsedTime = 0f;
        Debug.Log($"Down Light Attack animation duration: {animationDuration} seconds");
        StartCoroutine(ResetToIdleAfterAnimCoroutine("DownLight"));
    }

    private void HandleNeutralAirAttack()
    {
        // Just play the NeutralAir animation, do not use FlyingStraight
        anim.SetTrigger("NeutralAir");
        anim.SetBool("Jumping", false);
        anim.SetBool("Falling", false);
        StartCoroutine(ResetToIdleAfterAnimCoroutine("NeutralAir"));
    }

    // Add this coroutine to handle resetting to idle after animation
    private IEnumerator ResetToIdleAfterAnimCoroutine(string animName)
    {
        anim.SetTrigger(animName);

        // Wait until the animator transitions to the correct state
        yield return null;
        float maxWait = 10f; // Prevent infinite loop
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(animName) && maxWait-- > 0)
            yield return null;

        Debug.Log($"Current state: {anim.GetCurrentAnimatorStateInfo(0).IsName(animName)}");
        Debug.Log($"Animation duration: {anim.GetCurrentAnimatorStateInfo(0).length}");

        // Get the animation duration
        float animationDuration = anim.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log($"{animName} animation duration: {animationDuration} seconds");

        // Wait for the animation to finish
        yield return new WaitForSeconds(animationDuration);

        anim.SetBool("Idle", true); // Reset to idle
    }

    private void HandleSideAirAttack()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        anim.SetBool("Jumping", false);
        anim.SetBool("Falling", false);
        StartCoroutine(FlyingStraight("SideAir", dir));
    }

    private void HandleDownAirAttack()
    {
        Vector2 diagonalDown = (transform.localScale.x > 0) ? new Vector2(1, -1) : new Vector2(-1, -1);
        anim.SetBool("Jumping", false);
        anim.SetBool("Falling", false);
        StartCoroutine(FlyingStraight("DownAir", diagonalDown));
    }

    private void HandleNeutralHeavyAttack()
    {
        Debug.Log("Neutral Heavy Attack executed.");
        // Add specific logic for Neutral Heavy Attack
    }

    private void HandleSideHeavyAttack()
    {
        Debug.Log("Side Heavy Attack executed.");
        // Add specific logic for Side Heavy Attack
    }

    private void HandleDownHeavyAttack()
    {
        Debug.Log("Down Heavy Attack executed.");
        // Add specific logic for Down Heavy Attack
    }

    private void HandleRecoveryAttack()
    {
        // Diagonal up (left or right)
        Vector2 diagonalUp = (transform.localScale.x > 0) ? new Vector2(1, 1) : new Vector2(-1, 1);
        StartCoroutine(FlyingStraight("Recovery", diagonalUp));
    }

    private void HandleGroundPoundAttack()
    {
        Debug.Log("Ground Pound Attack executed.");
        // Add specific logic for Ground Pound Attack
    }
}
