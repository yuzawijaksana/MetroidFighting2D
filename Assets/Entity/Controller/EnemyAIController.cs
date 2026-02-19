using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Tilemaps;

public class EnemyAIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private Transform playerTarget;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float deceleration = 0.99f;
    [SerializeField] private float maxFallSpeed = -15f;

    [Header("Falling Settings")]
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float fastFallSpeed = -20f;

    [Header("Sliding Settings")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private LayerMask wallInteractionLayers;
    [SerializeField] private float wallCheckRadius = 0.2f;
    private bool isWallSliding;
    private Vector2 originalColliderSize;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10;
    [SerializeField] private GameObject jumpSmokePrefab;
    [SerializeField] private float jumpSmokeLifetime = 0.5f;
    [SerializeField] private int maxJumps = 2;
    private int jumpCount;
    private bool isDoubleJumping;

    [Header("Wall Jump Settings")]
    [SerializeField] private float wallJumpingDirection;
    [SerializeField] private float wallJumpingTime = 0.2f;
    [SerializeField] private float wallJumpingCounter;
    [SerializeField] private float wallJumpingDuration = 0.4f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(1f, 1f);
    private bool isWallJumping;

    [Header("AI Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f; // Faster attacks
    [SerializeField] private float jumpChance = 0.5f; // More aggressive jumping
    [SerializeField] private float wallSlideChance = 0.3f;
    [SerializeField] private float aerialAttackChance = 0.7f; // More aerial attacks
    [SerializeField] private float upwardAttackChance = 0.6f;
    [SerializeField] private float verticalThreshold = 2f;
    [SerializeField] private float aggression = 1.5f; // Aggression multiplier

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Attack Settings")]
    [SerializeField] private bool attackUsesRootMotion = false;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float attackSpeed = 5f;

    // References
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private PlayerAttack playerAttack;
    private ICharacterBehavior characterBehavior;

    // State Variables
    private bool isGrounded;
    private float currentXAxis;
    private bool isFacingRight = true;
    private bool isAttackLocked = false;
    public bool IsAttackLocked => isAttackLocked; // Public property for attack lock status
    private bool wasFalling = false;
    private bool isRecoveringFromHit = false;
    private float lastAttackTime = 0f;
    private float attackLockTimer = 0f; // Timer to auto-unlock attacks
    private bool wantToJump = false; // AI jump request flag

    // AI State
    private enum AIState { Idle, Chasing, Attacking, Recovering }
    private AIState currentState = AIState.Idle;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        characterBehavior = GetComponent<ICharacterBehavior>();
        originalColliderSize = boxCollider.size;

        // Initial player scan
        ScanForPlayer();
    }

    private void ScanForPlayer()
    {
        // Continuously look for the player with the "Player" tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private void Update()
    {
        // Update attack lock timer
        if (isAttackLocked)
        {
            attackLockTimer -= Time.deltaTime;
            if (attackLockTimer <= 0)
            {
                isAttackLocked = false;
                Debug.Log("[EnemyAI] Attack lock released by timer");
            }
        }

        // Always scan for player
        if (playerTarget == null)
        {
            ScanForPlayer();
        }

        if (playerTarget == null) return;

        isGrounded = Grounded();
        UpdateAIState();
        HandleAIBehavior();

        // Handle animations
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        // Apply gravity and falling mechanics
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }

        // Deceleration while moving
        if (Mathf.Abs(currentXAxis) < 0.1f && Grounded())
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
    }

    private void UpdateAIState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case AIState.Idle:
                if (distanceToPlayer < detectionRange)
                {
                    currentState = AIState.Chasing;
                }
                break;

            case AIState.Chasing:
                // Transition to attacking at longer range for more aggressive behavior
                if (distanceToPlayer < attackRange * 2.5f)
                {
                    currentState = AIState.Attacking;
                }
                else if (distanceToPlayer > detectionRange * 1.5f)
                {
                    currentState = AIState.Idle;
                }
                break;

            case AIState.Attacking:
                // Stay aggressive even if player backs away a bit
                if (distanceToPlayer > attackRange * 4f)
                {
                    currentState = AIState.Chasing;
                }
                break;

            case AIState.Recovering:
                if (!isRecoveringFromHit)
                {
                    currentState = AIState.Chasing;
                }
                break;
        }
    }

    private void HandleAIBehavior()
    {
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdle();
                break;

            case AIState.Chasing:
                HandleChasing();
                break;

            case AIState.Attacking:
                HandleAttacking();
                break;

            case AIState.Recovering:
                // Just stop moving during recovery
                currentXAxis = 0;
                break;
        }

        if (!isWallJumping)
        {
            Flip();
        }

        WallSlide();
        Jump();
        WallJump();
    }

    private void HandleIdle()
    {
        currentXAxis = 0;
        // Stop movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void HandleChasing()
    {
        float distanceToPlayer = transform.position.x - playerTarget.position.x;
        float verticalDistance = playerTarget.position.y - transform.position.y;
        
        // Determine direction toward player
        if (Mathf.Abs(distanceToPlayer) > stoppingDistance)
        {
            currentXAxis = distanceToPlayer > 0 ? -1 : 1;
            Move();
        }
        else
        {
            currentXAxis = 0;
        }

        wantToJump = false; // Reset jump request each frame
        
        // FIX: Allow jumping if grounded OR if we have jumps left (Double Jump)
        if (Grounded() || jumpCount > 0)
        {
            // Prevent instantly burning the double jump while shooting upwards
            bool canIntelligentlyJump = Grounded() || rb.linearVelocity.y < 1f;

            if (canIntelligentlyJump)
            {
                // Jump if player is higher (above the enemy)
                if (verticalDistance > 1f && Mathf.Abs(distanceToPlayer) < attackRange * 2f)
                {
                    wantToJump = true;
                }
                // Jump if there's random gap/obstacle chance
                else if (Random.value < jumpChance * Time.deltaTime)
                {
                    wantToJump = true;
                }
                // Jump attack if player is nearby
                else if (Mathf.Abs(distanceToPlayer) < attackRange * 1.5f && Mathf.Abs(verticalDistance) < 2f && Random.value < 0.3f * Time.deltaTime)
                {
                    wantToJump = true;
                }
            }
        }

        // Attack while in air if player is within range
        if (!Grounded() && !isAttackLocked && Mathf.Abs(distanceToPlayer) < attackRange * 1.2f)
        {
            if (Random.value < aerialAttackChance * Time.deltaTime)
            {
                PerformAIAttack(verticalDistance);
            }
        }

        // Attempt wall jump if on wall
        if (isWallSliding && Random.value < wallSlideChance * Time.deltaTime)
        {
            jumpCount = maxJumps;
        }
    }

    private void HandleAttacking()
    {
        float distanceToPlayer = transform.position.x - playerTarget.position.x;
        float verticalDistance = playerTarget.position.y - transform.position.y;
        
        // Keep moving toward player even during attack cooldown
        if (Mathf.Abs(distanceToPlayer) > stoppingDistance)
        {
            currentXAxis = distanceToPlayer > 0 ? -1 : 1;
        }
        else
        {
            currentXAxis = 0;
        }
        
        Move();
        
        if (!isWallJumping && currentXAxis != 0)
        {
            Flip();
        }

        // Aggressive attack pattern
        if (Time.time - lastAttackTime > attackCooldown)
        {
            if (Mathf.Abs(verticalDistance) > verticalThreshold && verticalDistance > 0)
            {
                if (PerformAIAttack(verticalDistance)) lastAttackTime = Time.time;
            }
            else
            {
                if (PerformAIAttack(verticalDistance)) lastAttackTime = Time.time;
            }
        }

        // FIX: Decoupled jumping from attack cooldown lock.
        // Uses wantToJump instead of calling PerformJump() directly to respect the flow.
        if (Grounded() || jumpCount > 0)
        {
            bool canIntelligentlyJump = Grounded() || rb.linearVelocity.y < 1f;

            if (canIntelligentlyJump && Random.value < jumpChance * aggression * Time.deltaTime)
            {
                wantToJump = true; 
            }
        }
    }

    private bool PerformAIAttack(float verticalDistance = 0)
    {
        // Prevent attacking while already locked
        if (isAttackLocked)
        {
            return false;
        }

        // Perform attack based on situation
        if (playerAttack != null)
        {
            float directionToPlayer = playerTarget.position.x - transform.position.x;
            float directionX = directionToPlayer > 0 ? 1 : -1;
            float directionY = 0;

            // Determine vertical attack direction
            if (Mathf.Abs(verticalDistance) > verticalThreshold)
            {
                if (verticalDistance > 0)
                {
                    directionY = 1; // Attack upward
                }
                else
                {
                    directionY = -1; // Attack downward
                }
            }

            // Perform the attack
            if (playerAttack.HandleAttack(isGrounded, directionY, directionX, true))
            {
                isAttackLocked = true;
                attackLockTimer = attackDuration + 0.3f; // Lock for attack duration + buffer
                Debug.Log($"[EnemyAI] Attack performed! Lock timer set to {attackLockTimer}s");
                return true;
            }
        }
        return false;
    }

    private void Move()
    {
        float targetSpeed = walkSpeed * currentXAxis;
        float acceleration = Grounded() ? 25f : 15f;

        if (currentXAxis != 0)
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, acceleration * Time.deltaTime),
                rb.linearVelocity.y
            );
        }
        else
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.deltaTime),
                rb.linearVelocity.y
            );
        }

        anim.SetBool("Walking", Grounded() && Mathf.Abs(rb.linearVelocity.x) > 0.1f);
    }

    private void Flip()
    {
        if ((currentXAxis < 0 && isFacingRight) || (currentXAxis > 0 && !isFacingRight))
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
            isFacingRight = !isFacingRight;
        }
    }

    public bool Grounded()
    {
        float groundCheckRadius = 0.2f;
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, whatIsGround);
    }

    private bool IsTouchingWall()
    {
        return Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallInteractionLayers);
    }

    private void WallSlide()
    {
        if (IsTouchingWall() && !Grounded())
        {
            isWallSliding = true;
            isWallJumping = false;
            jumpCount = 3;
            characterBehavior?.ShrinkColliderForWallSlide();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
            anim.SetBool("Sliding", isWallSliding);
        }
        else
        {
            isWallSliding = false;
            characterBehavior?.RestoreCollider();
            anim.SetBool("Sliding", isWallSliding);
        }
    }

    private void Jump()
    {
        if (isAttackLocked) return;

        if (Grounded())
        {
            jumpCount = maxJumps;
            isDoubleJumping = false;
        }

        // FIX: Only jump when requested, provided we have jumps left
        if (wantToJump && jumpCount > 0)
        {
            PerformJump();
            wantToJump = false; // Consume jump request
        }
    }

    private void PerformJump()
    {
        characterBehavior?.ShrinkColliderForJump();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        jumpCount--;

        anim.SetBool("Jumping", true);

        if (!Grounded() && jumpCount == 0)
        {
            isDoubleJumping = true;
            anim.SetBool("DoubleJumping", true);
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        // AI randomly attempts wall jump
        if (isWallSliding && wallJumpingCounter > 0f && Random.value < wallSlideChance * Time.deltaTime)
        {
            isWallJumping = true;
            SmoothWallJump();
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                transform.localScale = new Vector2(wallJumpingDirection, transform.localScale.y);
            }

            jumpCount = 1;
            isDoubleJumping = false;

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private async void SmoothWallJump()
    {
        float jumpDuration = 0.15f;
        float elapsedTime = 0f;

        Vector2 initialPosition = rb.position;
        Vector2 targetPosition = initialPosition + new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);

        float currentYVelocity = rb.linearVelocity.y;
        float targetYVelocity = wallJumpingPower.y / jumpDuration;
        float finalYVelocity = Mathf.Max(currentYVelocity, targetYVelocity);

        while (elapsedTime < jumpDuration)
        {
            float t = elapsedTime / jumpDuration;
            Vector2 lerped = Vector2.Lerp(initialPosition, targetPosition, t);
            rb.position = new Vector2(lerped.x, lerped.y);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalYVelocity);
            elapsedTime += Time.deltaTime;
            await Task.Yield();
        }

        rb.position = targetPosition;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalYVelocity);
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void UpdateAnimations()
    {
        // Update falling animation
        if (!Grounded() && rb.linearVelocity.y < 0)
        {
            anim.SetBool("Falling", true);
            anim.SetBool("Jumping", false);
            wasFalling = true;
        }
        else if (Grounded() && wasFalling)
        {
            anim.SetTrigger("Landing");
            anim.SetBool("Falling", false);
            wasFalling = false;
        }
        else
        {
            anim.SetBool("Falling", false);
            wasFalling = false;
        }

        // Update jumping animation
        anim.SetBool("Jumping", !Grounded());
        anim.SetBool("DoubleJumping", isDoubleJumping && !Grounded());

        // Update idle animation
        bool isIdle = 
            Grounded() &&
            Mathf.Abs(rb.linearVelocity.x) < 0.1f &&
            !anim.GetBool("Jumping") &&
            !anim.GetBool("Falling") &&
            !anim.GetBool("Sliding") &&
            !isAttackLocked;

        anim.SetBool("Idle", isIdle);
    }

    public void LockAttack(float duration)
    {
        isAttackLocked = true;
        UnlockAttackAfterDuration(duration);
    }

    private async void UnlockAttackAfterDuration(float duration)
    {
        await Task.Delay((int)(duration * 1000));
        isAttackLocked = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplyHitRecovery()
    {
        isRecoveringFromHit = true;
        currentState = AIState.Recovering;
        WaitForGroundAndSlide();
    }

    private async void WaitForGroundAndSlide()
    {
        while (!Grounded())
        {
            await Task.Yield();
        }

        await SlideToStop();
    }

    private async Task SlideToStop()
    {
        float slideDuration = 0.2f;
        float elapsedTime = 0f;

        Vector2 initialVelocity = rb.linearVelocity;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            rb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, elapsedTime / slideDuration);
            await Task.Yield();
        }

        rb.linearVelocity = Vector2.zero;
        isRecoveringFromHit = false;
    }

    public void HandleRespawning(bool isRespawning)
    {
        if (isRespawning && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }

    public void SetAttackRange(float range)
    {
        attackRange = range;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}