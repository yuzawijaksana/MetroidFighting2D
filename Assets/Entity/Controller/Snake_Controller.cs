using UnityEngine;

public class Snake_Controller : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeedMultiplier = 2f;

    [Header("Combat & Detection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float verticalDetectionRange = 2f;

    [Header("Environment")]
    [SerializeField] private LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool facingRight = true;
    private bool isPatrolling = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Find player in scene
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void Update()
    {
        if (player == null) return;
        
        if (CanSeePlayer())
        {
            isPatrolling = false;
            HandleChase();
        }
        else
        {
            isPatrolling = true;
            HandlePatrol();
        }
    }

    bool CanSeePlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
        bool isPlayerInFront = (facingRight && player.position.x > transform.position.x) || 
                               (!facingRight && player.position.x < transform.position.x);

        return distanceToPlayer < detectionRange && isPlayerInFront && verticalDistance < verticalDetectionRange;
    }

    void HandleChase()
    {
        // Check for wall or ledge (Air)
        Vector2 checkDir = facingRight ? Vector2.right : Vector2.left;
        
        // Check wall slightly above ground to avoid hitting floor seams
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, checkDir, 0.7f, groundLayer);
        // Check ledge slightly ahead
        RaycastHit2D ledgeHit = Physics2D.Raycast(transform.position + (Vector3)(checkDir * 0.7f), Vector2.down, 1.5f, groundLayer);

        if (wallHit.collider != null || ledgeHit.collider == null)
        {
            Flip();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            // Chase player
            float moveDirection = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed * chaseSpeedMultiplier, rb.linearVelocity.y);
        }
    }

    void HandlePatrol()
    {
        // Move forward
        float direction = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // Check for wall or ledge (Air)
        Vector2 checkDir = facingRight ? Vector2.right : Vector2.left;
        
        // Check wall slightly above ground to avoid hitting floor seams
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, checkDir, 0.7f, groundLayer);
        
        // Check ledge slightly ahead
        RaycastHit2D ledgeHit = Physics2D.Raycast(transform.position + (Vector3)(checkDir * 0.7f), Vector2.down, 1.5f, groundLayer);

        if (wallHit.collider != null || ledgeHit.collider == null)
        {
            Flip();
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
    }
}
