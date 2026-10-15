using System;
using System.Collections.Generic;
using UnityEngine;

// Bear-specific behavior. Unlike the bird, the bear never flies straight:
// SideLight and SideAir are only flop starters. They lunge the bear toward the
// nearest target, and the real attack (playerBear_flop) comes out the moment
// the bear touches the ground - so a side light performed off a cliff flops on
// landing below. All animator switching is done directly with anim.Play so it
// never depends on trigger transitions in the controller.
public class BearController : MonoBehaviour, ICharacterBehavior
{
    private BoxCollider2D boxCollider;
    private Vector2 originalColliderSize;
    private Rigidbody2D rb;
    private Animator anim;
    private CharacterAnimationController animController;
    private PlayerController playerController;
    private PlayerAttack playerAttack;
    private Damageable selfDamageable;

    [Header("Bear Collider Settings")]
    [SerializeField] private float wallSlideShrinkX = 1f;
    [SerializeField] private float wallSlideShrinkY = 1f;
    [SerializeField] private float jumpShrinkX = 1f;
    [SerializeField] private float jumpShrinkY = 1f;

    [Header("Flop Attack Settings")]
    [SerializeField, Tooltip("Horizontal push speed while flopping toward the target.")]
    private float flopLungeSpeed = 9f;
    [SerializeField, Tooltip("Upward force of the diagonal forward hop on side light.")]
    private float sideLightHopForce = 7f;
    [SerializeField, Tooltip("How far the bear looks for a target to flop toward.")]
    private float enemySearchRadius = 6f;
    [SerializeField, Tooltip("Layers considered when homing the flop toward a target.")]
    private LayerMask enemyLayers = ~0;

    private Dictionary<PlayerAttack.AttackType, Action> attackBehaviors;

    // Flop state: set when a starter starts, cleared when the flop comes out.
    private bool flopQueued;
    private bool flopLeftGround;
    private int flopLungeDirection = 1;

    // Timer for attacks that end by themselves (flop, neutral light/air).
    private float attackReturnTimer;
    private string currentAttackState;

    private const string IdleState = "playerBear_idle";

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        originalColliderSize = boxCollider.size;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        animController = GetComponent<CharacterAnimationController>();
        playerController = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();

        selfDamageable = GetComponentInChildren<Damageable>();

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
            { PlayerAttack.AttackType.DownLight, () => { } },
            { PlayerAttack.AttackType.NeutralAir, HandleNeutralAirAttack },
            { PlayerAttack.AttackType.SideAir, HandleSideAirAttack },
            { PlayerAttack.AttackType.DownAir, () => { } },
            { PlayerAttack.AttackType.NeutralHeavy, () => { } },
            { PlayerAttack.AttackType.SideHeavy, () => { } },
            { PlayerAttack.AttackType.DownHeavy, () => { } },
            { PlayerAttack.AttackType.Recovery, () => { } },
            { PlayerAttack.AttackType.GroundPound, () => { } }
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

    private void HandleSideLightAttack()
    {
        // Side light hops diagonally forward first; the flop comes out on landing.
        StartFlopStarter("playerBear_sideLight", sideLightHopForce);
    }

    private void HandleSideAirAttack()
    {
        StartFlopStarter("playerBear_sideAir", 4f);
    }

    private void StartFlopStarter(string starterState, float upwardForce)
    {
        flopLungeDirection = FindFlopDirection();
        FaceFlopDirection(flopLungeDirection);

        // Only the starter animation plays here; the flop waits for ground contact.
        anim.Play(starterState, 0, 0f);
        currentAttackState = starterState;

        flopQueued = true;
        flopLeftGround = playerController == null || !playerController.Grounded();
        rb.linearVelocity = new Vector2(flopLungeDirection * flopLungeSpeed, upwardForce);
    }

    private void HandleNeutralLightAttack()
    {
        PlayTimedAttack("playerBear_neutralLight");
    }

    private void HandleNeutralAirAttack()
    {
        PlayTimedAttack("playerBear_neutralAir");
    }

    // Plays an attack animation once and returns to idle when it finishes.
    private void PlayTimedAttack(string stateName)
    {
        anim.Play(stateName, 0, 0f);
        currentAttackState = stateName;
        float length = animController != null ? animController.GetAnimationLength(stateName) : 0f;
        attackReturnTimer = length > 0f ? length : 0.5f;
    }

    private void Update()
    {
        // Getting hit interrupts any ongoing attack commitment.
        if (selfDamageable != null && selfDamageable.IsLockedByKnockback)
        {
            CancelAttack();
            return;
        }

        // The flop comes out once the bear has been airborne and touches the ground.
        if (flopQueued)
        {
            bool grounded = playerController != null && playerController.Grounded();
            if (!grounded) flopLeftGround = true;
            if (grounded && flopLeftGround)
            {
                TriggerFlop();
            }
            return;
        }

        // Return to idle once a self-timed attack finishes.
        if (currentAttackState != null)
        {
            attackReturnTimer -= Time.deltaTime;
            if (attackReturnTimer <= 0f)
            {
                currentAttackState = null;
                anim.Play(IdleState, 0, 0f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!flopQueued) return;
        if (selfDamageable != null && selfDamageable.IsLockedByKnockback) return;

        // Keep pushing the bear toward the flop spot while it slides or falls.
        // Gravity is untouched so the bear still drops off ledges and lands the flop below.
        rb.linearVelocity = new Vector2(flopLungeDirection * flopLungeSpeed, rb.linearVelocity.y);
    }

    private void TriggerFlop()
    {
        flopQueued = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Slam to a stop on impact

        anim.Play("playerBear_flop", 0, 0f);
        currentAttackState = "playerBear_flop";
        float length = animController != null ? animController.GetAnimationLength("playerBear_flop") : 0f;
        attackReturnTimer = length > 0f ? length : 0.5f;
    }

    private void CancelAttack()
    {
        if (!flopQueued && currentAttackState == null) return;
        flopQueued = false;
        flopLeftGround = false;
        currentAttackState = null;

        // Jump out of the attack-tagged state so the input lock can release.
        anim.Play(IdleState, 0, 0f);
    }

    private void OnDisable()
    {
        flopQueued = false;
        flopLeftGround = false;
        currentAttackState = null;
    }

    private int FindFlopDirection()
    {
        int direction = playerController != null && playerController.isFacingRight ? 1 : -1;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemySearchRadius, enemyLayers);
        float closest = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Damageable target = hit.GetComponentInParent<Damageable>();
            if (target == null || target == selfDamageable) continue;
            if (target.transform.root == transform.root) continue;

            float dx = target.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) continue;

            if (Mathf.Abs(dx) < closest)
            {
                closest = Mathf.Abs(dx);
                direction = dx > 0f ? 1 : -1;
            }
        }

        return direction;
    }

    private void FaceFlopDirection(int direction)
    {
        bool shouldFaceRight = direction > 0;
        if (playerController != null && playerController.isFacingRight != shouldFaceRight)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            playerController.isFacingRight = shouldFaceRight;
        }
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
