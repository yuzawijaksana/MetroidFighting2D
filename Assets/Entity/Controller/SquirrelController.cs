using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class SquirrelController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerAttack playerAttack;
    private PlayerController playerController;

    [Header("Squirrel Settings")]
    [SerializeField] private float squirrelGravityReset = 2.5f; // Gravity scale for the squirrel

    [Header("Squirrel Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1.0f;
    [SerializeField] private float wallSlideShrinkY = 1.0f;
    [SerializeField] private float jumpShrinkX = 1.0f;
    [SerializeField] private float jumpShrinkY = 1.0f;

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

    private void HandleNeutralLightAttack()
    {
        // Blank handler for Neutral Light Attack
    }

    private void HandleSideLightAttack()
    {
        // Blank handler for Side Light Attack
    }

    private void HandleDownLightAttack()
    {
        // Blank handler for Down Light Attack
    }

    private void HandleNeutralAirAttack()
    {
        // Blank handler for Neutral Air Attack
    }

    private void HandleSideAirAttack()
    {
        // Blank handler for Side Air Attack
    }

    private void HandleDownAirAttack()
    {
        // Blank handler for Down Air Attack
    }

    private void HandleNeutralHeavyAttack()
    {
        // Blank handler for Neutral Heavy Attack
    }

    private void HandleSideHeavyAttack()
    {
        // Blank handler for Side Heavy Attack
    }

    private void HandleDownHeavyAttack()
    {
        // Blank handler for Down Heavy Attack
    }

    private void HandleRecoveryAttack()
    {
        // Blank handler for Recovery Attack
    }

    private void HandleGroundPoundAttack()
    {
        // Blank handler for Ground Pound Attack
    }
}
