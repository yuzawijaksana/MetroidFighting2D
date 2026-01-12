using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class Slime_Controller : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float jumpCooldown = 1.5f;
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private Vector2 contactKnockback = new Vector2(8f, 5f);
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private bool enableMovement = true;
    [SerializeField] private string targetTag = "Player";
    
    private float currentHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isGrounded;
    private bool wasGrounded;
    private float nextJumpTime;
    private bool facingRight = true;
    private bool hitWall = false;
    private float wallBounceTime;
    private bool hasJumped = false;
    private Vector2 patrolStartPos;
    private float patrolDirection = 1f;
    private Dictionary<Damageable, float> damageCooldown = new Dictionary<Damageable, float>();
    private float damageInterval = 0.5f;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Auto-find Grid and Tilemap if not assigned
        if (grid == null)
            grid = FindFirstObjectByType<Grid>();
        if (groundTilemap == null)
            groundTilemap = FindFirstObjectByType<Tilemap>();
        
        // Store starting position for patrol
        patrolStartPos = transform.position;
        
        // Find target using tag
        GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log($"Slime found target with tag '{targetTag}': {playerObject.name}");
        }
        else
        {
            Debug.LogWarning($"Slime could not find target with tag '{targetTag}'. Will keep searching...");
        }
    }
    
    // Check if there's a tile at a specific grid position
    bool HasTileAt(Vector3 worldPosition)
    {
        if (groundTilemap == null || grid == null)
            return false;
            
        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        return groundTilemap.HasTile(cellPosition);
    }
    
    // Get the grid cell position
    Vector3Int GetGridCell(Vector3 worldPosition)
    {
        if (grid == null)
            return Vector3Int.zero;
        return grid.WorldToCell(worldPosition);
    }

    void Update()
    {
        if (isDead) return;
        
        // Keep searching for target if not found yet
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
            if (playerObject != null)
            {
                player = playerObject.transform;
                Debug.Log($"Slime found target with tag '{targetTag}': {playerObject.name}");
            }
        }
        
        wasGrounded = isGrounded;
        CheckGrounded();
        
        // Reset cooldown only when landing on ground after a jump
        if (isGrounded && !wasGrounded && hasJumped)
        {
            nextJumpTime = Time.time + jumpCooldown;
            hasJumped = false;
        }
        
        // Only move if enabled
        if (enableMovement)
        {
            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                
                if (distanceToPlayer < detectionRange)
                {
                    // Debug.Log($"Slime chasing target! Distance: {distanceToPlayer:F2}");
                    HandleAI();
                }
                else
                {
                    // Debug.Log($"Slime patrolling - target out of range ({distanceToPlayer:F2} > {detectionRange})");
                    // Patrol when player is out of range
                    HandlePatrol();
                }
            }
            else
            {
                // Debug.Log("Slime patrolling - no target found");
                // Patrol when no player found
                HandlePatrol();
            }
        }
        
        UpdateAnimation();
    }

    void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);
        isGrounded = hit.collider != null;
    }

    void HandleAI()
    {
        // Face towards player when on ground
        if (isGrounded)
        {
            // Check for edge/corner ahead
            float checkDistance = 0.8f;
            float moveDirection = Mathf.Sign(player.position.x - transform.position.x);
            Vector2 edgeCheckPos = new Vector2(transform.position.x + (moveDirection * checkDistance), transform.position.y - 0.3f);
            RaycastHit2D edgeCheck = Physics2D.Raycast(edgeCheckPos, Vector2.down, 0.8f, groundLayer);
            
            // If at edge/corner, bounce back
            if (edgeCheck.collider == null)
            {
                rb.linearVelocity = new Vector2(-moveDirection * 4f, 7f);
                hasJumped = true;
                Flip();
                return;
            }
            
            if (player.position.x > transform.position.x && !facingRight)
                Flip();
            else if (player.position.x < transform.position.x && facingRight)
                Flip();

            // Jump diagonally towards player
            if (Time.time >= nextJumpTime)
            {
                // Check if path is blocked by wall/ceiling
                Vector2 checkDirection = new Vector2(moveDirection, 1).normalized;
                RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, checkDirection, 1.5f, groundLayer);
                
                // Calculate grid-based jump if tilemap is available
                if (grid != null && groundTilemap != null)
                {
                    Vector3Int currentCell = grid.WorldToCell(transform.position);
                    Vector3Int playerCell = grid.WorldToCell(player.position);
                    
                    // Calculate distance in grid cells
                    int cellsToPlayer = Mathf.Abs(playerCell.x - currentCell.x);
                    int verticalCells = playerCell.y - currentCell.y;
                    
                    // Determine jump distance in cells (1, 2, 3, or 4 cells)
                    int jumpCells = Mathf.Clamp(cellsToPlayer, 1, 4);
                    
                    // Calculate velocity needed to jump exactly that many cells
                    float cellSize = grid.cellSize.x;
                    float targetDistance = jumpCells * cellSize;
                    float adjustedMoveSpeed = targetDistance * 1.2f; // Speed to cover the distance
                    
                    // Adjust jump height based on vertical distance
                    float adjustedJumpForce = 8f + (verticalCells * 2f);
                    adjustedJumpForce = Mathf.Clamp(adjustedJumpForce, 6f, 14f);
                    
                    // If blocked, jump higher
                    if (wallCheck.collider != null && player.position.y > transform.position.y)
                    {
                        rb.linearVelocity = new Vector2(moveDirection * 2f, 12f);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector2(moveDirection * adjustedMoveSpeed, adjustedJumpForce);
                    }
                }
                else
                {
                    // Fallback to regular jump if no grid
                    float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
                    float verticalDistance = player.position.y - transform.position.y;
                    
                    float adjustedMoveSpeed = Mathf.Clamp(horizontalDistance * 0.8f, 2f, 8f);
                    float adjustedJumpForce = Mathf.Clamp(horizontalDistance * 0.6f + verticalDistance, 6f, 12f);
                    
                    rb.linearVelocity = new Vector2(moveDirection * adjustedMoveSpeed, adjustedJumpForce);
                }
                
                hasJumped = true;
                hitWall = false;
            }
        }
        // When in air, maintain the velocity set during jump
    }
    
    void HandlePatrol()
    {
        if (isGrounded)
        {
            // Calculate distance from patrol start
            float distanceFromStart = transform.position.x - patrolStartPos.x;
            
            // Use grid-based edge detection if tilemap is available
            Vector3 checkPos = new Vector3(transform.position.x + (patrolDirection * 1f), transform.position.y - 1f, 0);
            bool hasGroundAhead = HasTileAt(checkPos);
            
            // Check for wall ahead using grid
            Vector3 wallCheckPos = new Vector3(transform.position.x + (patrolDirection * 0.5f), transform.position.y, 0);
            bool hasWallAhead = HasTileAt(wallCheckPos);
            
            // Fallback to raycast if no tilemap
            if (groundTilemap == null)
            {
                float checkDistance = 0.8f;
                Vector2 edgeCheckPos = new Vector2(transform.position.x + (patrolDirection * checkDistance), transform.position.y - 0.3f);
                RaycastHit2D edgeCheck = Physics2D.Raycast(edgeCheckPos, Vector2.down, 0.8f, groundLayer);
                hasGroundAhead = edgeCheck.collider != null;
                
                RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, new Vector2(patrolDirection, 0), 0.6f, groundLayer);
                hasWallAhead = wallCheck.collider != null;
            }
            
            // Reverse direction if at edge, hit wall, or reached patrol limit
            if (!hasGroundAhead || hasWallAhead || Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                patrolDirection *= -1;
                Flip();
            }
            
            // Jump in patrol direction instead of walking
            if (Time.time >= nextJumpTime)
            {
                rb.linearVelocity = new Vector2(patrolDirection * 3f, 6f);
                hasJumped = true;
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if hit a wall/ceiling while in air
        if (!isGrounded && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Get the collision normal to determine direction
            Vector2 normal = collision.contacts[0].normal;
            
            // If hit a wall (normal is mostly horizontal) or ceiling (normal points down)
            if (Mathf.Abs(normal.x) > 0.5f || normal.y < -0.5f)
            {
                // Bounce backward
                float bounceDirection = -Mathf.Sign(rb.linearVelocity.x);
                rb.linearVelocity = new Vector2(bounceDirection * 3f, 5f);
                hitWall = true;
                wallBounceTime = Time.time;
            }
        }
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isJumping", !isGrounded);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;
        
        // Check if the collision is with a player's Damageable component
        Damageable damageable = collision.GetComponent<Damageable>();
        if (damageable != null)
        {
            // Check cooldown for this specific target
            if (!damageCooldown.ContainsKey(damageable) || Time.time >= damageCooldown[damageable])
            {
                // Calculate knockback direction
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                Vector2 finalKnockback = new Vector2(knockbackDirection.x * contactKnockback.x, contactKnockback.y);
                
                // Deal damage
                damageable.TakeDamage(contactDamage, finalKnockback, gameObject);
                
                // Set cooldown
                damageCooldown[damageable] = Time.time + damageInterval;
                
                Debug.Log($"Slime dealt {contactDamage} damage to {collision.gameObject.name}");
            }
        }
    }
    
    // Called by EnemyHurtbox child to forward trigger events
    public void OnHurtboxTrigger(Collider2D collision)
    {
        if (isDead) return;
        
        // Check if the collision is with a player's Damageable component
        Damageable damageable = collision.GetComponent<Damageable>();
        if (damageable != null)
        {
            // Check cooldown for this specific target
            if (!damageCooldown.ContainsKey(damageable) || Time.time >= damageCooldown[damageable])
            {
                // Calculate knockback direction
                Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                Vector2 finalKnockback = new Vector2(knockbackDirection.x * contactKnockback.x, contactKnockback.y);
                
                // Deal damage
                damageable.TakeDamage(contactDamage, finalKnockback, gameObject);
                
                // Set cooldown
                damageCooldown[damageable] = Time.time + damageInterval;
                
                Debug.Log($"Slime dealt {contactDamage} damage to {collision.gameObject.name}");
            }
        }
    }
    
    // Called by player attacks to damage the slime
    public void TakeDamage(float damage, Vector2 knockback, GameObject attacker)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log($"Slime took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Apply knockback using the same system as Damageable
        if (rb != null)
        {
            // Reset momentum before applying knockback (like Damageable)
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }
        
        // Reset jump cooldown so slime can jump immediately after getting hit
        nextJumpTime = Time.time;
        hasJumped = false;
        
        // Flash red or play hit animation
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
    
    private void Die()
    {
        isDead = true;
        Debug.Log("Slime died!");
        
        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Play death animation if you have one
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        // Destroy after a delay or immediately
        Destroy(gameObject, 0.5f);
    }
}
