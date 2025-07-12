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
    private CharacterAnimationController animController;

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
        animController = GetComponent<CharacterAnimationController>();
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
    public void ShrinkCollider(float xFactor, float yFactor)
    {
        boxCollider.size = new Vector2(originalColliderSize.x * xFactor, originalColliderSize.y * yFactor);
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

    private void HandleNeutralLightAttack()
    {
        Debug.Log("Performing NeutralLight Attack");
        animController?.SetTrigger("NeutralLight");
        float animLength = animController.GetAnimationLength("playerBird_neutralLight");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    public void HandleSideLightAttack()
    {
        Debug.Log("Performing SideLight Attack");
        animController?.SetBool("Idle", false);
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        StartCoroutine(FlyingStraight("SideLight", dir));
    }

    private void HandleDownLightAttack()
    {
        Debug.Log("Performing DownLight Attack");
        animController?.SetTrigger("DownLight");
        float animLength = animController.GetAnimationLength("playerBird_downLight");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private void HandleNeutralAirAttack()
    {
        Debug.Log("Performing NeutralAir Attack");
        animController?.SetTrigger("NeutralAir");
        float animLength = animController.GetAnimationLength("playerBird_neutralAir");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private void HandleSideAirAttack()
    {
        Debug.Log("Performing SideAir Attack");
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        StartCoroutine(FlyingStraight("SideAir", dir));
    }

    private void HandleDownAirAttack()
    {
        Debug.Log("Performing DownAir Attack");
        Vector2 diagonalDown = (transform.localScale.x > 0) ? new Vector2(1, -1) : new Vector2(-1, -1);
        StartCoroutine(FlyingStraight("DownAir", diagonalDown));
    }

    private void HandleNeutralHeavyAttack()
    {
        Debug.Log("Performing NeutralHeavy Attack");
        animController?.SetTrigger("NeutralHeavy");
        float animLength = animController.GetAnimationLength("playerBird_neutralHeavy");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private void HandleSideHeavyAttack()
    {
        Debug.Log("Performing SideHeavy Attack");
        animController?.SetTrigger("SideHeavy");
        float animLength = animController.GetAnimationLength("playerBird_sideHeavy");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private void HandleDownHeavyAttack()
    {
        Debug.Log("Performing DownHeavy Attack");
        animController?.SetTrigger("DownHeavy");
        float animLength = animController.GetAnimationLength("playerBird_downHeavy");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private void HandleRecoveryAttack()
    {
        Debug.Log("Performing Recovery Attack");
        Vector2 diagonalUp = (transform.localScale.x > 0) ? new Vector2(1, 1) : new Vector2(-1, 1);
        StartCoroutine(FlyingStraight("Recovery", diagonalUp));
    }

    private void HandleGroundPoundAttack()
    {
        Debug.Log("Performing GroundPound Attack");
        animController?.SetTrigger("GroundPound");
        float animLength = animController.GetAnimationLength("playerBird_groundPound");
        StartCoroutine(ResetIdleAfterDelay(animLength));
    }

    private IEnumerator ResetIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animController?.SetBool("Idle", true);
    }

    private IEnumerator FlyingStraight(string attackAnim, Vector2 pushDirection)
    {
        anim.SetTrigger(attackAnim);
        yield return null;
        float maxWait = 2.5f;
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(attackAnim) && maxWait-- > 0)
            yield return null;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        float sign = pushDirection.x != 0 ? Mathf.Sign(pushDirection.x) : Mathf.Sign(transform.localScale.x);
        float scaleX = Mathf.Abs(transform.localScale.x) * sign;
        transform.localScale = new Vector2(scaleX, transform.localScale.y);
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
        float animationDuration = animController != null ? animController.GetAnimationLength("playerBird_" + attackAnim.ToLower()) : 0.2f;
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            rb.linearVelocity = pushDirection.normalized * 7.5f;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rb.gravityScale = birdGravityReset;
        anim.SetBool("Idle", true);
        transform.rotation = Quaternion.identity;
    }
}
