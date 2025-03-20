using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private PlayerAttack playerAttack;
    private ICharacterBehavior characterBehavior;

    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float deceleration = 0.95f;
    [SerializeField] private float fallingSpeed = 10f;
    [SerializeField] private float maxFallSpeed = -20f;

    [Header("Sliding Settings")]
    private bool isWallSliding;
    [SerializeField] private float wallSlidingSpeed = 2f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private Vector2 originalColliderSize;

    [Header("Wall Jumping Settings")]
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(4f, 8f);

    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 45;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    private bool isDashing;

    [Header("Double Jump Settings")]
    [SerializeField] private int maxJumps = 2;
    private int jumpCount;
    private bool isDoubleJumping;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionFactor = 0.5f;
    private bool isSlowMotionActive = false;

    
    [Header("Key Bindings")]
    public KeyCode lightAttackKey = KeyCode.J;
    public KeyCode heavyAttackKey = KeyCode.K;
    public KeyCode dashKey = KeyCode.L;


    private bool isGrounded;
    private float xAxis;
    private bool isFacingRight = true;
    private bool isAttacking = false; // Flag to track if an attack is being performed

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        characterBehavior = GetComponent<ICharacterBehavior>(); // Dynamically link to character behavior
        originalColliderSize = boxCollider.size;
    }

    private void Update()
    {
        GetInputs();
        if (!isDashing)
        {
            Move();
            WallSlide();
            Jump();
            WallJump();
            if (!isWallJumping) Flip();
            Flip();
        }
        HandleDash();

        if (!Grounded())
        {
            characterBehavior?.ShrinkColliderForJump(); // Shrink horizontally when not grounded
        }
        else
        {
            characterBehavior?.RestoreCollider(); // Restore collider when grounded
        }

        if (isWallSliding)
        {
            characterBehavior?.ShrinkColliderForWallSlide(); // Shrink vertically during wall sliding
        }

        if (Input.GetKeyDown(KeyCode.BackQuote)) ToggleSlowMotion();
        if (Input.GetKeyDown(KeyCode.R)) ResetPosition();

        isGrounded = Grounded();

        // Handle attack input
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(lightAttackKey))
        {
            isAttacking = true; // Set attacking flag
            playerAttack.HandleAttack(isGrounded, vertical, horizontal, true); // Light attack
            StartCoroutine(ResetAttackFlag(playerAttack.GetCurrentAttackDuration()));
        }
        else if (Input.GetKeyDown(heavyAttackKey))
        {
            isAttacking = true; // Set attacking flag
            playerAttack.HandleAttack(isGrounded, vertical, horizontal, false); // Heavy attack
            StartCoroutine(ResetAttackFlag(playerAttack.GetCurrentAttackDuration()));
        }
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }

    private void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    private void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            isFacingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            isFacingRight = true;
        }
    }

    private void Move()
    {
        if (xAxis != 0)
        {
            float targetSpeed = walkSpeed * xAxis;
            float acceleration = 10f;
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, targetSpeed, Time.deltaTime * acceleration), rb.linearVelocity.y);
            anim.SetBool("Walking", true);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * deceleration, rb.linearVelocity.y);
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                anim.SetBool("Walking", false);
            }
        }

        anim.SetBool("Walking", Mathf.Abs(rb.linearVelocity.x) > 0.1f && Grounded());
    }

    public bool Grounded()
    {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround) ||
               Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) ||
               Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround);
    }

    private bool isWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (isWalled() && !Grounded())
        {
            isWallSliding = true;
            characterBehavior?.ShrinkColliderForWallSlide(); // Shrink collider for wall sliding
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
            anim.SetBool("Sliding", isWallSliding);
        }
        else
        {
            isWallSliding = false;
            characterBehavior?.RestoreCollider(); // Restore collider when not wall sliding
            anim.SetBool("Sliding", isWallSliding);
        }
    }

    private void Jump()
    {
        if (isAttacking) return; // Prevent jumping during attacks

        if (Grounded())
        {
            jumpCount = 0;
            isDoubleJumping = false;
        }

        if (Input.GetButtonDown("Jump") && (Grounded() || jumpCount < maxJumps || isDashing))
        {
            characterBehavior?.ShrinkColliderForJump(); // Shrink collider for jumping
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpCount++;

            if (!Grounded() && jumpCount == 2)
            {
                isDoubleJumping = true;
                anim.SetBool("DoubleJumping", true);
            }
            else
            {
                anim.SetBool("Jumping", true);
            }

            if (isDashing) isDashing = false;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        if (Input.GetKeyDown(KeyCode.S) && !Grounded())
        {
            rb.AddForce(new Vector2(0, -fallingSpeed), ForceMode2D.Impulse);
        }

        anim.SetBool("Jumping", !Grounded());
        anim.SetBool("DoubleJumping", isDoubleJumping && !Grounded());
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
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

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            StartCoroutine(SmoothWallJump());
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

    private IEnumerator SmoothWallJump()
    {
        float jumpDuration = 0.05f;
        float elapsedTime = 0f;

        Vector2 initialVelocity = rb.linearVelocity;
        Vector2 targetVelocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);

        while (elapsedTime < jumpDuration)
        {
            rb.linearVelocity = targetVelocity;
            elapsedTime += Time.deltaTime;

            if (transform.localScale.x != wallJumpingDirection)
            {
                transform.localScale = new Vector2(wallJumpingDirection, transform.localScale.y);
            }

            yield return null;
        }

        rb.linearVelocity = targetVelocity;
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(dashKey) && !isDashing && Grounded())
        {
            Dash();
        }
    }

    private void Dash()
    {
        isDashing = true;
        rb.AddForce(new Vector2(isFacingRight ? dashSpeed : -dashSpeed, 0), ForceMode2D.Impulse);
        isDashing = false;
    }

    private void ToggleSlowMotion()
    {
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            isSlowMotionActive = false;
        }
        else
        {
            Time.timeScale = slowMotionFactor;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
            isSlowMotionActive = true;
        }
    }

    private void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    private IEnumerator ResetAttackFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false; // Reset attacking flag
    }

    private void OnDrawGizmosSelected()
    {
    }
}
