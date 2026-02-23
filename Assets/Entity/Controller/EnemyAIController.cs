using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding; // Required for A* components

public enum AIDifficulty { Easy, Normal, Hard }

public class EnemyAIController : MonoBehaviour
{
    [Header("A* Integration")]
    [SerializeField] private AIPath aiPath;
    [SerializeField] private AIDestinationSetter destinationSetter;

    [Header("AI Settings")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
    [SerializeField] private float decisionInterval = 0.1f; // How often the AI can "think" in seconds
    [SerializeField] public bool isAIEnabled = true;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stoppingDistance = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;

    [Header("Sliding & Jump Settings")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private LayerMask wallInteractionLayers;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private float jumpForce = 12f; 
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float maxJumpHoldTime = 0.25f; // Used for Variable Jump Height

    [Header("AI Probabilities")]
    [SerializeField] private float jumpChance = 0.5f; 
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackLockDuration = 0.8f;
    [SerializeField] private float aggression = 1.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask whatIsGround;
    
    [Header("Area Limit")]
    public AreaEnemySpawner myHomeArea;

    // Link playerTarget to the A* Destination Setter's target
    private Transform playerTarget => destinationSetter.target;

    // References
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerAttack playerAttack;

    // State
    private bool isGrounded;
    private bool isFacingRight = true;
    
    [Header("Debug Info")]
    public int homeGraphIndex = -1; // Set by AreaEnemySpawner
    private bool isAttackLocked = false;
    public bool IsAttackLocked => isAttackLocked; // Fixed: Restored for Damageable.cs
    
    private float attackLockTimer = 0f;
    private float lastAttackTime = 0f;
    private bool wantToJump = false;
    private int jumpCount;
    private bool isWallSliding;
    private float jumpHoldTimer = 0f; // Tracks how long the AI "holds" the jump button
    private float decisionTimer = 0f;

    private enum AIState { Idle, Chasing, Attacking }
    private AIState currentState = AIState.Idle;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();

        if (aiPath == null) aiPath = GetComponent<AIPath>();
        if (destinationSetter == null) destinationSetter = GetComponent<AIDestinationSetter>();
        
        // Let A* calculate path, but we handle physics
        if (aiPath != null) aiPath.canMove = false; 

        // Register with the global EnemyManager for camera systems
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(transform);
        }
    }

    private void Update()
    {
        HandleTimers();
        isGrounded = Grounded();

        if (!isAIEnabled)
        {
            // Decelerate to a stop if AI is disabled
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.deltaTime), rb.linearVelocity.y);
            UpdateAnimations();
            return;
        }
        
        decisionTimer -= Time.deltaTime;

        UpdateAIState();
        HandleAIBehavior();
        UpdateAnimations();
    }

    private void HandleTimers()
    {
        if (isAttackLocked)
        {
            attackLockTimer -= Time.deltaTime;
            if (attackLockTimer <= 0) isAttackLocked = false;
        }
    }

    private float GetReactionTimeMultiplier()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:   return 1.5f;  // Thinks 50% slower
            case AIDifficulty.Normal: return 1.0f;
            case AIDifficulty.Hard:   return 0.75f; // Thinks 25% faster
            default: return 1.0f;
        }
    }

    private float GetCooldownMultiplier()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:   return 1.5f;  // 50% longer cooldowns
            case AIDifficulty.Normal: return 1.0f;
            case AIDifficulty.Hard:   return 0.75f; // 25% shorter cooldowns
            default: return 1.0f;
        }
    }

    private void UpdateAIState()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        
        bool playerInBounds = false;
        if (homeGraphIndex != -1 && AstarPath.active != null)
        {
            // Check if the player is on the same navigation graph as the enemy
            var playerNode = AstarPath.active.GetNearest(playerTarget.position).node;
            playerInBounds = playerNode != null && (int)playerNode.GraphIndex == homeGraphIndex;
        }
        else
        {
            playerInBounds = myHomeArea == null || myHomeArea.IsInArea(playerTarget.position);
        }

        if (currentState == AIState.Chasing && !playerInBounds)
        {
            currentState = AIState.Idle;
        }
        else if (currentState == AIState.Idle && playerInBounds && distanceToPlayer < detectionRange)
        {
            currentState = AIState.Chasing;
        }
    }

    private void HandleAIBehavior()
    {
        if (currentState == AIState.Idle)
        {
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.deltaTime), rb.linearVelocity.y);
            return;
        }

        Vector3 directionToPath = (aiPath.steeringTarget - transform.position).normalized;
        float distanceX = aiPath.steeringTarget.x - transform.position.x;

        // VARIABLE JUMP: Apply continuous force if the AI is "holding" the jump button
        if (!isGrounded && jumpHoldTimer > 0)
        {
            jumpHoldTimer -= Time.deltaTime;
            
            // If the green line STILL points UP, keep pushing up
            if (directionToPath.y > 0.2f)
            {
                rb.AddForce(Vector2.up * (jumpForce * 2.5f) * Time.deltaTime, ForceMode2D.Impulse);
            }
            else
            {
                jumpHoldTimer = 0f; // Let go of the button early if the path flattens out
            }
        }

        if (Mathf.Abs(distanceX) > stoppingDistance)
        {
            Move(directionToPath.x);
        }

        FaceTarget();

        // AI can only make a new "complex" decision after the decision timer is up.
        if (decisionTimer <= 0)
        {
            HandleActionLogic(directionToPath);
            decisionTimer = decisionInterval * GetReactionTimeMultiplier(); // Reset decision timer
        }
        
        WallSlide();
        Jump(); 
    }

    private void Move(float moveDir)
    {
        float targetSpeed = walkSpeed * (moveDir > 0 ? 1 : -1);
        float accel = isGrounded ? 25f : 15f;

        rb.linearVelocity = new Vector2(
            Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.deltaTime),
            rb.linearVelocity.y
        );
    }

    private void HandleActionLogic(Vector3 pathDir)
    {
        if (playerTarget == null) return;
        
        float verticalDist = playerTarget.position.y - transform.position.y;

        // First Jump: ONLY jump if the A* green line is pointing UP
        if ((isGrounded || isWallSliding) && pathDir.y > 0.5f)
        {
            wantToJump = true;
        }
        // Double Jump: ONLY double jump if the green line is STILL pointing UP
        else if (!isGrounded && jumpCount > 0 && rb.linearVelocity.y < 1f && pathDir.y > 0.5f)
        {
            wantToJump = true;
        }

        // Attacking
        if (distanceToPlayer() < attackRange && Time.time - lastAttackTime > (attackCooldown * GetCooldownMultiplier()))
        {
            if (PerformAIAttack(verticalDist)) lastAttackTime = Time.time;
        }
    }

    private float distanceToPlayer() => playerTarget != null ? Vector2.Distance(transform.position, playerTarget.position) : 999f;

    private void Jump()
    {
        if (isGrounded) jumpCount = maxJumps;

        if (wantToJump && !isAttackLocked)
        {
            if (isWallSliding)
            {
                float wallPushDir = isFacingRight ? -1f : 1f;
                rb.linearVelocity = Vector2.zero; 
                rb.AddForce(new Vector2(wallPushDir * walkSpeed * 1.5f, jumpForce), ForceMode2D.Impulse);
                
                jumpCount--; 
                wantToJump = false;
                anim.SetBool("Jumping", true);
                Flip(); 
                
                jumpHoldTimer = maxJumpHoldTime; // Start holding jump!
            }
            else if (jumpCount > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                
                jumpCount--;
                wantToJump = false;
                anim.SetBool("Jumping", true);
                
                jumpHoldTimer = maxJumpHoldTime; // Start holding jump!
            }
        }
    }

    private void WallSlide()
    {
        bool touchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallInteractionLayers);
        if (touchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlidingSpeed));
        }
        else isWallSliding = false;
        
        anim.SetBool("Sliding", isWallSliding);
    }

    private bool PerformAIAttack(float verticalDistance)
    {
        if (isAttackLocked || playerAttack == null) return false;
        
        float dirX = isFacingRight ? 1 : -1;
        float dirY = Mathf.Abs(verticalDistance) > 2f ? (verticalDistance > 0 ? 1 : -1) : 0;

        if (playerAttack.HandleAttack(isGrounded, dirY, dirX, true))
        {
            isAttackLocked = true;
            attackLockTimer = attackLockDuration * GetCooldownMultiplier(); 
            return true;
        }
        return false;
    }

    private void FaceTarget()
    {
        if (playerTarget == null) return;
        
        if (playerTarget.position.x > transform.position.x && !isFacingRight) Flip();
        else if (playerTarget.position.x < transform.position.x && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    public bool Grounded() => Physics2D.OverlapCircle(groundCheckPoint.position, 0.2f, whatIsGround);

    private void UpdateAnimations()
    {
        if (isGrounded) anim.SetBool("Jumping", false);
        anim.SetBool("Walking", isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        anim.SetBool("Idle", isGrounded && Mathf.Abs(rb.linearVelocity.x) < 0.1f);
        anim.SetBool("Falling", !isGrounded && rb.linearVelocity.y < -0.1f);
    }

    private void OnDestroy()
    {
        // When this enemy is destroyed (either by being killed or despawned), unregister it from the manager.
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(transform);
        }
    }
}