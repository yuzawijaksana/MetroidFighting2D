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



    private void HandleNeutralLightAttack()
    {
        Debug.Log("Neutral Light Attack executed.");
        // Add specific logic for Neutral Light Attack
    }

    public void HandleSideLightAttack()
    {
        anim.SetTrigger("SideLight"); // Trigger the SideLight animation
        StartCoroutine(SideLightAttackRoutine());
    }

    private IEnumerator SideLightAttackRoutine()
    {
        float elapsedTime = 0f;
        float animationDuration = anim.GetCurrentAnimatorStateInfo(0).length; // Use animation duration
        Vector2 pushDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        while (elapsedTime < animationDuration)
        {
            // Push the bird during the attack
            rb.linearVelocity = new Vector2(pushDirection.x * 5f, rb.linearVelocity.y);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the bird stops moving after the attack
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void HandleDownLightAttack()
    {
        Debug.Log("Down Light Attack executed.");
        // Add specific logic for Down Light Attack
    }

    private void HandleNeutralAirAttack()
    {
        Debug.Log("Neutral Air Attack executed.");
        // Add specific logic for Neutral Air Attack
    }

    private void HandleSideAirAttack()
    {
        Debug.Log("Side Air Attack executed.");
        // Add specific logic for Side Air Attack
    }

    private void HandleDownAirAttack()
    {
        Debug.Log("Down Air Attack executed.");
        // Add specific logic for Down Air Attack
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
        Debug.Log("Recovery Attack executed.");
        // Add specific logic for Recovery Attack
    }

    private void HandleGroundPoundAttack()
    {
        Debug.Log("Ground Pound Attack executed.");
        // Add specific logic for Ground Pound Attack
    }
}
