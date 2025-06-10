using System.Collections;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    public enum AttackType
    {
        NeutralLight,
        SideLight,
        DownLight,
        NeutralAir,
        SideAir,
        DownAir,
        NeutralHeavy,
        SideHeavy,
        DownHeavy,
        Recovery,
        GroundPound
    }

    // Single hitbox reference
    public GameObject attackHitbox;
    public GameObject knockbackObject;

    // Event triggered when an attack is performed
    public static event Action<AttackHitbox> OnAttackPerformed;

    private PlayerController playerController;
    private Animator anim; // Reference to the Animator component
    private Dictionary<Damageable, Vector2> storedKnockbacks = new Dictionary<Damageable, Vector2>();
    private Dictionary<AttackType, Action> attackBehaviors;
    private ICharacterBehavior characterBehavior;
    private Damageable selfDamageable; // Reference to the player's own Damageable component

    // Cooldown fields
    [Header("Attack Cooldowns")]
    public float sameAttackCooldown = 0.75f;
    public float differentAttackCooldown = 0.25f;
    private AttackType lastPerformedAttackType;
    private float lastAttackExecutionTime = -1f; // Initialize to allow the first attack

    private void Start()
    {
        
        attackHitbox.SetActive(false);
        knockbackObject.SetActive(false);

        // Get reference to PlayerController
        playerController = GetComponent<PlayerController>();
        // Get reference to Animator
        anim = GetComponent<Animator>();
        // Get reference to the connected character controller
        characterBehavior = GetComponent<ICharacterBehavior>();
        // Get reference to the player's own Damageable component
        selfDamageable = GetComponentInChildren<Damageable>();
        InitializeAttackBehaviors();

        // Initialize lastAttackExecutionTime to a value that ensures the first attack is always allowed.
        // A very small negative number or -float.MaxValue would also work.
        lastAttackExecutionTime = -Mathf.Max(sameAttackCooldown, differentAttackCooldown) - 1f; 
        // Initialize lastPerformedAttackType to a value that won't match any actual attack type initially,
        // or rely on the first attack always using differentAttackCooldown logic due to time check.
        // For simplicity, we'll let the time check handle the first attack.
    }

    private void InitializeAttackBehaviors()
    {
        attackBehaviors = new Dictionary<AttackType, Action>
        {
            { AttackType.NeutralLight, () => characterBehavior?.PerformAttack(AttackType.NeutralLight) },
            { AttackType.SideLight, () => characterBehavior?.PerformAttack(AttackType.SideLight) },
            { AttackType.DownLight, () => characterBehavior?.PerformAttack(AttackType.DownLight) },
            { AttackType.NeutralAir, () => characterBehavior?.PerformAttack(AttackType.NeutralAir) },
            { AttackType.SideAir, () => characterBehavior?.PerformAttack(AttackType.SideAir) },
            { AttackType.DownAir, () => characterBehavior?.PerformAttack(AttackType.DownAir) },
            { AttackType.NeutralHeavy, () => characterBehavior?.PerformAttack(AttackType.NeutralHeavy) },
            { AttackType.SideHeavy, () => characterBehavior?.PerformAttack(AttackType.SideHeavy) },
            { AttackType.DownHeavy, () => characterBehavior?.PerformAttack(AttackType.DownHeavy) },
            { AttackType.Recovery, () => characterBehavior?.PerformAttack(AttackType.Recovery) },
            { AttackType.GroundPound, () => characterBehavior?.PerformAttack(AttackType.GroundPound) }
        };
    }

    private void Update()
    {
        if (anim == null) return;

        // Check animator parameters
        float hitboxActiveValue = anim.GetFloat("Hitbox.Active");
        float knockbackActiveValue = anim.GetFloat("Knockback.Active");
        float lockEnemyActiveValue = anim.GetFloat("LockEnemy.Active");
        float attackWindowOpenValue = anim.GetFloat("Attack.Window.Open");

        bool isHitboxActive = hitboxActiveValue > 0.5f;
        bool isKnockbackActive = knockbackActiveValue > 0.5f;
        bool isLockEnemyActive = lockEnemyActiveValue > 0.5f;
        bool isAttackWindowOpen = attackWindowOpenValue > 0.5f;

        // Activate or deactivate the hitbox
        if (isHitboxActive)
        {
            if (attackHitbox != null && !attackHitbox.activeInHierarchy)
            {
                attackHitbox.SetActive(true);
            }
            HandleHitboxLogic(isLockEnemyActive);
        }
        else
        {
            if (attackHitbox != null && attackHitbox.activeInHierarchy)
            {
                attackHitbox.SetActive(false);
            }
        }

        // Activate or deactivate the knockback object
        if (knockbackObject != null)
        {
            knockbackObject.SetActive(isKnockbackActive);
        }

        if (!isAttackWindowOpen)
        {
            // Attack window closed logic
        }
    }

    private void HandleHitboxLogic(bool isLockEnemyActive)
    {
        if (attackHitbox == null) return;

        // Perform hitbox-related operations
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(
            attackHitbox.transform.position,
            attackHitbox.GetComponent<CapsuleCollider2D>().size,
            0
        );

        foreach (Collider2D collider in hitObjects)
        {
            Damageable target = collider.GetComponent<Damageable>();
            if (target != null && target != selfDamageable) // Ensure target is not self
            {
                Transform targetParent = target.transform.parent; // Move the parent of the Damageable
                if (targetParent == null)
                {
                    continue;
                }

                if (isLockEnemyActive)
                {
                    // Suck enemy in slowly toward the center of the attack hitbox collider
                    var hitboxCollider = attackHitbox.GetComponent<CapsuleCollider2D>();
                    if (hitboxCollider != null)
                    {
                        Vector3 hitboxCenter = hitboxCollider.bounds.center;
                        float suckSpeed = 15f; // Units per second
                        // Move the target parent towards the hitbox center smoothly
                        targetParent.position = Vector3.MoveTowards(
                            targetParent.position,
                            hitboxCenter,
                            suckSpeed * Time.deltaTime
                        );
                    }
                }
            }
        }
    }

    public void PerformAttack(AttackType attackType)
    {
        if (attackHitbox != null && attackHitbox.activeInHierarchy) // Ensure hitbox is active
        {
            AttackHitbox hitbox = attackHitbox.GetComponent<AttackHitbox>();
            if (hitbox != null)
            {
                hitbox.Initialize(gameObject); // Set the originating player
                OnAttackPerformed?.Invoke(hitbox); // Trigger the OnAttackPerformed event
            }
        }
        else
        {
            Debug.LogWarning("Hitbox is inactive. Cannot start attack.");
        }
    }

    // Helper method to determine attack type based on input
    private AttackType DetermineAttackType(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        if (isGrounded)
        {
            if (verticalInput > 0) // Up
            {
                return isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
            else if (verticalInput < 0) // Down
            {
                return isLightAttack ? AttackType.DownLight : AttackType.DownHeavy;
            }
            else if (horizontalInput != 0) // Side
            {
                return isLightAttack ? AttackType.SideLight : AttackType.SideHeavy;
            }
            else // Neutral
            {
                return isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
        }
        else // Aerial
        {
            if (verticalInput > 0) // Up
            {
                // For aerial, light up is NeutralAir, heavy up is Recovery
                return isLightAttack ? AttackType.NeutralAir : AttackType.Recovery;
            }
            else if (verticalInput < 0) // Down
            {
                return isLightAttack ? AttackType.DownAir : AttackType.GroundPound;
            }
            else if (horizontalInput != 0) // Side
            {
                // For aerial, light side is SideAir, heavy side is also Recovery (as per common fighting game tropes or can be specific)
                // Assuming heavy side air is also a recovery type move or a distinct SideHeavyAir if you add it.
                // Using Recovery for heavy side air for now.
                return isLightAttack ? AttackType.SideAir : AttackType.Recovery;
            }
            else // Neutral
            {
                return isLightAttack ? AttackType.NeutralAir : AttackType.Recovery; // Heavy neutral air often a recovery/special
            }
        }
    }

    public bool HandleAttack(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        AttackType currentAttackType = DetermineAttackType(isGrounded, verticalInput, horizontalInput, isLightAttack);

        float requiredCooldown;
        // Check if it's the very first attack or if lastPerformedAttackType is uninitialized (e.g. if we had an AttackType.None)
        // The initial value of lastAttackExecutionTime handles the first attack.
        if (currentAttackType == lastPerformedAttackType)
        {
            requiredCooldown = sameAttackCooldown;
        }
        else
        {
            requiredCooldown = differentAttackCooldown;
        }

        if (Time.time < lastAttackExecutionTime + requiredCooldown)
        {
            // Optionally, provide feedback that the attack is on cooldown
            // Debug.Log($"Attack {currentAttackType} on cooldown. Time left: {(lastAttackExecutionTime + requiredCooldown) - Time.time:F2}s");
            return false; // Attack is on cooldown
        }

        // Delegate attack handling to the connected character controller
        characterBehavior?.PerformAttack(currentAttackType);

        // Update last attack info
        lastPerformedAttackType = currentAttackType;
        lastAttackExecutionTime = Time.time;
        
        // Debug.Log($"Performed {currentAttackType}. Cooldown for same: {sameAttackCooldown}s, for different: {differentAttackCooldown}s.");
        return true; // Attack performed
    }
}