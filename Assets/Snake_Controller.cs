using UnityEngine;

public class Snake_Controller : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 patrolDistance = new Vector2(5f, 0f);
    
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isGrounded;
    private float nextAttackTime;
    private bool facingRight = true;
    private Vector2 patrolTarget;
    private bool isPatrolling = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set initial patrol target
        patrolTarget = new Vector2(transform.position.x + patrolDistance.x, transform.position.y);
        
        // Find player in scene
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void Update()
    {
        CheckGrounded();
        
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer < detectionRange)
            {
                HandleChase();
                isPatrolling = false;
            }
            else
            {
                isPatrolling = true;
            }
        }
        
        if (isPatrolling)
            HandlePatrol();
        
        UpdateAnimation();
    }

    void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);
        isGrounded = hit.collider != null;
    }

    void HandleChase()
    {
        // Face towards player
        if (player.position.x > transform.position.x && !facingRight)
            Flip();
        else if (player.position.x < transform.position.x && facingRight)
            Flip();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Attack if in range
        if (distanceToPlayer < attackRange && Time.time >= nextAttackTime)
        {
            animator.SetTrigger("Attack");
            nextAttackTime = Time.time + attackCooldown;
        }
        else if (distanceToPlayer > attackRange)
        {
            // Chase player
            float moveDirection = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Stop moving if in attack range but can't attack yet
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void HandlePatrol()
    {
        // Check if reached patrol target
        if (Mathf.Abs(transform.position.x - patrolTarget.x) < 0.5f)
        {
            patrolTarget.x = transform.position.x - patrolDistance.x;
            Flip();
        }

        // Move towards patrol target
        float moveDirection = Mathf.Sign(patrolTarget.x - transform.position.x);
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed * 0.6f, rb.linearVelocity.y);
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", rb.linearVelocity.x != 0);
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !facingRight;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
