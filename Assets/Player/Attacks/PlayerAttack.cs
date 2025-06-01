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

    // Event triggered when an attack is performed
    public static event Action<AttackHitbox> OnAttackPerformed;

    private PlayerController playerController;
    private Animator anim; // Reference to the Animator component
    private Dictionary<Damageable, Vector2> storedKnockbacks = new Dictionary<Damageable, Vector2>();
    private Dictionary<AttackType, Action> attackBehaviors;
    private ICharacterBehavior characterBehavior;
    private Damageable selfDamageable; // Reference to the player's own Damageable component

    private void Start()
    {
        // Ensure the hitbox is hidden at the start
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }

        // Get reference to PlayerController
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController component not found on the same GameObject.");
        }

        // Get reference to Animator
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animator component not found on the same GameObject.");
        }

        // Get reference to the connected character controller
        characterBehavior = GetComponent<ICharacterBehavior>();
        if (characterBehavior == null)
        {
            Debug.LogError("ICharacterBehavior component not found on the same GameObject.");
        }

        // Get reference to the player's own Damageable component
        selfDamageable = GetComponentInChildren<Damageable>();
        if (selfDamageable == null)
        {
            Debug.LogError("Damageable component not found on the player or its children.");
        }

        InitializeAttackBehaviors();
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
        float lockEnemyActiveValue = anim.GetFloat("LockEnemy.Active");
        float attackWindowOpenValue = anim.GetFloat("Attack.Window.Open");

        bool isHitboxActive = hitboxActiveValue > 0.5f;
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
            attackHitbox.GetComponent<BoxCollider2D>().size,
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
                    // Lock enemy to the center of the attack hitbox collider
                    var hitboxCollider = attackHitbox.GetComponent<BoxCollider2D>();
                    if (hitboxCollider != null)
                    {
                        Vector3 hitboxCenter = hitboxCollider.bounds.center;
                        targetParent.position = hitboxCenter;
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
                hitbox.StartAttack(anim.GetCurrentAnimatorStateInfo(0).length); // Use animation duration
                OnAttackPerformed?.Invoke(hitbox); // Trigger the OnAttackPerformed event
            }
        }
        else
        {
            Debug.LogWarning("Hitbox is inactive. Cannot start attack.");
        }
    }

    private async Task DeactivateHitboxAfterDuration(float duration, AttackHitbox attackHitbox)
    {
        await Task.Delay((int)(duration * 1000)); // Convert seconds to milliseconds
        attackHitbox.ResetHitObjects(); // Ensure hit objects are reset
    }

    public void HandleAttack(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        AttackType attackType;

        if (isGrounded)
        {
            if (horizontalInput != 0)
            {
                attackType = isLightAttack ? AttackType.SideLight : AttackType.SideHeavy;
            }
            else if (verticalInput > 0)
            {
                attackType = isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
            else if (verticalInput < 0)
            {
                attackType = isLightAttack ? AttackType.DownLight : AttackType.DownHeavy;
            }
            else
            {
                attackType = isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
        }
        else
        {
            if (horizontalInput != 0)
            {
                attackType = isLightAttack ? AttackType.SideAir : AttackType.Recovery;
            }
            else if (verticalInput > 0)
            {
                attackType = isLightAttack ? AttackType.NeutralAir : AttackType.Recovery;
            }
            else if (verticalInput < 0)
            {
                attackType = isLightAttack ? AttackType.DownAir : AttackType.GroundPound;
            }
            else
            {
                attackType = isLightAttack ? AttackType.NeutralAir : AttackType.Recovery;
            }
        }

        // Delegate attack handling to the connected character controller
        characterBehavior?.PerformAttack(attackType);
    }
}