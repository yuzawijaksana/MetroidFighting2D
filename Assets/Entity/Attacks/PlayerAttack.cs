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

    public GameObject attackHitbox;
    public GameObject knockbackObject;

    public static event Action<AttackHitbox> OnAttackPerformed;

    private PlayerController playerController;
    private Animator anim;
    private Dictionary<Damageable, Vector2> storedKnockbacks = new Dictionary<Damageable, Vector2>();
    private Dictionary<AttackType, Action> attackBehaviors;
    private ICharacterBehavior characterBehavior;
    private Damageable selfDamageable;

    [Header("Attack Cooldowns")]
    public float sameAttackCooldown = 0.75f;
    public float differentAttackCooldown = 0.25f;
    private AttackType lastPerformedAttackType;
    private float lastAttackExecutionTime = -1f;

    private void Start()
    {
        attackHitbox.SetActive(false);
        knockbackObject.SetActive(false);
        playerController = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
        characterBehavior = GetComponent<ICharacterBehavior>();
        selfDamageable = GetComponentInChildren<Damageable>();
        InitializeAttackBehaviors();
        lastAttackExecutionTime = -Mathf.Max(sameAttackCooldown, differentAttackCooldown) - 1f;
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
        float hitboxActiveValue = anim.GetFloat("Hitbox.Active");
        float knockbackActiveValue = anim.GetFloat("Knockback.Active");
        float lockEnemyActiveValue = anim.GetFloat("LockEnemy.Active");
        float attackWindowOpenValue = anim.GetFloat("Attack.Window.Open");
        bool isHitboxActive = hitboxActiveValue > 0.5f;
        bool isKnockbackActive = knockbackActiveValue > 0.5f;
        bool isLockEnemyActive = lockEnemyActiveValue > 0.5f;
        bool isAttackWindowOpen = attackWindowOpenValue > 0.5f;
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
        if (knockbackObject != null)
        {
            knockbackObject.SetActive(isKnockbackActive);
        }
        if (!isAttackWindowOpen)
        {
        }
    }

    private void HandleHitboxLogic(bool isLockEnemyActive)
    {
        if (attackHitbox == null) return;
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(
            attackHitbox.transform.position,
            attackHitbox.GetComponent<CapsuleCollider2D>().size,
            0
        );
        foreach (Collider2D collider in hitObjects)
        {
            Damageable target = collider.GetComponent<Damageable>();
            if (target != null && target != selfDamageable)
            {
                Transform targetParent = target.transform.parent;
                if (targetParent == null)
                {
                    continue;
                }
                if (isLockEnemyActive)
                {
                    var hitboxCollider = attackHitbox.GetComponent<CapsuleCollider2D>();
                    if (hitboxCollider != null)
                    {
                        Vector3 hitboxCenter = hitboxCollider.bounds.center;
                        float suckSpeed = 15f;
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
        if (attackHitbox != null && attackHitbox.activeInHierarchy)
        {
            AttackHitbox hitbox = attackHitbox.GetComponent<AttackHitbox>();
            if (hitbox != null)
            {
                hitbox.Initialize(gameObject);
                OnAttackPerformed?.Invoke(hitbox);
            }
        }
        else
        {
            Debug.LogWarning("Hitbox is inactive. Cannot start attack.");
        }
    }

    private AttackType DetermineAttackType(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        if (isGrounded)
        {
            if (verticalInput > 0)
            {
                return isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
            else if (verticalInput < 0)
            {
                return isLightAttack ? AttackType.DownLight : AttackType.DownHeavy;
            }
            else if (horizontalInput != 0)
            {
                return isLightAttack ? AttackType.SideLight : AttackType.SideHeavy;
            }
            else
            {
                return isLightAttack ? AttackType.NeutralLight : AttackType.NeutralHeavy;
            }
        }
        else
        {
            if (verticalInput > 0)
            {
                return isLightAttack ? AttackType.NeutralAir : AttackType.Recovery;
            }
            else if (verticalInput < 0)
            {
                return isLightAttack ? AttackType.DownAir : AttackType.GroundPound;
            }
            else if (horizontalInput != 0)
            {
                return isLightAttack ? AttackType.SideAir : AttackType.Recovery;
            }
            else
            {
                return isLightAttack ? AttackType.NeutralAir : AttackType.Recovery;
            }
        }
    }

    public bool HandleAttack(bool isGrounded, float verticalInput, float horizontalInput, bool isLightAttack)
    {
        AttackType currentAttackType = DetermineAttackType(isGrounded, verticalInput, horizontalInput, isLightAttack);
        float requiredCooldown;
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
            return false;
        }
        characterBehavior?.PerformAttack(currentAttackType);
        lastPerformedAttackType = currentAttackType;
        lastAttackExecutionTime = Time.time;
        return true;
    }
}